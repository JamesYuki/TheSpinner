using System;
using UnityEngine;
using PurrNet;
using PurrNet.Prediction;

namespace Spinner
{
    public class PlayerCore : PredictedIdentity<PlayerCore.State>
    {

        public struct State : IPredictedData<State>
        {
            public int TeamId;

            public void Dispose()
            {
            }
        }

        [Header("プレイヤー設定")]
        [SerializeField, Tooltip("このプレイヤーのチームID（0または1）")]
        private int m_TeamId;

        private PredictedRigidbody m_Rigidbody;

        // TeamIdはStateから取得（ネットワーク同期される）
        public int TeamId => currentState.TeamId;

        private void Awake()
        {
            m_Rigidbody = GetComponent<PredictedRigidbody>();
        }

        protected override void LateAwake()
        {
        }

        protected override State GetInitialState()
        {
            return new State
            {
                TeamId = m_TeamId
            };
        }

        private void OnEnable()
        {
            if (m_Rigidbody != null)
            {
                m_Rigidbody.onCollisionEnter += OnCollisionStart;
            }
        }

        private void OnDisable()
        {
            if (m_Rigidbody != null)
            {
                m_Rigidbody.onCollisionEnter -= OnCollisionStart;
            }
        }

        private void OnCollisionStart(GameObject other, PhysicsCollision collision)
        {
        }

        protected override void Simulate(ref State state, float delta)
        {
            state.TeamId = m_TeamId;
        }
    }
}
