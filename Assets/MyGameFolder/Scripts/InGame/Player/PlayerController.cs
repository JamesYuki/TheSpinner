using UnityEngine;
using PurrNet;
using PurrNet.Prediction;

namespace Spinner
{
    public class PlayerController : PredictedIdentity<MoveInput, MoveState>
    {
        [SerializeField]
        private float m_Speed = 10f;

        [SerializeField]
        private float m_MaxAngularVelocity = 180f;

        [SerializeField]
        private float m_RotationAcceleration = 540f;

        [SerializeField]
        private float m_RotationDeceleration = 360f;

        private PlayerInputHandler m_Input;
        private PredictedRigidbody m_Rigidbody;

        private void Awake()
        {
            m_Input = gameObject.GetComponent<PlayerInputHandler>();
            m_Rigidbody = gameObject.GetComponent<PredictedRigidbody>();
        }

        private void OnEnable()
        {
            m_Rigidbody.onCollisionEnter += OnCollisionStart;
        }

        private void OnDisable()
        {
            m_Rigidbody.onCollisionEnter -= OnCollisionStart;
        }

        private void OnCollisionStart(GameObject other, PhysicsCollision physicsEvent)
        {
            AppLogger.Log($"PlayerController OnCollisionStart with {other.name}");

            if (!other.TryGetComponent<PlayerController>(out var otherPlayer))
            {
                return;
            }

            AppLogger.Log("PlayerController OnCollisionStart with another PlayerController");
        }

        protected override void UpdateInput(ref MoveInput input)
        {
        }

        protected override void GetFinalInput(ref MoveInput input)
        {
            var moveInput = m_Input.GetMove();
            input.RotationInput = moveInput.x;
        }

        protected override void ModifyExtrapolatedInput(ref MoveInput input)
        {
        }

        protected override void Simulate(MoveInput input, ref MoveState state, float delta)
        {
            // 現在の角度を取得して正規化（-180~180の範囲）
            float currentY = transform.localEulerAngles.y;
            if (currentY > 180f) currentY -= 360f;

            // 角度を-90~90度の範囲に強制的にクランプ
            bool wasAtRightLimit = false;
            bool wasAtLeftLimit = false;

            if (currentY >= 90f)
            {
                currentY = 90f;
                wasAtRightLimit = true;
                // 正方向の角速度のみリセット
                if (state.AngularVelocity > 0f)
                {
                    state.AngularVelocity = 0f;
                }
                Vector3 euler = transform.localEulerAngles;
                euler.y = 90f;
                transform.localEulerAngles = euler;
            }
            else if (currentY <= -90f)
            {
                currentY = -90f;
                wasAtLeftLimit = true;
                // 負方向の角速度のみリセット
                if (state.AngularVelocity < 0f)
                {
                    state.AngularVelocity = 0f;
                }
                Vector3 euler = transform.localEulerAngles;
                euler.y = 270f; // -90度は270度と同じ
                transform.localEulerAngles = euler;
            }

            if (Mathf.Abs(input.RotationInput) > 0.01f)
            {
                // 制限に達している場合、その方向への入力は無視
                if ((wasAtRightLimit && input.RotationInput > 0) || (wasAtLeftLimit && input.RotationInput < 0))
                {
                    // 制限方向への入力は無視（角速度は変更しない）
                }
                else
                {
                    // 新しい角速度を計算
                    float newAngularVelocity = state.AngularVelocity + input.RotationInput * m_RotationAcceleration * delta;
                    newAngularVelocity = Mathf.Clamp(newAngularVelocity, -m_MaxAngularVelocity, m_MaxAngularVelocity);

                    // 予測される次の角度を計算
                    float predictedY = currentY + newAngularVelocity * delta * m_Speed;

                    // 制限に近づいている場合は角速度を減衰
                    if (predictedY > 90f && input.RotationInput > 0)
                    {
                        // 制限までの距離に応じて角速度を調整
                        float distanceToLimit = 90f - currentY;
                        float maxSafeVelocity = distanceToLimit / (delta * m_Speed);
                        state.AngularVelocity = Mathf.Min(newAngularVelocity, maxSafeVelocity);
                    }
                    else if (predictedY < -90f && input.RotationInput < 0)
                    {
                        // 制限までの距離に応じて角速度を調整
                        float distanceToLimit = currentY - (-90f);
                        float maxSafeVelocity = -distanceToLimit / (delta * m_Speed);
                        state.AngularVelocity = Mathf.Max(newAngularVelocity, maxSafeVelocity);
                    }
                    else
                    {
                        state.AngularVelocity = newAngularVelocity;
                    }
                }
            }
            else
            {
                // 入力がない場合は自然減速
                float deceleration = m_RotationDeceleration * delta;
                if (Mathf.Abs(state.AngularVelocity) <= deceleration)
                {
                    state.AngularVelocity = 0f;
                }
                else
                {
                    state.AngularVelocity -= Mathf.Sign(state.AngularVelocity) * deceleration;
                }
            }

            // Rigidbodyに角速度を適用
            if (Mathf.Abs(state.AngularVelocity) > 0.01f)
            {
                float targetAngularVelRad = state.AngularVelocity * Mathf.Deg2Rad * m_Speed;
                m_Rigidbody.angularVelocity = new Vector3(0f, targetAngularVelRad, 0f);
            }
            else
            {
                m_Rigidbody.angularVelocity = Vector3.zero;
            }
        }

        protected override void SanitizeInput(ref MoveInput input)
        {
            input.RotationInput = Mathf.Clamp(input.RotationInput, -1f, 1f);
        }
    }

    public struct MoveState : IPredictedData<MoveState>
    {
        public float AngularVelocity;
        public void Dispose()
        {
        }
    }

    public struct MoveInput : IPredictedData
    {
        public float RotationInput;
        public void Dispose()
        {
        }
    }
}