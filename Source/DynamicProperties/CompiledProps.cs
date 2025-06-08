using System.Collections.Generic;
using UnityEngine;

namespace Shabby.DynamicProperties;

#nullable enable

internal class MpbCacheEntry
{
	internal readonly MaterialPropertyBlock Mpb = new();
	internal readonly Dictionary<Props, List<int>> ManagedIds = [];
	internal bool Changed = true;
}

internal class CompiledProps
{
	private static readonly IEqualityComparer<SortedSet<Props>> CascadeKeyComparer =
		SortedSet<Props>.CreateSetComparer();

	// FIXME: clear old entries...
	private static readonly Dictionary<SortedSet<Props>, MpbCacheEntry> MpbCache =
		new(CascadeKeyComparer);

	internal static void Clear() => MpbCache.Clear();

	private readonly SortedSet<Props> cascade = new(Props.PriorityComparer);

	internal bool Add(Props props)
	{
		var added = cascade.Add(props);
		if (added) cacheEntry = null;
		return added;
	}

	private MpbCacheEntry? cacheEntry = null;

	// Should this be a hashset?
	private static readonly List<Props> _changedProps = [];

	internal static void RefreshChangedProps()
	{
		foreach (var (cascade, cache) in MpbCache) {
			cache.Changed = false;
			foreach (var props in cascade) {
				if (!props.Changed) continue;
				cache.Changed = true;
				_changedProps.Add(props);
				foreach (var managedId in cache.ManagedIds[props]) {
					props.Write(managedId, cache.Mpb);
				}
			}
		}

		foreach (var props in _changedProps) props.Changed = false;
		_changedProps.Clear();
	}

	private static MpbCacheEntry BuildCacheEntry(SortedSet<Props> cascade)
	{
		var clonedCascade = new SortedSet<Props>(cascade, Props.PriorityComparer);
		var entry = MpbCache[clonedCascade] = new MpbCacheEntry();

		Dictionary<int, Props> idManagers = [];
		foreach (var props in cascade) {
			foreach (var id in props.ManagedIds) {
				idManagers[id] = props;
			}
		}

		foreach (var (id, props) in idManagers) {
			if (!entry.ManagedIds.TryGetValue(props, out var ids)) {
				entry.ManagedIds[props] = ids = [];
			}

			ids.Add(id);
			props.Write(id, entry.Mpb);
		}

		return entry;
	}

	internal bool GetIfChanged(out MaterialPropertyBlock? mpb)
	{
		if (cacheEntry != null) {
			mpb = cacheEntry.Changed ? cacheEntry.Mpb : null;
			return cacheEntry.Changed;
		}

		if (!MpbCache.TryGetValue(cascade, out cacheEntry)) {
			Debug.Log("cache not hit");
			cacheEntry = BuildCacheEntry(cascade);
		} else {
			Debug.Log("cache hit!");
		}

		mpb = cacheEntry.Mpb;
		return true;
	}
}
