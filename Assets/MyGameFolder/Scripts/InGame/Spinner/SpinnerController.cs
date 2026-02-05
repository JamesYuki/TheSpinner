using System;
using UnityEngine;
using PurrNet;
using PurrNet.Prediction;
using PurrNet.Logging;

namespace Spinner
{
    public class SpinnerController : PredictedIdentity<SpinnerInput, SpinnerState>
    {
        [Header("設定")]
        [SerializeField]
        private SpinnerSettings m_Settings;

        [Header("コンポーネント参照")]
        [SerializeField, Tooltip("回転するアームのRigidbody（物理回転用）")]
        private PredictedRigidbody m_ArmRigidbody;

        [SerializeField, Tooltip("左アームのコライダー")]
        private Collider m_LeftArmCollider;

        [SerializeField, Tooltip("右アームのコライダー")]
        private Collider m_RightArmCollider;

        [Header("物理設定")]
        [SerializeField, Tooltip("衝突時のインパクト倍率")]
        private float m_ImpactMultiplier = 2f;

        [SerializeField, Tooltip("最大インパクト速度")]
        private float m_MaxImpactVelocity = 50f;

        private SpinnerInputHandler m_InputHandler;
        private PredictedRigidbody m_Rigidbody;

        // イベント
        public Action OnDeath;
        public Action OnRespawn;
        public Action<float> OnAngularVelocityChanged;

        // プロパティ
        public SpinnerSettings Settings => m_Settings;
        public float CurrentAngularVelocity { get; private set; }

        // PurrNet入力システム用
        public override bool hasInput => true;

        private void Awake()
        {
            m_InputHandler = GetComponent<SpinnerInputHandler>();
            m_Rigidbody = GetComponent<PredictedRigidbody>();
        }

        protected override void LateAwake()
        {
            // ネットワーク初期化後の処理
            PurrLogger.Log($"[SpinnerController] LateAwake - isOwner: {isOwner}");

            // Rigidbody の初期状態をログ
            if (m_ArmRigidbody != null)
            {
                PurrLogger.Log($"[SpinnerController] Initial RB state - AngularVel: {m_ArmRigidbody.angularVelocity}, Rotation: {m_ArmRigidbody.rotation.eulerAngles}, isKinematic: {m_ArmRigidbody.isKinematic}", this);
            }
        }

        protected override SpinnerState GetInitialState()
        {
            var initialState = new SpinnerState
            {
                AngularVelocity = 0f,
                CurrentAngle = 0f
            };

            PurrLogger.LogWarning($"[SpinnerController] GetInitialState called! Returning state with AngVel={initialState.AngularVelocity}°/s, CurrentAngVel={CurrentAngularVelocity}°/s, isOwner={isOwner}", this);
            return initialState;
        }

        private void OnEnable()
        {
            if (m_Rigidbody != null)
            {
                m_Rigidbody.onCollisionEnter += OnCollisionStart;
            }
        }

        private void OnDisable()
        {
            if (m_Rigidbody != null)
            {
                m_Rigidbody.onCollisionEnter -= OnCollisionStart;
            }
        }

        private void OnCollisionStart(GameObject other, PhysicsCollision collision)
        {
            // パックとの衝突処理
            if (other.TryGetComponent<Puck>(out var puck))
            {
                HandlePuckCollision(puck, collision);
            }
            // 他プレイヤーとの衝突
            else if (other.TryGetComponent<SpinnerController>(out var otherPlayer))
            {
                HandlePlayerCollision(otherPlayer, collision);
            }
        }

        private void HandlePuckCollision(Puck puck, PhysicsCollision collision)
        {
            // アームの回転速度に基づいてパックに力を加える
            float impactSpeed = Mathf.Abs(CurrentAngularVelocity) * Mathf.Deg2Rad * m_Settings.ArmLength;

            // 衝突点からパックへの方向
            Vector3 impactDirection = (puck.transform.position - transform.position).normalized;

            // 回転方向を考慮した力の方向
            Vector3 tangentDirection = Vector3.Cross(Vector3.up, impactDirection);
            if (CurrentAngularVelocity < 0) tangentDirection = -tangentDirection;

            // 衝撃力を計算
            Vector3 impactForce = (impactDirection + tangentDirection).normalized *
                                  Mathf.Min(impactSpeed * m_Settings.PuckImpactMultiplier, m_Settings.MaxImpactVelocity);

            puck.ApplyImpact(impactForce);
        }

        private void HandlePlayerCollision(SpinnerController otherPlayer, PhysicsCollision collision)
        {
            // 他プレイヤーとの衝突処理（必要に応じて拡張）
        }

        /// <summary>
        /// アームの衝突処理（SpinnerArmCollisionForwarderから呼ばれる）
        /// </summary>
        public void HandleArmCollision(Collision collision, Collider armCollider)
        {
            if (collision.contactCount == 0) return;

            // 衝突点での接線速度を計算
            ContactPoint contact = collision.GetContact(0);
            Vector3 contactPoint = contact.point;

            // 中心から衝突点への距離
            Vector3 toContact = contactPoint - transform.position;
            toContact.y = 0;
            float radius = Mathf.Max(0.1f, toContact.magnitude);

            // 接線速度 = 角速度(rad/s) × 半径
            float angularVelRad = CurrentAngularVelocity * Mathf.Deg2Rad;
            float tangentSpeed = Mathf.Abs(angularVelRad) * radius;

            // 接線方向を計算
            Vector3 tangentDir = Vector3.Cross(Vector3.up, toContact.normalized);
            if (CurrentAngularVelocity < 0) tangentDir = -tangentDir;

            // 外向き方向
            Vector3 outwardDir = toContact.normalized;

            // 衝撃方向（接線 + 外向き）
            Vector3 impactDir = (tangentDir * 0.6f + outwardDir * 0.4f).normalized;

            // 力の大きさ
            float impactMagnitude = Mathf.Min(tangentSpeed * m_ImpactMultiplier, m_MaxImpactVelocity);

            // Puckの場合
            if (collision.gameObject.TryGetComponent<Puck>(out var puck))
            {
                puck.ApplyImpact(impactDir * impactMagnitude);
            }
            // PredictedRigidbodyの場合
            else if (collision.gameObject.TryGetComponent<PredictedRigidbody>(out var predictedRb))
            {
                predictedRb.AddForce(impactDir * impactMagnitude, ForceMode.Impulse);
            }
            // 通常のRigidbody
            else if (collision.rigidbody != null)
            {
                collision.rigidbody.AddForce(impactDir * impactMagnitude, ForceMode.Impulse);
            }
        }

        protected override void UpdateInput(ref SpinnerInput input)
        {
            // 持続入力の更新（予測フレームで呼ばれる）
            if (m_InputHandler != null && isOwner)
            {
                input.RotationDirection = m_InputHandler.GetRotationInput();
                input.Brake = m_InputHandler.GetBrakeInput();
                if (Mathf.Abs(input.RotationDirection) > 0.01f || input.Brake)
                {
                    PurrLogger.Log($"[SpinnerController] UpdateInput - Rotation: {input.RotationDirection}, Brake: {input.Brake}, isOwner: {isOwner}, CurrentAngVel={CurrentAngularVelocity}°/s", this);
                }
            }
            else
            {
                if (isOwner && m_InputHandler == null)
                {
                    PurrLogger.LogWarning($"[SpinnerController] UpdateInput - InputHandler is null but isOwner is true", this);
                }
            }
        }

        protected override void GetFinalInput(ref SpinnerInput input)
        {
            if (m_InputHandler == null || !isOwner) return;

            input.RotationDirection = m_InputHandler.GetRotationInput();
            input.Brake = m_InputHandler.GetBrakeInput();
        }

        protected override void ModifyExtrapolatedInput(ref SpinnerInput input)
        {
            // 外挿時はブレーキを無効化
            input.Brake = false;
        }

        protected override void Simulate(SpinnerInput input, ref SpinnerState state, float delta)
        {
            // 簡単な角速度制御（ステートマシンなし）
            if (Mathf.Abs(input.RotationDirection) > 0.01f)
            {
                // 加速
                state.AngularVelocity += input.RotationDirection * m_Settings.AngularAcceleration * delta;
                state.AngularVelocity = Mathf.Clamp(state.AngularVelocity, -m_Settings.MaxAngularVelocity, m_Settings.MaxAngularVelocity);
            }
            else if (input.Brake)
            {
                // ブレーキ
                float brakeForce = m_Settings.BrakeDeceleration * delta;
                if (Mathf.Abs(state.AngularVelocity) <= brakeForce)
                {
                    state.AngularVelocity = 0f;
                }
                else
                {
                    state.AngularVelocity -= Mathf.Sign(state.AngularVelocity) * brakeForce;
                }
            }
            else
            {
                // 自然減速
                float friction = m_Settings.NaturalDeceleration * delta;
                if (Mathf.Abs(state.AngularVelocity) <= friction)
                {
                    state.AngularVelocity = 0f;
                }
                else
                {
                    state.AngularVelocity -= Mathf.Sign(state.AngularVelocity) * friction;
                }
            }

            if (m_ArmRigidbody != null)
            {
                // 角速度を度/秒からラジアン/秒に変換してY軸に設定
                float targetAngularVelRad = state.AngularVelocity * Mathf.Deg2Rad;
                m_ArmRigidbody.angularVelocity = new Vector3(0f, targetAngularVelRad, 0f);

                // 現在の角度をRigidbodyから取得して状態に反映
                float currentAngle = m_ArmRigidbody.rotation.eulerAngles.y;
                if (currentAngle > 180f) currentAngle -= 360f;
                state.CurrentAngle = currentAngle;
            }

            // プロパティを更新
            CurrentAngularVelocity = state.AngularVelocity;

            // イベント発火
            OnAngularVelocityChanged?.Invoke(state.AngularVelocity);
        }

        protected override void SanitizeInput(ref SpinnerInput input)
        {
            input.RotationDirection = Mathf.Clamp(input.RotationDirection, -1f, 1f);
        }

        private void OnDrawGizmosSelected()
        {
            if (m_Settings == null) return;

            // アームの範囲を表示
            Gizmos.color = Color.cyan;
            Vector3 leftArm = transform.position + transform.right * -m_Settings.ArmLength;
            Vector3 rightArm = transform.position + transform.right * m_Settings.ArmLength;

            Gizmos.DrawLine(leftArm, rightArm);
            Gizmos.DrawWireSphere(transform.position, 0.3f); // コア
            Gizmos.DrawWireSphere(leftArm, m_Settings.ArmWidth * 0.5f);
            Gizmos.DrawWireSphere(rightArm, m_Settings.ArmWidth * 0.5f);
        }
    }
}
