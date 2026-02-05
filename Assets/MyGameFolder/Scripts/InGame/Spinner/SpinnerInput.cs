using PurrNet.Prediction;

namespace Spinner
{
    /// <summary>
    /// スピナープレイヤーの入力データ
    /// </summary>
    public struct SpinnerInput : IPredictedData
    {
        /// <summary>
        /// 回転方向 (-1: 左回転, 0: 停止, 1: 右回転)
        /// A/Dキーまたは左右矢印キーで制御
        /// </summary>
        public float RotationDirection;

        /// <summary>
        /// ブレーキ入力（回転を急停止）
        /// </summary>
        public bool Brake;

        public void Dispose()
        {
        }
    }
}
