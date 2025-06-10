using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KSPBuildTools;
using UnityEngine;

namespace Shabby.DynamicProperties;

public sealed class Props(int priority) : IDisposable
{
	/// Ordered by lowest to highest priority. Equal priority is disambiguated by unique IDs.
	/// Note that this is compatible with default object reference equality.
	public static readonly Comparer<Props> PriorityComparer = Comparer<Props>.Create((a, b) =>
	{
		var priorityCmp = a.Priority.CompareTo(b.Priority);
		return priorityCmp != 0 ? priorityCmp : a.UniqueId.CompareTo(b.UniqueId);
	});

	private static uint _idCounter = 0;
	private static uint _nextId() => _idCounter++;

	public readonly uint UniqueId = _nextId();

	public readonly int Priority = priority;

	private readonly Dictionary<int, Prop> props = [];

	internal IEnumerable<int> ManagedIds => props.Keys;

	internal delegate void ValueChangedHandler(Props props, int? id);

	internal delegate void EntriesChangedHandler(Props props);

	internal ValueChangedHandler OnValueChanged = delegate { };
	internal EntriesChangedHandler OnEntriesChanged = delegate { };

	internal bool SuppressEagerUpdate = false;
	internal bool NeedsValueUpdate = false;
	internal bool NeedsEntriesUpdate = false;

	public void SuppressEagerUpdatesThisFrame()
	{
		SuppressEagerUpdate = true;
		MaterialPropertyManager.Instance?.ScheduleLateUpdate(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void _internalSet<T, TProp>(int id, T value) where TProp : Prop<T>
	{
		if (props.TryGetValue(id, out var prop)) {
			if (prop is TProp typedProp) {
				if (!typedProp.UpdateIfChanged(value)) return;

				if (!SuppressEagerUpdate) {
					OnValueChanged(this, id);
				} else {
					NeedsValueUpdate = true;
				}

				return;
			}

			MaterialPropertyManager.Instance.LogWarning(
				$"property {PropIdToName.Get(id)} has mismatched type; overwriting with {typeof(T).Name}!");
		}

		props[id] = (TProp)Activator.CreateInstance(typeof(TProp), value);

		if (!SuppressEagerUpdate) {
			OnEntriesChanged(this);
		} else {
			NeedsEntriesUpdate = true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetColor(int id, Color value) => _internalSet<Color, PropColor>(id, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetFloat(int id, float value) => _internalSet<float, PropFloat>(id, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetInt(int id, int value) => _internalSet<int, PropInt>(id, value);

	public void SetTexture(int id, Texture value) => _internalSet<Texture, PropTexture>(id, value);
	public void SetVector(int id, Vector4 value) => _internalSet<Vector4, PropVector>(id, value);

	private bool _internalHas<T>(int id) => props.TryGetValue(id, out var prop) && prop is Prop<T>;

	public bool HasColor(int id) => _internalHas<Color>(id);
	public bool HasFloat(int id) => _internalHas<float>(id);
	public bool HasInt(int id) => _internalHas<int>(id);
	public bool HasTexture(int id) => _internalHas<Texture>(id);
	public bool HasVector(int id) => _internalHas<Vector4>(id);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Write(int id, MaterialPropertyBlock mpb)
	{
		if (!props.TryGetValue(id, out var prop)) {
			throw new KeyNotFoundException($"property {PropIdToName.Get(id)} not found");
		}

		MaterialPropertyManager.Instance?.LogDebug(
			$"writing property {PropIdToName.Get(id)} = {prop}");

		prop.Write(id, mpb);
	}

	public override string ToString()
	{
		var sb = StringBuilderCache.Acquire();
		sb.AppendFormat("(Priority {0}) {{\n", Priority);
		foreach (var (id, prop) in props) {
			sb.AppendFormat("{0} = {1}\n", PropIdToName.Get(id), prop);
		}

		sb.AppendLine("}");
		return sb.ToStringAndRelease();
	}

	private bool _disposed = false;

	private void UnregisterSelf(bool disposing)
	{
		if (_disposed) return;
		Debug.Log($"disposing Props instance {UniqueId}");
		if (disposing) MaterialPropertyManager.Instance?.Remove(this);
		_disposed = true;
	}

	public void Dispose()
	{
		UnregisterSelf(true);
		GC.SuppressFinalize(this);
	}

	~Props()
	{
		UnregisterSelf(false);
	}
}
