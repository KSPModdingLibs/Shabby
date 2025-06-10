#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KSPBuildTools;
using UnityEngine;

namespace Shabby.DynamicProperties;

internal class MpbCompiler : IDisposable
{
	#region Fields

	/// Immutable.
	internal readonly SortedSet<Props> Cascade;

	private readonly HashSet<Renderer> linkedRenderers = [];
	private readonly MaterialPropertyBlock mpb = new();
	private readonly Dictionary<Props, List<int>> idManagerMap = [];

	private static readonly MaterialPropertyBlock EmptyMpb = new();

	#endregion

	internal MpbCompiler(SortedSet<Props> cascades)
	{
		MaterialPropertyManager.Instance?.LogDebug(
			$"new cache entry {RuntimeHelpers.GetHashCode(this)}");

		Cascade = cascades;
		RebuildManagerMap();
		RewriteMpb();
		foreach (var props in Cascade) {
			props.OnValueChanged += OnPropsValueChanged;
			props.OnEntriesChanged += OnPropsEntriesChanged;
		}
	}

	#region Renderer registration

	internal void Register(Renderer renderer)
	{
		linkedRenderers.Add(renderer);
		Apply(renderer);
	}

	internal void Unregister(Renderer renderer)
	{
		linkedRenderers.Remove(renderer);
		renderer.SetPropertyBlock(EmptyMpb);
		CheckLiveness();
	}

	private void CheckLiveness()
	{
		if (linkedRenderers.Count > 0) return;
		MaterialPropertyManager.Instance.LogDebug(
			$"dead cache entry {RuntimeHelpers.GetHashCode(this)}");
		PropsCascade.RemoveCacheEntry(this);
	}

	#endregion

	#region Props updates

	private void RebuildManagerMap()
	{
		idManagerMap.Clear();

		Dictionary<int, Props> idManagers = [];
		foreach (var props in Cascade) {
			foreach (var id in props.ManagedIds) {
				idManagers[id] = props;
			}
		}

		foreach (var (id, props) in idManagers) {
			if (!idManagerMap.TryGetValue(props, out var ids)) {
				idManagerMap[props] = ids = [];
			}

			ids.Add(id);
		}
	}

	private void OnPropsValueChanged(Props props)
	{
		WriteMpb(props);
		ApplyAll();
	}

	private void OnPropsEntriesChanged(Props props)
	{
		RebuildManagerMap();
		RewriteMpb();
		ApplyAll();
	}

	#endregion

	#region Apply

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteMpb(Props props)
	{
		foreach (var id in idManagerMap[props]) props.Write(id, mpb);
	}

	private void RewriteMpb()
	{
		mpb.Clear();
		foreach (var props in idManagerMap.Keys) WriteMpb(props);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Apply(Renderer renderer) => renderer.SetPropertyBlock(mpb);

	private readonly List<Renderer> _deadRenderers = [];

	private void ApplyAll()
	{
		foreach (var renderer in linkedRenderers) {
			if (renderer == null) {
				_deadRenderers.Add(renderer!);
				continue;
			}

			Apply(renderer);
		}

		foreach (var dead in _deadRenderers) {
			MaterialPropertyManager.Instance.LogDebug($"dead renderer {dead.GetHashCode()}");
			MaterialPropertyManager.Instance.Remove(dead);
		}

		CheckLiveness();
	}

	#endregion

	#region dtor

	private bool _disposed = false;

	private void UnlinkProps()
	{
		if (_disposed) return;

		Debug.Log("disposing MPB cache entry");

		foreach (var props in Cascade) {
			props.OnValueChanged -= OnPropsValueChanged;
			props.OnEntriesChanged -= OnPropsEntriesChanged;
		}

		_disposed = true;
	}

	public void Dispose()
	{
		UnlinkProps();
		GC.SuppressFinalize(this);
	}

	~MpbCompiler()
	{
		UnlinkProps();
	}

	#endregion
}
