using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using UnityEngine;

namespace Spinner
{
    public class RoundStartState : PredictedStateNode<RoundStartState.State>
    {
        [SerializeField] private float m_StartDelay = 3.0f;

        public override void Enter()
        {
            ServiceLocator.Service<InGameUIManager>().ActiveStateUI(this);
        }

        protected override State GetInitialState()
        {
            return new State
            {
                StartTimer = m_StartDelay
            };
        }

        protected override void StateSimulate(ref State state, float delta)
        {
            state.StartTimer -= delta;

            ServiceLocator.Service<InGameUIManager>().UpdateStateUI(this, state.StartTimer);

            if (state.StartTimer <= 0)
            {
                machine.Next();
            }
        }

        public struct State : IPredictedData<State>
        {
            public float StartTimer;
            public void Dispose() { }
        }
    }
}