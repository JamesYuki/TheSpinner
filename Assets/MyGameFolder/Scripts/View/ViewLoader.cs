using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

namespace JPS
{
    public static class ViewLoader
    {
        /// <summary>
        /// Addressables.Load & Instantiate し、ViewModelをセットする拡張メソッド（非同期）。
        /// </summary>
        /// <typeparam name="TView">View(MonoBehaviour)の型</typeparam>
        /// <typeparam name="TViewModel">ViewModelの型</typeparam>
        /// <param name="address">Addressablesのアドレス</param>
        /// <param name="viewModel">セットするViewModel</param>
        /// <returns>生成されたView</returns>
        public static async UniTask<TView> LoadViewAsync<TView, TViewModel>(string address, TViewModel viewModel)
            where TView : MonoBehaviour, IViewWithViewModel<TViewModel>
        {
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Prefab not found at address: {address}");
                return null;
            }
            var instance = Object.Instantiate(handle.Result);
            var view = instance.GetComponent<TView>();
            if (view == null)
            {
                Debug.LogError($"Component {typeof(TView).Name} not found on prefab.");
                return null;
            }
            view.SetViewModel(viewModel);
            return view;
        }
    }

    /// <summary>
    /// ViewModelをセットできるView用インターフェース
    /// </summary>
    public interface IViewWithViewModel<TViewModel>
    {
        void SetViewModel(TViewModel viewModel);
    }
}
