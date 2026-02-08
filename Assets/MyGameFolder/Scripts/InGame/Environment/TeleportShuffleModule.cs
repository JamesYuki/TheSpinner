using PurrNet.Prediction;
using UnityEngine;

namespace Spinner
{
    /// <summary>
    /// テレポートゾーンのシャッフルを予測ループ内で管理するモジュール。
    /// RoundRunningState に自動登録され、独自のStateでタイマー・乱数・シードを管理する。
    /// ロールバック・リプレイには PredictedModule のHistory基盤で自動対応。
    /// ビジュアル適用は UpdateView で行うため、リプレイ中の高速シャッフル問題が発生しない。
    /// </summary>
    public class TeleportShuffleModule : PredictedModule<TeleportShuffleModule.ShuffleState>
    {
        private readonly float m_ShuffleInterval;
        private readonly float m_InitialDelay;
        private readonly bool m_Enabled;

        /// <summary>最後にビジュアル適用したシード（不要な再適用を防ぐ）</summary>
        private uint m_LastAppliedSeed;

        public TeleportShuffleModule(
            PredictedIdentity identity,
            float shuffleInterval,
            float initialDelay,
            bool enabled
        ) : base(identity)
        {
            m_ShuffleInterval = shuffleInterval;
            m_InitialDelay = initialDelay;
            m_Enabled = enabled;
        }

        protected override void SimulationStart()
        {
            // 初期状態を設定
            var state = currentState;
            state.ShuffleRandom = PredictedRandom.Create(12345);
            state.CurrentShuffleSeed = state.ShuffleRandom.Next();
            state.ShuffleTimer = m_InitialDelay;
            currentState = state;
        }

        protected override void Simulate(ref ShuffleState state, float delta)
        {
            if (!m_Enabled) return;

            state.ShuffleTimer -= delta;
            if (state.ShuffleTimer <= 0f)
            {
                state.ShuffleTimer = m_ShuffleInterval;
                state.CurrentShuffleSeed = state.ShuffleRandom.Next();
            }
        }

        /// <summary>
        /// フレーム毎に呼ばれるビジュアル更新。
        /// リプレイ中は呼ばれないため、高速シャッフル問題は発生しない。
        /// </summary>
        protected override void UpdateView(ShuffleState viewState, ShuffleState? verifiedState)
        {
            if (viewState.CurrentShuffleSeed == 0) return;
            if (viewState.CurrentShuffleSeed == m_LastAppliedSeed) return;

            m_LastAppliedSeed = viewState.CurrentShuffleSeed;

            var teleportManager = ServiceLocator.Service<TeleportManager>();
            teleportManager?.ApplyShuffleFromSeed(viewState.CurrentShuffleSeed);
        }

        public struct ShuffleState : IPredictedData<ShuffleState>
        {
            public float ShuffleTimer;
            public PredictedRandom ShuffleRandom;
            public uint CurrentShuffleSeed;

            public void Dispose() { }
        }
    }
}
