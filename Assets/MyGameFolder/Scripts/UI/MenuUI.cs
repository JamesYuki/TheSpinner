using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace Spinner.UI
{
    public class MenuUI : BaseUIModule, IMenuUI
    {
        /// <summary>
        /// 背景UIの親
        /// </summary>
        public Transform BackgroundParent { get; set; }

        /// <summary>
        /// 前面UIの親
        /// </summary>
        public Transform ForwardParent { get; set; }

        protected override void Awake()
        {
            base.Awake();
            ServiceLocator.Register<IMenuUI>(this);

            // 背景用
            var bgObj = GameObject.Find("BackgroundRoot");
            if (bgObj != null)
            {
                BackgroundParent = bgObj.transform;
            }
            else
            {
                AppLogger.LogError("BackgroundRootがシーンに存在しません。BackgroundParentの設定に失敗しました。");
            }

            // 前面用
            var fwObj = GameObject.Find("ForwardRoot");
            if (fwObj != null)
            {
                ForwardParent = fwObj.transform;
            }
            else
            {
                AppLogger.LogError("ForwardRootがシーンに存在しません。ForwardParentの設定に失敗しました。");
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ServiceLocator.Unregister<IMenuUI>();
        }

        /// <summary>
        /// 背景UIとしてロード
        /// </summary>
        public async UniTask<UIModule> LoadViewToBackgroundAsync(string address)
        {
            return await LoadViewInternalAsync(address, BackgroundParent != null ? BackgroundParent : this.transform);
        }

        /// <summary>
        /// 前面UIとしてロード
        /// </summary>
        public async UniTask<UIModule> LoadViewToForwardAsync(string address)
        {
            return await LoadViewInternalAsync(address, ForwardParent != null ? ForwardParent : this.transform);
        }

        /// <summary>
        /// 内部共通処理
        /// </summary>
        private async UniTask<UIModule> LoadViewInternalAsync(string address, Transform parent)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(address);
            await handle.Task;

            var go = handle.Result;
            if (go == null)
            {
                AppLogger.LogError($"Addressables: プレハブがロードできません: {address}");
                return null;
            }

            var instance = Object.Instantiate(go, parent);
            if (instance == null)
            {
                AppLogger.LogError("Instantiate失敗");
                return null;
            }

            var module = instance.GetComponent<UIModule>();
            if (module == null)
            {
                AppLogger.LogError("UIModuleが見つかりません");
                return null;
            }

            // ViewModelのセットは行わない（Prefab側のViewModelを利用）
            return module;
        }
    }
}
