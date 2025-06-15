using System;
using KSPBuildTools;

namespace Shabby.DynamicProperties;

public abstract class Disposable : IDisposable
{
	protected virtual bool IsUnused() => false;

	protected abstract void OnDispose();

	private bool _disposed = false;

	private void HandleDispose(bool disposing)
	{
		if (_disposed) return;

		if (disposing) {
			Log.Debug($"disposing {GetType().Name} instance {GetHashCode()}");
			OnDispose();
		} else if (!IsUnused()) {
			Log.Warning(
				$"active {GetType().Name} instance {GetHashCode()} was not disposed");
		}

		_disposed = true;
	}

	public void Dispose()
	{
		HandleDispose(true);
		GC.SuppressFinalize(this);
	}

	~Disposable()
	{
		HandleDispose(false);
	}
}
