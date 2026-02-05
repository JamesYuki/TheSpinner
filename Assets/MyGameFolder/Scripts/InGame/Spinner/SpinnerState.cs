using PurrNet.Prediction;

namespace Spinner
{
    /// <summary>
    /// スピナープレイヤーの状態データ（予測用）
    /// </summary>
    public struct SpinnerState : IPredictedData<SpinnerState>
    {
        /// <summary>
        /// 現在の角速度（度/秒）
        /// </summary>
        public float AngularVelocity;

        /// <summary>
        /// 現在の回転角度
        /// </summary>
        public float CurrentAngle;

        /// <summary>
        /// 累積回転角度（制限チェック用、初期位置からの相対角度）
        /// </summary>
        public float AccumulatedAngle;

        /// <summary>
        /// 現在のプレイヤーステート
        /// </summary>
        public SpinnerPlayerStateType CurrentStateType;

        /// <summary>
        /// ステート持続時間
        /// </summary>
        public float StateDuration;

        /// <summary>
        /// 死亡後のリスポーン待機時間
        /// </summary>
        public float RespawnTimer;

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// プレイヤーの状態タイプ
    /// </summary>
    public enum SpinnerPlayerStateType
    {
        Idle,       // 待機中（ラウンド開始前など）
        Active,     // 通常プレイ中（回転可能）
        Dead,       // 死亡中（リスポーン待ち）
    }
}
