using TMPro;
using UnityEngine;

namespace Spinner
{

    public class RoundStartUI : InGameStateUIBase<RoundStartState>
    {
        [SerializeField] private TextMeshProUGUI m_TimerText;

        protected override void OnUpdateUI(RoundStartState state, object param = null)
        {
            if (param is not float timeRemaining)
            {
                return;
            }

            m_TimerText.text = timeRemaining > 0.5f ? $"{timeRemaining:F0}" : "GO!";
        }
    }
}
