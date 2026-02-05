using System;
using UnityEngine;
using UnityEngine.UI;

namespace Spinner
{
    public class TitleUIModule : UIModule
    {
        [SerializeField] private Button m_StartGameButton;

        public event Action StartGameButtonEvent;

        public override void Initialize()
        {
            if (m_StartGameButton != null)
            {
                m_StartGameButton.onClick.AddListener(OnTitleButtonClicked);
            }
        }

        public override void Complete()
        {
            if (m_StartGameButton != null)
            {
                m_StartGameButton.onClick.RemoveListener(OnTitleButtonClicked);
            }
        }

        private void OnTitleButtonClicked()
        {
            StartGameButtonEvent?.Invoke();
        }
    }
}