#nullable enable

using System;
using System.Collections.Generic;
using KSPBuildTools;
using UnityEngine;

namespace Shabby.DynamicProperties;

internal class PropsCascade(Renderer renderer)
{
	internal readonly Renderer Renderer = renderer;

	private static readonly IEqualityComparer<SortedSet<Props>> CacheKeyComparer =
		SortedSet<Props>.CreateSetComparer(); // Object equality is fine.

	private static readonly Dictionary<SortedSet<Props>, MpbCompiler> MpbCache =
		new(CacheKeyComparer);

	internal static void ClearCache() => MpbCache.Clear();

	private readonly SortedSet<Props> cascade = new(Props.PriorityComparer);
	private MpbCompiler? mpbCompiler = null;

	internal static void RemoveCacheEntry(MpbCompiler entry)
	{
		MpbCache.Remove(entry.Cascade);
		entry.Dispose();
	}

	private static readonly List<MpbCompiler> _entriesToRemove = [];

	internal static void RemoveCacheEntriesWith(Props props)
	{
		foreach (var entry in MpbCache) {
			if (entry.Key.Contains(props)) _entriesToRemove.Add(entry.Value);
		}

		foreach (var entry in _entriesToRemove) RemoveCacheEntry(entry);
		_entriesToRemove.Clear();
	}

	private void ReacquireCompiler()
	{
		mpbCompiler?.Unregister(Renderer);

		if (!MpbCache.TryGetValue(cascade, out mpbCompiler)) {
			MaterialPropertyManager.Instance.LogDebug("building new cache entry");

			// Don't accidentally mutate the cache key...
			var clonedCascade = new SortedSet<Props>(cascade, Props.PriorityComparer);
			mpbCompiler = new MpbCompiler(clonedCascade);
#if DEBUG
			if (!(!ReferenceEquals(cascade, mpbCompiler.Cascade) &&
			      CacheKeyComparer.Equals(cascade, mpbCompiler.Cascade))) {
				throw new InvalidOperationException("cache key equality check failed");
			}
#endif
			MpbCache[mpbCompiler.Cascade] = mpbCompiler;
		} else {
			MaterialPropertyManager.Instance.LogDebug("cache hit");
		}

		mpbCompiler.Register(Renderer);
	}

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
}
