using System.Collections.Generic;
using JPS.System;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using Spinner;
using UnityEngine;

namespace JPS
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

                // SpinnerをPlayerCoreに関連付け
                if (playerCoreId.HasValue && spinnerId.HasValue)
                {
                    var playerCore = playerCoreId.GetComponent<PlayerCore>(predictionManager);
                    var spinnerController = spinnerId.GetComponent<SpinnerController>(predictionManager);

                    if (playerCore != null && spinnerController != null)
                    {
                        // SpinnerをPlayerCoreの子にする
                        var spinnerTransform = spinnerController.transform;
                        spinnerTransform.SetParent(playerCore.SpinnerParent);
                        spinnerTransform.localPosition = Vector3.zero;
                        spinnerTransform.localRotation = Quaternion.identity;

                        // 参照を設定
                        playerCore.SetSpinner(spinnerController);
                    }
                }
            }

            machine.Next();
        }

        public struct State : IPredictedData<State>
        {
            public void Dispose() { }
        }
    }
}
