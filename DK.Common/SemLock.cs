using System;
using System.Threading;
using System.Threading.Tasks;

namespace DK
{
    public class SemLock : IDisposable
    {
        private SemaphoreSlim _sem;

        public SemLock()
        {
            _sem = new SemaphoreSlim(1, 1);
        }

        public void Dispose()
        {
            _sem?.Dispose();
            _sem = null;
        }

        public async Task WaitAsync() => await _sem.WaitAsync();

        public void Release() => _sem.Release();

        public async Task<WaitInstance> LockAsync()
        {
            await _sem.WaitAsync();
            return new WaitInstance(this);
        }

        public class WaitInstance : IDisposable
        {
            private SemLock _sl;

            public WaitInstance(SemLock sl)
            {
                _sl = sl;
            }

            public void Dispose()
            {
                _sl._sem.Release();
            }
        }

    }
}
