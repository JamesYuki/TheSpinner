using UnityEngine;
using PurrNet;
using PurrNet.Prediction;

namespace Spinner
{
    /// <summary>
    /// プレイヤーのコア部分。ダメージ判定を担当し、Spinnerへの参照を保持する。
    /// Spinnerの生成はPlayerSpawningStateで行う。
    /// 構造の詳細は StructureChanges.md を参照。
    /// </summary>
    public class PlayerCore : PredictedIdentity<PlayerCore.State>
    {
        [Header("Spinner設定")]
        [SerializeField, Tooltip("Spinnerの親Transform（空の場合はこのオブジェクト）")]
        private Transform m_SpinnerParent;

        private SpinnerController m_SpinnerController;

        public SpinnerController SpinnerController => m_SpinnerController;
        public bool IsSpinnerLoaded => m_SpinnerController != null;
        public Transform SpinnerParent => m_SpinnerParent != null ? m_SpinnerParent : transform;

        /// <summary>
        /// Spinnerの参照を設定する（PlayerSpawningStateから呼ばれる）
        /// </summary>
        public void SetSpinner(SpinnerController spinner)
        {
            m_SpinnerController = spinner;
            Debug.Log($"[PlayerCore] Spinner設定完了: {spinner?.gameObject.name}");
        }

        public struct State : IPredictedData<State>
        {
            public void Dispose() { }
        }
    }
}
