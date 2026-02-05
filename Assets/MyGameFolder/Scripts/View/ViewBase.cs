using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

namespace JPS
{
    /// <summary>
    /// ViewModelをセットできるViewの基底クラス（抽象）
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class ViewBase<TViewModel> : UIModule, IViewWithViewModel<TViewModel>
    {
        private CanvasGroup _canvasGroup;

        private CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup == null)
                    _canvasGroup = GetComponent<CanvasGroup>();
                return _canvasGroup;
            }
        }

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public override async UniTask Show()
        {
            gameObject.SetActive(true);
            CanvasGroup.alpha = 1f;
            await UniTask.CompletedTask;
        }

        public override async UniTask Hide()
        {
            CanvasGroup.alpha = 0f;
            await UniTask.CompletedTask;
        }

        protected TViewModel ViewModel { get; private set; }
        private bool m_DisposedViewBase = false;

        public virtual void SetViewModel(TViewModel viewModel)
        {
            ViewModel = viewModel;
            OnViewModelSet();
        }

        /// <summary>
        /// ViewModelセット時の追加処理があればオーバーライド
        /// </summary>
        protected virtual void OnViewModelSet() { }

        /// <summary>
        /// ViewやViewModelのリソース解放
        /// </summary>
        public override void Dispose()
        {
            if (m_DisposedViewBase) return;
            if (ViewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }
            m_DisposedViewBase = true;
            base.Dispose();
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }
    }
}
