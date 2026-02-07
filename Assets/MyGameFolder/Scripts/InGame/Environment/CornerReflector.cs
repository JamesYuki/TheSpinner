using UnityEngine;

namespace Spinner
{
    /// <summary>
    /// コーナーに配置する障害物。接触したオブジェクトを中央に向かって反射させます。
    /// </summary>
    public class CornerReflector : MonoBehaviour
    {
        [Header("Reflection Settings")]
        [SerializeField, Tooltip("反射の強さ（1.0 = 通常の反射、1.5 = より強い反射）")]
        private float m_ReflectionStrength = 1.2f;

        [SerializeField, Tooltip("中央へ向かう力を追加する")]
        private float m_CenterPullForce = 5f;

        [SerializeField, Tooltip("反射時のエフェクト")]
        private ParticleSystem m_ReflectEffect;

        [SerializeField, Tooltip("反射時のサウンド")]
        private AudioClip m_ReflectSound;

        [Header("Center Position")]
        [SerializeField, Tooltip("反射の中心位置（通常はゲームフィールドの中心）")]
        private Transform m_CenterPoint;

        private AudioSource m_AudioSource;

        private void Awake()
        {
            // AudioSourceがなければ追加
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null && m_ReflectSound != null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
                m_AudioSource.playOnAwake = false;
                m_AudioSource.spatialBlend = 1f; // 3D sound
            }

            // 中心点が設定されていない場合は原点を使用
            if (m_CenterPoint == null)
            {
                GameObject centerObj = GameObject.Find("GameCenter");
                if (centerObj != null)
                {
                    m_CenterPoint = centerObj.transform;
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            Rigidbody rb = collision.rigidbody;
            if (rb == null) return;

            // 反射ベクトルを計算
            Vector3 incidentVelocity = rb.linearVelocity;
            Vector3 contactNormal = collision.contacts[0].normal;
            
            // 通常の反射ベクトル
            Vector3 reflectedVelocity = Vector3.Reflect(incidentVelocity, contactNormal);

            // 中央への方向ベクトルを計算
            Vector3 centerDirection = Vector3.zero;
            if (m_CenterPoint != null)
            {
                centerDirection = (m_CenterPoint.position - collision.contacts[0].point).normalized;
            }
            else
            {
                // 中心点がない場合は原点方向
                centerDirection = (Vector3.zero - collision.contacts[0].point).normalized;
            }

            // 反射ベクトルと中央方向を組み合わせる
            Vector3 finalVelocity = reflectedVelocity * m_ReflectionStrength + 
                                   centerDirection * m_CenterPullForce;

            // 元の速度の大きさを維持（オプション）
            float originalSpeed = incidentVelocity.magnitude;
            if (originalSpeed > 0)
            {
                finalVelocity = finalVelocity.normalized * Mathf.Max(originalSpeed, finalVelocity.magnitude);
            }

            // 新しい速度を適用
            rb.linearVelocity = finalVelocity;

            // エフェクトとサウンドを再生
            PlayReflectEffect(collision.contacts[0].point);
            PlayReflectSound();
        }

        private void PlayReflectEffect(Vector3 position)
        {
            if (m_ReflectEffect != null)
            {
                // エフェクトの位置を接触点に設定して再生
                ParticleSystem effect = Instantiate(m_ReflectEffect, position, Quaternion.identity);
                Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
            }
        }

        private void PlayReflectSound()
        {
            if (m_AudioSource != null && m_ReflectSound != null)
            {
                m_AudioSource.PlayOneShot(m_ReflectSound);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // エディタでの視覚化
            Gizmos.color = Color.cyan;
            
            // 中心点への方向を表示
            Vector3 centerPos = m_CenterPoint != null ? m_CenterPoint.position : Vector3.zero;
            Gizmos.DrawLine(transform.position, centerPos);
            
            // 反射範囲を表示
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
