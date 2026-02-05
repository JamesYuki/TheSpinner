using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace JPS
{
    public class BaseGameScene : MonoBehaviour, IAsyncDisposable
    {
        protected bool m_IsSceneFinished = false;

        public void FinishScene()
        {
            m_IsSceneFinished = true;
        }
        public async UniTask PushScene(CancellationToken token)
        {
            await using (this)
            {
                await CreateScene(token);
                await Process(token);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DestroyScene();
        }

        /// <summary>
        /// Addressables経由でViewをロードしViewModelをバインドする簡易ラッパー
        /// </summary>
        protected async Task<TView> LoadViewAsync<TView, TViewModel>(string address, TViewModel viewModel)
            where TView : MonoBehaviour, IViewWithViewModel<TViewModel>
        {
            return await ViewLoader.LoadViewAsync<TView, TViewModel>(address, viewModel);
        }

        /// <summary>
        /// シーン生成時の処理（public、内部で非同期実装を呼ぶ）
        /// </summary>
        private async UniTask CreateScene(CancellationToken token)
        {
            await CreateSceneImpl(token);
        }

        /// <summary>
        /// シーン生成時の非同期実装（protected）
        /// </summary>
        protected virtual async UniTask CreateSceneImpl(CancellationToken token) { await UniTask.CompletedTask; }
        /// <summary>
        /// シーンのメイン処理（public、非同期）
        /// </summary>
        protected virtual async UniTask Process(CancellationToken token)
        {
            await WaitForSceneFinish(token);
        }

        /// <summary>
        /// シーン終了まで待機する補助メソッド。派生クラスで明示的に呼び出せる。
        /// </summary>
        protected async UniTask WaitForSceneFinish(CancellationToken token)
        {
            while (!m_IsSceneFinished && !token.IsCancellationRequested)
            {
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// シーン破棄時の処理（public、内部で非同期実装を呼ぶ）
        /// </summary>
        private async UniTask DestroyScene()
        {
            await DestroySceneImpl();
        }

        /// <summary>
        /// シーン破棄時の非同期実装（protected）
        /// </summary>
        protected virtual async UniTask DestroySceneImpl() { await UniTask.CompletedTask; }
    }
}
