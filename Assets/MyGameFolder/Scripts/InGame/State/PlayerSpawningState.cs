using System.Collections.Generic;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using UnityEngine;

namespace Spinner
{
    public class PlayerSpawningState : PredictedStateNode<PlayerSpawningState.State>
    {
        [SerializeField, Tooltip("プレイヤーコアプレハブ")]
        private GameObject m_PlayerCorePrefab;

        [SerializeField, Tooltip("スピナープレハブ")]
        private GameObject m_SpinnerPrefab;

        [SerializeField]
        private List<Transform> m_SpawnPoints = new();

        public override void Enter()
        {
            ServiceLocator.Service<InGameUIManager>().ActiveStateUI(this);

            for (var i = 0; i < predictionManager.players.currentState.players.Count; i++)
            {
                var player = predictionManager.players.currentState.players[i];
                var spawnPoint = m_SpawnPoints[i % m_SpawnPoints.Count];

                // PlayerCoreを生成
                var playerCoreId = predictionManager.hierarchy.Create(
                    m_PlayerCorePrefab,
                    spawnPoint.position,
                    spawnPoint.rotation,
                    player
                );

                // Spinnerを生成
                var spinnerId = predictionManager.hierarchy.Create(
                    m_SpinnerPrefab,
                    spawnPoint.position,
                    spawnPoint.rotation,
                    player
                );

                AppLogger.Log($"プレイヤー生成: PlayerID={player}, PlayerCoreID={playerCoreId}, SpinnerID={spinnerId}");
            }

            // UIはPlayerHealth.LateAwake()で各自登録される

            machine.Next();
        }

        public struct State : IPredictedData<State>
        {
            public void Dispose() { }
        }
    }
}
