using System.Collections.Generic;
using KSPBuildTools;
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

	internal static void ClearCache() => MpbCache.Clear();

	private readonly SortedSet<Props> cascade = new(Props.PriorityComparer);
	private MpbCacheEntry? cacheEntry = null;

	internal bool Add(Props props)
	{
		var added = cascade.Add(props);
		if (added) cacheEntry = null;
		return added;
	}

	internal bool Remove(Props props)
	{
		var removed = cascade.Remove(props);
		if (removed) cacheEntry = null;
		return removed;
	}

	// Should this be a hashset?
	private static readonly List<Props> _changedProps = [];

	internal static void RefreshChangedProps()
	{
		foreach (var (cascade, cacheEntry) in MpbCache) {
			cacheEntry.Changed = false;
			foreach (var props in cascade) {
				if (!props.Changed) continue;
				cacheEntry.Changed = true;
				_changedProps.Add(props);
				foreach (var managedId in cacheEntry.ManagedIds[props]) {
					props.Write(managedId, cacheEntry.Mpb);
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
			// if (cacheEntry.Changed && HighLogic.LoadedSceneIsEditor) {
			// 	Debug.Log(cascade.Aggregate("props:\n", (current, props) => current + props));
			// }

			return cacheEntry.Changed;
		}

		if (!MpbCache.TryGetValue(cascade, out cacheEntry)) {
			MaterialPropertyManager.Instance.LogDebug("building new MPB");
			cacheEntry = BuildCacheEntry(cascade);
		} else {
			MaterialPropertyManager.Instance.LogDebug("MPB cache hit");
		}

		// if (HighLogic.LoadedSceneIsEditor) {
		// 	Debug.Log(cascade.Aggregate("props:\n", (current, props) => current + props));
		// }

		mpb = cacheEntry.Mpb;
		return true;
	}
}
