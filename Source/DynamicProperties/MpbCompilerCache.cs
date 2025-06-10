using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KSPBuildTools;
using UnityEngine;

namespace Shabby.DynamicProperties;

internal static class MpbCompilerCache
{
	private static readonly IEqualityComparer<SortedSet<Props>> CacheKeyComparer =
		SortedSet<Props>.CreateSetComparer(); // Object equality is fine.

	private static readonly Dictionary<SortedSet<Props>, MpbCompiler> Cache =
		new(CacheKeyComparer);

	internal static MpbCompiler Get(SortedSet<Props> cascade)
	{
		if (Cache.TryGetValue(cascade, out var compiler)) {
			MaterialPropertyManager.Instance?.LogDebug(
				$"MpbCompiler cache hit instance {RuntimeHelpers.GetHashCode(compiler)}");
			return compiler;
		}

		// Don't accidentally mutate the cache key...
		var clonedCascade = new SortedSet<Props>(cascade);
		compiler = new MpbCompiler(clonedCascade);
#if DEBUG
		if (!(!ReferenceEquals(cascade, compiler.Cascade) &&
		      CacheKeyComparer.Equals(cascade, compiler.Cascade))) {
			throw new InvalidOperationException("cache key equality check failed");
		}
#endif
		Cache[compiler.Cascade] = compiler;
		return compiler;
	}

	internal static void Remove(MpbCompiler entry)
	{
		Cache.Remove(entry.Cascade);
		entry.Dispose();
	}

	internal static void CheckCleared()
	{
		if (Cache.Count == 0) return;

		Debug.LogError($"{Cache.Count} MpbCompilers were not disposed; forcing removal");
		foreach (var compiler in Cache.Values) compiler.Dispose();
		Cache.Clear();
	}
}
