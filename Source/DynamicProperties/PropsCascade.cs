#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KSPBuildTools;
using UnityEngine;

namespace Shabby.DynamicProperties;

internal class PropsCascade(Renderer renderer) : IDisposable
{
	private readonly Renderer renderer = renderer;
	private readonly SortedSet<Props> cascade = new();
	private MpbCompiler? compiler = null;

	internal bool Add(Props props)
	{
		if (!cascade.Add(props)) return false;

		ReacquireCompiler();
		return true;
	}

	internal bool Remove(Props props)
	{
		if (!cascade.Remove(props)) return false;

		ReacquireCompiler();
		return true;
	}

	private void ReacquireCompiler()
	{
		UnregisterFromCompiler();
		compiler = MpbCompilerCache.Get(cascade);
		compiler.Register(renderer);
	}

	private void UnregisterFromCompiler()
	{
		compiler?.Unregister(renderer);
		compiler = null;
	}

	public void Dispose()
	{
		Log.Debug($"disposing cascade instance {RuntimeHelpers.GetHashCode(this)}");
		UnregisterFromCompiler();
		GC.SuppressFinalize(this);
	}

	~PropsCascade()
	{
		UnregisterFromCompiler();
	}
}
