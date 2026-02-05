namespace Spinner
{
    /// <summary>
    /// ダメージを受けることができるオブジェクトのインターフェース
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// ダメージを受ける
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        /// <param name="source">ダメージ源（オプション）</param>
        void TakeDamage(float damage, object source = null);

        /// <summary>
        /// 現在の体力を取得
        /// </summary>
        float CurrentHealth { get; }

        /// <summary>
        /// 最大体力を取得
        /// </summary>
        float MaxHealth { get; }

        /// <summary>
        /// 生存しているか
        /// </summary>
        bool IsAlive { get; }
    }
}
