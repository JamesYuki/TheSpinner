using UnityEngine;
using PurrNet;

namespace Spinner
{
    /// <summary>
    /// スピナーのアームを物理ベースで回転させるコンポーネント
    /// HingeJointのモーターを使用して、Transform操作ではなく物理回転を行う
    /// これにより高速回転でも貫通しない
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SpinnerArmPhysics : MonoBehaviour
    {
        [Header("設定")]
        [SerializeField, Tooltip("親のSpinnerController")]
        private SpinnerController m_SpinnerController;

        [SerializeField, Tooltip("HingeJoint（自動設定される）")]
        private HingeJoint m_HingeJoint;

        [SerializeField, Tooltip("モーターの最大トルク")]
        private float m_MotorMaxTorque = 10000f;

        [SerializeField, Tooltip("衝突時のインパクト倍率")]
        private float m_ImpactMultiplier = 1.5f;

        [SerializeField, Tooltip("最大インパクト速度")]
        private float m_MaxImpactVelocity = 50f;

        private Rigidbody m_Rigidbody;
        private float m_TargetAngularVelocity;

        public Rigidbody ArmRigidbody => m_Rigidbody;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();

            // Rigidbodyの設定
            m_Rigidbody.useGravity = false;
            m_Rigidbody.linearDamping = 0f;
            m_Rigidbody.angularDamping = 0.5f;
            m_Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Y軸回転のみに制限
            m_Rigidbody.constraints = RigidbodyConstraints.FreezePositionX |
                                       RigidbodyConstraints.FreezePositionY |
                                       RigidbodyConstraints.FreezePositionZ |
                                       RigidbodyConstraints.FreezeRotationX |
                                       RigidbodyConstraints.FreezeRotationZ;

            SetupHingeJoint();
        }

        private void SetupHingeJoint()
        {
            if (m_HingeJoint == null)
            {
                m_HingeJoint = GetComponent<HingeJoint>();
            }

            if (m_HingeJoint == null)
            {
                m_HingeJoint = gameObject.AddComponent<HingeJoint>();
            }

            // HingeJointの設定
            m_HingeJoint.axis = Vector3.up; // Y軸周りに回転
            m_HingeJoint.useMotor = true;
            m_HingeJoint.useLimits = false; // 無制限回転

            // 親のRigidbodyに接続（なければワールドに固定）
            if (m_SpinnerController != null)
            {
                var parentRb = m_SpinnerController.GetComponent<Rigidbody>();
                if (parentRb != null)
                {
                    m_HingeJoint.connectedBody = parentRb;
                }
            }

            // アンカーを中心に設定
            m_HingeJoint.anchor = Vector3.zero;
        }

        /// <summary>
        /// 目標角速度を設定（SpinnerControllerから呼ばれる）
        /// </summary>
        public void SetTargetAngularVelocity(float angularVelocityDegrees)
        {
            m_TargetAngularVelocity = angularVelocityDegrees;

            // HingeJointのモーターを更新
            var motor = m_HingeJoint.motor;
            motor.targetVelocity = angularVelocityDegrees;
            motor.force = m_MotorMaxTorque;
            motor.freeSpin = false;
            m_HingeJoint.motor = motor;
        }

        /// <summary>
        /// 現在の角度を取得
        /// </summary>
        public float GetCurrentAngle()
        {
            return m_HingeJoint.angle;
        }

        /// <summary>
        /// 現在の角速度を取得（度/秒）
        /// </summary>
        public float GetCurrentAngularVelocity()
        {
            return m_Rigidbody.angularVelocity.y * Mathf.Rad2Deg;
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            // 継続的な接触でも力を加える（押し出し効果）
            HandleCollision(collision);
        }

        private void HandleCollision(Collision collision)
        {
            if (m_SpinnerController == null) return;

            var otherRb = collision.rigidbody;
            if (otherRb == null) return;

            // 衝突点での接線速度を計算
            ContactPoint contact = collision.GetContact(0);
            Vector3 contactPoint = contact.point;

            // 中心から衝突点への距離
            Vector3 toContact = contactPoint - transform.position;
            toContact.y = 0;
            float radius = toContact.magnitude;

            // 接線速度 = 角速度(rad/s) × 半径
            float angularVelRad = m_Rigidbody.angularVelocity.y;
            float tangentSpeed = Mathf.Abs(angularVelRad) * radius;

            // 接線方向を計算
            Vector3 tangentDir = Vector3.Cross(Vector3.up, toContact.normalized);
            if (angularVelRad < 0) tangentDir = -tangentDir;

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
            else
            {
                // 通常のRigidbody
                otherRb.AddForce(impactDir * impactMagnitude, ForceMode.Impulse);
            }
        }

        private void OnValidate()
        {
            if (m_SpinnerController == null)
            {
                m_SpinnerController = GetComponentInParent<SpinnerController>();
            }
        }
    }
}
