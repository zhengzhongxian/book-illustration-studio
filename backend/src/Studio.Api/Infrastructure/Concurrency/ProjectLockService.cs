using System.Collections.Concurrent;

namespace Studio.Api.Infrastructure.Concurrency;

public interface IProjectLockService
{
    bool TryAcquire(string projectId, out IDisposable? lockReleaser);
}

public class ProjectLockService : IProjectLockService
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public bool TryAcquire(string projectId, out IDisposable? lockReleaser)
    {
        var semaphore = _locks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
        var acquired = semaphore.Wait(0); // non-blocking attempt

        if (!acquired)
        {
            lockReleaser = null;
            return false;
        }

        lockReleaser = new Releaser(semaphore);
        return true;
    }

    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public Releaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _semaphore.Release();
                _disposed = true;
            }
        }
    }
}
