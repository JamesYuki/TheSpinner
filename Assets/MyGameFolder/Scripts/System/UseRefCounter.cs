using System;
using System.Threading;

public sealed class UseRefCounter
{
    private int m_count;
    public int Count => Volatile.Read(ref m_count);

    public event Action OnUse;
    public event Action OnReleased;

    public bool IsInUse => Count > 0;

    public IDisposable Use()
    {
        Interlocked.Increment(ref m_count);
        OnUse?.Invoke();
        return new Ref(this);
    }

    private void Release()
    {
        int newCount = Interlocked.Decrement(ref m_count);
        if (newCount < 0)
        {
            Interlocked.Increment(ref m_count);
            throw new InvalidOperationException("Reference count underflow detected.");
        }
        if (newCount == 0)
        {
            OnReleased?.Invoke();
        }
    }

    private sealed class Ref : IDisposable
    {
        private UseRefCounter m_counter;
        private int m_disposed;

        public Ref(UseRefCounter counter)
        {
            m_counter = counter;
            m_disposed = 0;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref m_disposed, 1) == 0)
            {
                m_counter?.Release();
                m_counter = null;
            }
        }
    }
}
