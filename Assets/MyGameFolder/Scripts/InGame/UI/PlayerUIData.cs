using TMPro;
using UnityEngine;
using System;

namespace Spinner
{
    /// <summary>
    /// プレイヤーのUI要素をまとめたクラス
    /// </summary>
    public class PlayerUIData : MonoBehaviour
    {
        [SerializeField, Tooltip("体力表示用テキスト")]
        private TextMeshProUGUI m_HealthText;

        [SerializeField, Tooltip("プレイヤー名表示用テキスト")]
        private TextMeshProUGUI m_NameText;

        public TextMeshProUGUI HealthText => m_HealthText;
        public TextMeshProUGUI NameText => m_NameText;

        private void OnDestroy()
        {
            Debug.Log($"[PlayerUIData] OnDestroy called: {gameObject.name}");
        }

        /// <summary>
        /// UIの表示/非表示を切り替え
        /// </summary>
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        /// <summary>
        /// 体力テキストを更新
        /// </summary>
        public void UpdateHealthText(int health, int maxHealth)
        {
            if (m_HealthText != null)
            {
                m_HealthText.text = $"{health}";
            }
        }

        /// <summary>
        /// プレイヤー名テキストを更新
        /// </summary>
        public void UpdateNameText(string playerName)
        {
            if (m_NameText != null)
            {
                m_NameText.text = playerName;
            }
        }
    }
}
