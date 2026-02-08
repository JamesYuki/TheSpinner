using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using PurrNet;
using PurrNet.Prediction;

namespace Spinner
{
    public class Puck : PredictedIdentity<Puck.PuckState>
    {
        [Header("設定")]
        [SerializeField, Tooltip("最大速度")]
        private float m_MaxSpeed = 30f;

        [SerializeField, Tooltip("一定速度（最小速度）")]
        private float m_ConstantSpeed = 5f;

        [SerializeField, Tooltip("摩擦（減速率）")]
        private float m_Friction = 0.5f;

        [SerializeField, Tooltip("一定速度を維持するか")]
        private bool m_MaintainConstantSpeed = true;

        [SerializeField, Tooltip("壁での反射時の速度維持率")]
        private float m_WallBounceRetention = 0.95f;

        [Header("ダメージ設定")]
        [SerializeField, Tooltip("ダメージを与える最低速度")]
        private float m_DamageThreshold = 10f;

        [SerializeField, Tooltip("速度あたりのダメージ倍率")]
        private float m_DamageMultiplier = 1.0f;

        [SerializeField, Tooltip("最大ダメージ")]
        private float m_MaxDamage = 1.0f;

        [Header("ビジュアル設定")]
        [SerializeField, Tooltip("速度に応じた色のグラデーション（0=停止時、1=最大速度時）")]
        private Gradient m_SpeedColorGradient;

        [SerializeField, Tooltip("色を変更するレンダラー")]
        private Renderer m_Renderer;

        [Header("テレポート設定")]
        [SerializeField, Tooltip("テレポート後の再テレポート防止クールダウン（秒）")]
        private float m_TeleportCooldown = 0.5f;

        private PredictedRigidbody m_Rigidbody;
        private MaterialPropertyBlock m_PropertyBlock;

        private float m_TeleportCooldownTimer;

        private TeleportManager TeleportManager => ServiceLocator.Service<TeleportManager>();

        public struct PuckState : IPredictedData<PuckState>
        {
            public Vector3 Velocity;
            public bool IsActive;

            public void Dispose()
            {
            }
        }

        private void Awake()
        {
            m_Rigidbody = GetComponent<PredictedRigidbody>();

            if (m_Renderer == null)
            {
                m_Renderer = GetComponent<Renderer>();
            }

            m_PropertyBlock = new MaterialPropertyBlock();
        }

        protected override void LateAwake()
        {
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

        /// <summary>
        /// Unity ネイティブのトリガー検知
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // 予測シミュレーション中のみテレポート処理を実行
            if (predictionManager != null && predictionManager.isSimulating && !predictionManager.isVerifiedAndReplaying)
            {
                HandleTeleportTrigger(other.gameObject);
            }
        }



        /// <summary>
        /// テレポート処理の共通ロジック
        /// </summary>
        private void HandleTeleportTrigger(GameObject other)
        {
            if (other == null) return;

            // クールダウン中はテレポートしない
            if (m_TeleportCooldownTimer > 0f)
                return;

            var teleportZone = other.GetComponent<TeleportZone>();
            if (teleportZone == null)
                return;

            // テレポート前に現在速度を保存し、パックを停止させる
            float currentSpeed = GetCurrentSpeed();
            teleportZone.SetEntrySpeed(currentSpeed);

            // パックを完全に停止（直線速度と回転速度の両方をゼロに）
            if (m_Rigidbody != null)
            {
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }

            if (TeleportManager != null && TeleportManager.TryTeleport(teleportZone, this))
            {
                m_TeleportCooldownTimer = m_TeleportCooldown;
            }
        }

        private void OnCollisionStart(GameObject other, PhysicsCollision collision)
        {
            if (other == null) return;

            // 壁との衝突処理
            if (other.CompareTag(Tags.Wall))
            {
                if (m_Rigidbody != null && collision.contacts.Count > 0)
                {
                    Vector3 contactNormal = collision.contacts[0].normal;
                    Vector3 currentVelocity = m_Rigidbody.velocity;
                    float currentSpeed = currentVelocity.magnitude;
                    Vector3 reflectedVelocity = Vector3.Reflect(currentVelocity, contactNormal);

                    if (m_MaintainConstantSpeed)
                    {
                        float maintainedSpeed = Mathf.Clamp(currentSpeed, m_ConstantSpeed, m_MaxSpeed);
                        m_Rigidbody.velocity = reflectedVelocity.normalized * maintainedSpeed;
                    }
                    else
                    {
                        m_Rigidbody.velocity = reflectedVelocity * m_WallBounceRetention;
                    }
                }
            }

            // IDamageableを実装しているオブジェクトとの衝突処理
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                float currentSpeed = GetCurrentSpeed();

                // 速度が閾値を超えている場合のみダメージを与える
                if (currentSpeed >= m_DamageThreshold)
                {
                    float damage = (currentSpeed - m_DamageThreshold) * m_DamageMultiplier;
                    damage = Mathf.Min(damage, m_MaxDamage);

                    damageable.TakeDamage(damage, this);

                    AppLogger.Log($"パックがダメージ与えた: {damage} damage to {other.name} at speed {currentSpeed}");
                }
            }
        }

        /// <summary>
        /// スピナーからの衝撃を適用
        /// </summary>
        public void ApplyImpact(Vector3 force)
        {
            if (m_Rigidbody == null) return;

            Vector3 newVelocity = m_Rigidbody.velocity + force;

            if (newVelocity.magnitude > m_MaxSpeed)
            {
                newVelocity = newVelocity.normalized * m_MaxSpeed;
            }

            if (m_MaintainConstantSpeed && newVelocity.magnitude < m_ConstantSpeed)
            {
                newVelocity = newVelocity.normalized * m_ConstantSpeed;
            }

            m_Rigidbody.velocity = newVelocity;
        }

        /// <summary>
        /// 一定速度の設定を変更
        /// </summary>
        public void SetConstantSpeed(float speed, bool maintainConstant = true)
        {
            m_ConstantSpeed = speed;
            m_MaintainConstantSpeed = maintainConstant;
        }

        /// <summary>
        /// 現在の速度を取得
        /// </summary>
        public float GetCurrentSpeed()
        {
            return m_Rigidbody != null ? m_Rigidbody.velocity.magnitude : 0f;
        }

        /// <summary>
        /// パックをテレポートさせる（位置と速度を直接設定）
        /// PredictedRigidbodyを一時無効化して瞬間移動を実現
        /// </summary>
        public void Teleport(Vector3 position, Vector3 velocity)
        {
            if (m_Rigidbody != null)
            {
                TeleportAsync(position, velocity).Forget();
            }
            else
            {
                var targetPosition = position;
                targetPosition.y = transform.position.y;
                transform.position = targetPosition;
            }
        }

        /// <summary>
        /// テレポート処理（UniTask版）
        /// 一時的にPredictedIdentityとRigidbodyを無効化して位置を設定し、次のフレームで再有効化
        /// これによりPredictedTransformの補間を防ぐ
        /// </summary>
        private async UniTask TeleportAsync(Vector3 position, Vector3 velocity)
        {
            // PredictedIdentity自体を一時無効化してネットワーク同期を停止
            bool wasThisEnabled = enabled;
            bool wasRigidbodyEnabled = m_Rigidbody.enabled;

            enabled = false;
            m_Rigidbody.enabled = false;

            // Rigidbody.positionを使用してTransform補間を回避
            var targetPosition = position;
            targetPosition.y = m_Rigidbody.position.y;
            m_Rigidbody.position = targetPosition;

            // 次フレーム待機
            await UniTask.NextFrame();

            if (wasThisEnabled)
            {
                enabled = true;
            }
            if (wasRigidbodyEnabled)
            {
                m_Rigidbody.enabled = true;
            }

            m_Rigidbody.velocity = velocity;

            AppLogger.Log($"[Puck] テレポート完了: Pos={targetPosition}, Vel={velocity}");
        }

        /// <summary>
        /// パックをリセット（ゴール後など）
        /// </summary>
        public void ResetPuck(Vector3 position, Vector3 initialVelocity = default)
        {
            transform.position = position;
            if (m_Rigidbody != null)
            {
                if (initialVelocity == Vector3.zero && m_MaintainConstantSpeed)
                {
                    Vector3 randomDirection = new Vector3(
                        UnityEngine.Random.Range(-1f, 1f),
                        0f,
                        UnityEngine.Random.Range(-1f, 1f)
                    ).normalized;
                    initialVelocity = randomDirection * m_ConstantSpeed;
                }

                m_Rigidbody.velocity = initialVelocity;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }
        }

        protected override void Simulate(ref PuckState state, float delta)
        {
            if (m_Rigidbody != null)
            {
                Vector3 velocity = m_Rigidbody.velocity;

                if (m_MaintainConstantSpeed)
                {
                    // 一定速度を維持するモード
                    if (velocity.magnitude > 0.1f) // わずかでも動いている場合
                    {
                        Vector3 direction = velocity.normalized;
                        float currentSpeed = velocity.magnitude;

                        if (currentSpeed > m_MaxSpeed)
                        {
                            velocity = direction * m_MaxSpeed;
                        }
                        else if (currentSpeed < m_ConstantSpeed)
                        {
                            velocity = direction * m_ConstantSpeed;
                        }
                    }
                    else
                    {
                        // 完全に停止している場合は、ランダムな方向に動き始める
                        Vector3 randomDirection = new Vector3(
                            UnityEngine.Random.Range(-1f, 1f),
                            0f,
                            UnityEngine.Random.Range(-1f, 1f)
                        ).normalized;
                        velocity = randomDirection * m_ConstantSpeed;
                    }
                }
                else
                {
                    velocity = Vector3.MoveTowards(velocity, Vector3.zero, m_Friction * delta);
                }

                m_Rigidbody.velocity = velocity;
                state.Velocity = velocity;
            }

            // テレポートクールダウンを減少
            if (m_TeleportCooldownTimer > 0f)
            {
                m_TeleportCooldownTimer -= delta;
            }

            UpdateColorBySpeed();
        }

        /// <summary>
        /// 速度に応じて色を更新
        /// </summary>
        private void UpdateColorBySpeed()
        {
            if (m_Renderer == null || m_SpeedColorGradient == null || m_Rigidbody == null)
                return;

            float currentSpeed = m_Rigidbody.velocity.magnitude;
            float normalizedSpeed = Mathf.Clamp01(currentSpeed / m_MaxSpeed);

            Color speedColor = m_SpeedColorGradient.Evaluate(normalizedSpeed);

            m_Renderer.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetColor("_Color", speedColor);
            m_PropertyBlock.SetColor("_BaseColor", speedColor); // URPの場合
            m_Renderer.SetPropertyBlock(m_PropertyBlock);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.2f);

            if (m_Rigidbody != null)
            {
                float currentSpeed = m_Rigidbody.velocity.magnitude;

                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, transform.position + m_Rigidbody.velocity * 0.5f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 1f,
                    $"Speed: {currentSpeed:F2} m/s\nMax: {m_MaxSpeed:F1}\nMin: {m_ConstantSpeed:F1}",
                    new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = Color.white },
                        fontSize = 12,
                        fontStyle = FontStyle.Bold
                    }
                );
#endif

                if (m_MaintainConstantSpeed)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(transform.position, m_ConstantSpeed * 0.1f);
                }
            }
        }
    }
}
