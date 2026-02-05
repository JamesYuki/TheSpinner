using UnityEngine;

namespace Spinner
{
    /// <summary>
    /// スピナーのアームに付けるコンポーネント
    /// 衝突イベントをSpinnerControllerに転送する
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SpinnerArmCollisionForwarder : MonoBehaviour
    {
        [SerializeField]
        private SpinnerController m_SpinnerController;

        private Collider m_Collider;

        private void Awake()
        {
            m_Collider = GetComponent<Collider>();

            if (m_SpinnerController == null)
            {
                m_SpinnerController = GetComponentInParent<SpinnerController>();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (m_SpinnerController != null)
            {
                m_SpinnerController.HandleArmCollision(collision, m_Collider);
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            // 継続的な接触でも力を加えたい場合
            if (m_SpinnerController != null)
            {
                m_SpinnerController.HandleArmCollision(collision, m_Collider);
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
