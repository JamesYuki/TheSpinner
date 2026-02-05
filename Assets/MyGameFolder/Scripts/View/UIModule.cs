using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.PlayerLoop;

namespace Spinner
{
    /// <summary>
    /// UIモジュールの基底クラス。Show/Hide/CloseとDisposeのデフォルト実装を提供。
    /// </summary>
    public abstract class UIModule : MonoBehaviour, IUIModule, IDisposable
    {
        private bool m_DisposedUIModule = false;

        public abstract void Initialize();
        public abstract void Complete();

        public virtual async UniTask Show()
        {
            gameObject.SetActive(true);
            await UniTask.CompletedTask;
        }

        public virtual async UniTask Hide()
        {
            gameObject.SetActive(false);
            await UniTask.CompletedTask;
        }

        public virtual async UniTask Close()
        {
            Dispose();
            DestroyImmediate(gameObject);
            await UniTask.CompletedTask;
        }

        public virtual void Dispose()
        {
            if (m_DisposedUIModule) return;
            m_DisposedUIModule = true;
        }
    }
}
