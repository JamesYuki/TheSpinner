using Cysharp.Threading.Tasks;

namespace JPS
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
