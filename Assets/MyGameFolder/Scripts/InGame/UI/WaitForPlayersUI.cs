using PurrNet.Prediction.StateMachine;
using TMPro;
using UnityEngine;

namespace Spinner
{
    public class WaitForPlayersUI : InGameStateUIBase<WaitForPlayerState>
    {
        [SerializeField] private TextMeshProUGUI m_PlayerCountText;


        protected override void OnUpdateUI(WaitForPlayerState state, object param = null)
        {
            if (param is string playerDataText)
            {
                m_PlayerCountText.text = $"プレイヤー待機中... {playerDataText}";
            }
        }
    }
}