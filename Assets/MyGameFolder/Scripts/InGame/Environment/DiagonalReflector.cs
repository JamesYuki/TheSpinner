using UnityEngine;

namespace Spinner
{
    /// <summary>
    /// 対角線方向に反射する障害物。コーナーに配置して、
    /// 当たったオブジェクトを対角線方向（中央方向）に反射させます。
    /// </summary>
    public class DiagonalReflector : MonoBehaviour
    {
        [Header("Reflector Settings")]
        [SerializeField, Tooltip("反射角度のオフセット（度）")]
        private float m_AngleOffset = 0f;

        [SerializeField, Tooltip("反射時の速度倍率")]
        private float m_VelocityMultiplier = 1.1f;

        [SerializeField, Tooltip("最小反射速度")]
        private float m_MinReflectSpeed = 5f;

        [SerializeField, Tooltip("反射時のエフェクト")]
        private ParticleSystem m_ReflectEffect;

        [Header("Debug")]
        [SerializeField]
        private bool m_ShowDebugRays = true;

        [SerializeField]
        private Color m_GizmoColor = Color.yellow;

        private void OnCollisionEnter(Collision collision)
        {
            HandleReflection(collision);
        }

        private void HandleReflection(Collision collision)
        {
            Rigidbody rb = collision.rigidbody;
            if (rb == null) return;

            ContactPoint contact = collision.contacts[0];
            Vector3 incomingVelocity = rb.linearVelocity;
            
            // 法線ベクトル
            Vector3 normal = contact.normal;
            
            // 対角線方向に反射（45度の角度で反射）
            // transformのforward方向を反射の基準とする
            Vector3 reflectDirection = transform.forward;
            
            // 角度オフセットを適用
            if (m_AngleOffset != 0)
            {
                reflectDirection = Quaternion.Euler(0, m_AngleOffset, 0) * reflectDirection;
            }

            // 入射速度の大きさを保持
            float speed = incomingVelocity.magnitude;
            speed = Mathf.Max(speed * m_VelocityMultiplier, m_MinReflectSpeed);

            // 新しい速度を設定
            Vector3 newVelocity = reflectDirection * speed;
            
            // Y軸の速度を保持（ジャンプなどの高さ情報を維持）
            newVelocity.y = incomingVelocity.y * 0.8f;

            rb.linearVelocity = newVelocity;

            // エフェクトを再生
            if (m_ReflectEffect != null)
            {
                ParticleSystem effect = Instantiate(m_ReflectEffect, contact.point, Quaternion.LookRotation(normal));
                Destroy(effect.gameObject, effect.main.duration + 1f);
            }

            // デバッグ表示
            if (m_ShowDebugRays)
            {
                Debug.DrawRay(contact.point, incomingVelocity.normalized * 2f, Color.red, 1f);
                Debug.DrawRay(contact.point, newVelocity.normalized * 2f, Color.green, 1f);
            }
        }

        private void OnDrawGizmos()
        {
            // エディタでの反射方向を表示
            Gizmos.color = m_GizmoColor;
            
            Vector3 direction = transform.forward;
            if (m_AngleOffset != 0)
            {
                direction = Quaternion.Euler(0, m_AngleOffset, 0) * direction;
            }

            // 反射方向の矢印を描画
            Vector3 start = transform.position;
            Vector3 end = start + direction * 3f;
            
            Gizmos.DrawLine(start, end);
            
            // 矢印の頭部
            Vector3 right = Quaternion.Euler(0, 45, 0) * -direction * 0.5f;
            Vector3 left = Quaternion.Euler(0, -45, 0) * -direction * 0.5f;
            
            Gizmos.DrawLine(end, end + right);
            Gizmos.DrawLine(end, end + left);
        }

        private void OnDrawGizmosSelected()
        {
            // 選択時により詳細な情報を表示
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
        }
    }
}
