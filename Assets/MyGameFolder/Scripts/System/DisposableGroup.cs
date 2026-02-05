using System;
using System.Collections.Generic;

namespace Spinner.System
{
    /// <summary>
    /// 複数のIDisposableをまとめて管理・一括Disposeできるクラス。
    /// </summary>
    public sealed class DisposableGroup : IDisposable
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private bool _disposed;

        public void Add(IDisposable disposable)
        {
            if (disposable == null) return;
            if (_disposed)
                throw new ObjectDisposedException(nameof(DisposableGroup));
            _disposables.Add(disposable);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var d in _disposables)
            {
                d?.Dispose();
            }
            _disposables.Clear();
        }
    }
}
