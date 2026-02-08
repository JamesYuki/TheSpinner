using PurrNet;
using PurrNet.Pooling;
using PurrNet.Prediction;
using PurrNet.Prediction.StateMachine;
using R3;
using UnityEngine;

namespace Spinner
{
    public class RoundRunningState : PredictedStateNode<RoundRunningState.State>
    {
        [Header("パック設定")]
        [SerializeField, Tooltip("パックプレハブ")]
        private GameObject m_PuckPrefab;

        [SerializeField, Tooltip("パックの初期位置")]
        private Transform m_PuckSpawnPoint;
        private void Awake()
        {
            // PlayerHealth.OnDeathHandler += OnPlayerDeath;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            //  PlayerHealth.OnDeathHandler -= OnPlayerDeath;
        }

        public override void Enter()
        {
            // 既存のリストがあれば先にDisposeする
            if (!currentState.players.isDisposed)
            {
                currentState.players.Dispose();
            }

            // 新しいDisposableListを作成（プレイヤー数分の容量を確保）
            var playerCount = predictionManager.players.currentState.players.Count;
            var state = currentState;
            state.players = DisposableList<PlayerID>.Create(playerCount);

            // プレイヤーIDをコピー
            for (int i = 0; i < playerCount; i++)
            {
                state.players.Add(predictionManager.players.currentState.players[i]);
            }
            currentState = state;

            ServiceLocator.Service<InGameUIManager>().ActiveStateUI(this);

            // テレポートシステムの初期化（サーバーのみ）
            if (predictionManager.isServer)
            {
                var teleportManager = ServiceLocator.Service<TeleportManager>();
                if (teleportManager != null)
                {
                    teleportManager.InitializeForRound();
                    AppLogger.Log("[RoundRunning] テレポートシステムを初期化しました");
                }

                // パックを生成
                if (m_PuckPrefab != null && m_PuckSpawnPoint != null)
                {
                    var puckId = predictionManager.hierarchy.Create(
                        m_PuckPrefab,
                        m_PuckSpawnPoint.position,
                        m_PuckSpawnPoint.rotation,
                        null // パックはオーナーなし
                    );
                    AppLogger.Log($"[RoundRunning] パックを生成しました: PuckID={puckId}");
                }
                else
                {
                    AppLogger.LogWarning("[RoundRunning] パックプレハブまたはスポーン位置が設定されていません");
                }
            }
        }

        public override void Exit()
        {
            // Exit時にDisposableListをDisposeする
            if (!currentState.players.isDisposed)
            {
                var state = currentState;
                state.players.Dispose();
                currentState = state;
            }
            //  PlayerHealth.ClearPlayerHandler?.Invoke();
        }

        private void OnPlayerDeath(PlayerID? owner)
        {
            if (machine.currentStateNode is not RoundRunningState runningState || runningState != this)
            {
                return;
            }

            if (!owner.HasValue)
            {
                return;
            }

            // DisposableListがDisposeされていないか確認
            if (currentState.players.isDisposed)
            {
                return;
            }

            var state = currentState;
            state.players.Remove(owner.Value);
            currentState = state;

            if (currentState.players.Count <= 1)
            {
                machine.Next();
            }
        }

        public struct State : IPredictedData<State>
        {
            public DisposableList<PlayerID> players;
            public void Dispose()
            {
                if (!players.isDisposed)
                {
                    players.Dispose();
                }
            }
        }
    }
}
