using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wintellect.Sterling.Core
{
	public class AsyncSemaphore
	{
		private readonly static Task<bool> _completed = TaskEx.FromResult(true);
		private readonly Queue<TaskCompletionSource<bool>> _waiters = new Queue<TaskCompletionSource<bool>>();
		private int _currentCount;

		public AsyncSemaphore(int initialCount)
		{
			if (initialCount < 0)
			{
				throw new ArgumentOutOfRangeException("initialCount");
			}
			_currentCount = initialCount;
		}

		public Task<bool> WaitAsync()
		{
			lock (_waiters)
			{
				if (_currentCount > 0)
				{
					_currentCount--;
					return _completed;
				}
				else
				{
					var waiter = new TaskCompletionSource<bool>();
					_waiters.Enqueue(waiter);
					return waiter.Task;
				}
			}
		}

		public void Release()
		{
			TaskCompletionSource<bool> toRelease = null;
			lock (_waiters)
			{
				if (_waiters.Count > 0)
				{
					toRelease = _waiters.Dequeue();
				}
				else
				{
					_currentCount++;
				}
			}
			if (toRelease != null)
			{
				toRelease.SetResult(true);
			}
		}
	}

	/// <summary>AsyncLock locks across one or several await calls.
	/// 
	/// </summary>
	public class AsyncLock
	{
		private readonly AsyncSemaphore _semaphore;
		private readonly Task<Releaser> _releaser;

		public AsyncLock()
		{
			_semaphore = new AsyncSemaphore(1);
			_releaser = TaskEx.FromResult(new Releaser(this));
		}

		public Task<Releaser> LockAsync()
		{
			var wait = _semaphore.WaitAsync();
			var that = this;
			return wait.IsCompleted ?
				_releaser :
				wait.ContinueWith((t) => new Releaser((AsyncLock)that),
					CancellationToken.None,
					TaskContinuationOptions.ExecuteSynchronously,
					TaskScheduler.Default);
		}


		public struct Releaser : IDisposable
		{
			private readonly AsyncLock _toRelease;

			internal Releaser(AsyncLock toRelease)
			{
				_toRelease = toRelease;
			}

			public void Dispose()
			{
				if (_toRelease != null)
				{
					_toRelease._semaphore.Release();
				}
			}
		}
	}
}
