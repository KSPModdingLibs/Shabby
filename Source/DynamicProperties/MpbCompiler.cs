#nullable enable

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KSPBuildTools;
using UnityEngine;

namespace Shabby.DynamicProperties;

internal class MpbCompiler : Disposable
{
	#region Fields

	/// Immutable.
	internal readonly SortedSet<Props> Cascade;

	private readonly HashSet<Renderer> linkedRenderers = [];
	private readonly MaterialPropertyBlock mpb = new();
	private readonly Dictionary<int, Props> idManagers = [];

	private static readonly MaterialPropertyBlock EmptyMpb = new();

	#endregion

	internal MpbCompiler(SortedSet<Props> cascade)
	{
		MaterialPropertyManager.Instance?.LogDebug(
			$"new MpbCompiler instance {RuntimeHelpers.GetHashCode(this)}");

		Cascade = cascade;
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
		if (renderer != null) renderer.SetPropertyBlock(EmptyMpb);

		if (linkedRenderers.Count > 0) return;
		Log.Debug(
			$"last renderer unregistered from MpbCompiler instance {RuntimeHelpers.GetHashCode(this)}");
		MpbCompilerCache.Remove(this);
	}

	#endregion

	#region Props updates

	private void RebuildManagerMap()
	{
		idManagers.Clear();
		foreach (var props in Cascade) {
			foreach (var id in props.ManagedIds) {
				idManagers[id] = props;
			}
		}
	}

	private void OnPropsEntriesChanged(Props props)
	{
		RebuildManagerMap();
		RewriteMpb();
		ApplyAll();
	}

	private void OnPropsValueChanged(Props props, int? id)
	{
		WriteMpb(props, id);
		ApplyAll();
	}

	#endregion

	#region Apply

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteMpb(Props props, int? id)
	{
		if (id.HasValue) {
			var changedId = id.GetValueOrDefault();
			if (idManagers[changedId] != props) return;
			props.Write(changedId, mpb);
		} else {
			foreach (var (managedId, managingProps) in idManagers) {
				if (props != managingProps) continue;
				props.Write(managedId, mpb);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void RewriteMpb()
	{
		mpb.Clear();
		foreach (var (id, props) in idManagers) props.Write(id, mpb);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Apply(Renderer renderer) => renderer.SetPropertyBlock(mpb);

	private void ApplyAll()
	{
		var hasDeadRenderer = false;

		foreach (var renderer in linkedRenderers) {
			if (renderer != null) {
				Apply(renderer);
			} else {
				hasDeadRenderer = true;
			}
		}

		if (hasDeadRenderer) MaterialPropertyManager.Instance?.CheckRemoveDeadRenderers();
	}

	#endregion

	protected override bool IsUnused() => linkedRenderers.Count == 0;

	protected override void OnDispose()
	{
		foreach (var props in Cascade) {
			props.OnEntriesChanged -= OnPropsEntriesChanged;
			props.OnValueChanged -= OnPropsValueChanged;
		}
	}
}
