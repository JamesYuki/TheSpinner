using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace JPS
{
    public class PlayerItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_NameText;
        [SerializeField] private RawImage m_IconImage;

        public void SetData(string name, Texture2D icon)
        {
            if (m_NameText != null)
                m_NameText.text = name;
            if (m_IconImage != null)
                m_IconImage.texture = icon;
        }
    }
}
