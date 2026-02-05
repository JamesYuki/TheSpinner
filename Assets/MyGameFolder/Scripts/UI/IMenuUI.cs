using Cysharp.Threading.Tasks;

namespace Spinner.UI
{
    /// <summary>
    /// UIのロード先を用途で分けるインターフェース
    /// </summary>
    public interface IMenuUI
    {
        /// <summary>
        /// 背景UIとしてロード
        /// </summary>
        UniTask<UIModule> LoadViewToBackgroundAsync(string address);

        /// <summary>
        /// 前面UIとしてロード
        /// </summary>
        UniTask<UIModule> LoadViewToForwardAsync(string address);
    }
}
