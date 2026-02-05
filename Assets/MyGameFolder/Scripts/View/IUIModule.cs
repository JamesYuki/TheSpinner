using Cysharp.Threading.Tasks;

namespace Spinner
{
    public interface IUIModule
    {
        UniTask Show();
        UniTask Hide();
        UniTask Close();

        abstract void Initialize();
        abstract void Complete();
    }
}
