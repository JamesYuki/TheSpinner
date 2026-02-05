using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using UnityEngine;

namespace Spinner
{
    public class WaitForPlayerState : PredictedStateNode<WaitForPlayerState.State>
    {
        [SerializeField] private int m_RequiredPlayers = 2;

        public override void Enter()
        {
            ServiceLocator.Service<InGameUIManager>().ActiveStateUI(this);
        }
        protected override void StateSimulate(ref State state, float delta)
        {
            string playerDataText = $"{predictionManager.players.currentState.players.Count}/{m_RequiredPlayers}";
            ServiceLocator.Service<InGameUIManager>().UpdateStateUI(this, playerDataText);

            if (predictionManager.players.currentState.players.Count >= m_RequiredPlayers)
            {
                machine.Next();
            }
        }

        public struct State : IPredictedData<State>
        {

            public void Dispose() { }
        }
    }
}