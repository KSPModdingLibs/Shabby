using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KSPBuildTools;
using UnityEngine;

namespace Shabby;

internal abstract class Prop;

internal class Prop<T>(T value) : Prop
{
	internal T Value = value;

	public override string ToString() => Value.ToString();
}

public sealed class Props(int priority)
{
	public readonly int Priority = priority;

	private readonly Dictionary<int, Prop> _props = [];

	internal bool Changed = false;

	private static uint _idCounter = 0;
	private static uint _nextId() => _idCounter++;
	private readonly uint _uniqueId = _nextId();

	// Note that this is compatible with default object reference equality.
	public static readonly Comparer<Props> PriorityComparer = Comparer<Props>.Create((a, b) =>
	{
		var priorityCmp = a.Priority.CompareTo(b.Priority);
		return priorityCmp != 0 ? priorityCmp : a._uniqueId.CompareTo(b._uniqueId);
	});

	internal IEnumerable<int> ManagedIds => _props.Keys;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void _internalSet<T>(int id, T value)
	{
		Dirty = true;

		MaterialPropertyManager.Instance.LogDebug($"setting {id} to {value}");

		if (!_props.TryGetValue(id, out var prop)) {
			_props[id] = new Prop<T>(value);
			Changed = true;
			return;
		}

		if (prop is not Prop<T> propT) {
			MaterialPropertyManager.Instance.LogWarning(
				$"property {id} has mismatched type; overwriting with {typeof(T).Name}!");
			_props[id] = new Prop<T>(value);
			Changed = true;
			return;
		}

		if (EqualityComparer<T>.Default.Equals(value, propT.Value)) return;

		propT.Value = value;
		Changed = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetColor(int id, Color value) => _internalSet<Color>(id, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetFloat(int id, float value) => _internalSet<float>(id, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetInt(int id, int value) => _internalSet<int>(id, value);

	public void SetTexture(int id, Texture value) => _internalSet<Texture>(id, value);
	public void SetVector(int id, Vector4 value) => _internalSet<Vector4>(id, value);

	private bool _internalHas<T>(int id) => _props.TryGetValue(id, out var prop) && prop is Prop<T>;

	public bool HasColor(int id) => _internalHas<Color>(id);
	public bool HasFloat(int id) => _internalHas<float>(id);
	public bool HasInt(int id) => _internalHas<int>(id);
	public bool HasTexture(int id) => _internalHas<Texture>(id);
	public bool HasVector(int id) => _internalHas<Vector4>(id);

	internal void Write(int id, MaterialPropertyBlock mpb)
	{
		if (!_props.TryGetValue(id, out var prop)) {
			throw new KeyNotFoundException($"property {id} not found");
		}

		switch (prop) {
			case Prop<Color> c: mpb.SetColor(id, c.Value); break;
			case Prop<float> f: mpb.SetFloat(id, f.Value); break;
			case Prop<int> i: mpb.SetInt(id, i.Value); break;
			case Prop<Texture> t: mpb.SetTexture(id, t.Value); break;
			case Prop<Vector4> v: mpb.SetVector(id, v.Value); break;
		}
	}

	public override string ToString()
	{
		var sb = StringBuilderCache.Acquire();
		sb.AppendFormat("(Priority {0}) {{\n", Priority);
		foreach (var (id, prop) in _props) {
			sb.AppendFormat("{0} = {1}\n", id, prop);
		}

		sb.AppendLine("}");
		return sb.ToStringAndRelease();
	}
}
