using UnityEngine;

namespace Spinner
{
    /// <summary>
    /// ゴールエリア
    /// パックがここに入ったら得点
    /// </summary>
    public class Goal : MonoBehaviour
    {
        [SerializeField, Tooltip("このゴールが所属するチームID（0または1）")]
        private int m_TeamId;

        public int TeamId => m_TeamId;

        private void OnDrawGizmos()
        {
            Gizmos.color = m_TeamId == 0 ? Color.blue : Color.red;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }
}
