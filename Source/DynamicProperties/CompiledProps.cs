using System.Collections.Generic;
using UnityEngine;

namespace Shabby;

#nullable enable

internal class MpbCacheEntry
{
	internal readonly MaterialPropertyBlock Mpb = new();
	internal readonly Dictionary<Props, List<int>> ManagedIds = [];
}

internal class CompiledProps
{
	private static readonly IEqualityComparer<SortedSet<Props>> CascadeKeyComparer =
		SortedSet<Props>.CreateSetComparer();

	// FIXME: clear old entries...
	private static readonly Dictionary<SortedSet<Props>, MpbCacheEntry> mpbCache =
		new(CascadeKeyComparer);

	internal static void Clear() => mpbCache.Clear();

	private readonly SortedSet<Props> cascade = new(Props.PriorityComparer);

	internal bool Add(Props props)
	{
		cachedMpb = null;
		return cascade.Add(props);
	}

	private MaterialPropertyBlock? cachedMpb = null;

	// Should this be a hashset?
	private static readonly List<Props> _dirtyProps = [];

	internal static void UpdateDirtyProps()
	{
		foreach (var (cascade, cache) in mpbCache) {
			foreach (var props in cascade) {
				if (!props.Dirty) continue;
				_dirtyProps.Add(props);
				foreach (var managedId in cache.ManagedIds[props]) {
					props.Write(managedId, cache.Mpb);
				}
			}
		}

		foreach (var props in _dirtyProps) props.Dirty = false;
		_dirtyProps.Clear();
	}

	internal MaterialPropertyBlock Get()
	{
		if (cachedMpb != null) return cachedMpb;

		if (!mpbCache.TryGetValue(cascade, out var cacheEntry)) {
			mpbCache[cascade] = cacheEntry = new MpbCacheEntry();

			Dictionary<int, Props> idManagers = [];
			foreach (var props in cascade) {
				foreach (var id in props.ManagedIds) {
					idManagers[id] = props;
				}
			}

			foreach (var (id, props) in idManagers) {
				if (!cacheEntry.ManagedIds.TryGetValue(props, out var ids)) {
					cacheEntry.ManagedIds[props] = ids = [];
				}

				ids.Add(id);
				props.Write(id, cacheEntry.Mpb);
			}
		}

		cachedMpb = cacheEntry.Mpb;
		return cachedMpb;
	}
}
