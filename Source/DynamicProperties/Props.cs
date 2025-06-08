using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using KSPBuildTools;
using UnityEngine;

namespace Shabby;

internal static class PropIdToName
{
	private static readonly string[] CommonProperties = [
		"TransparentFX",
		"_BumpMap",
		"_Color",
		"_EmissiveColor",
		"_MainTex",
		"_MaxX",
		"_MaxY",
		"_MinX",
		"_MinY",
		"_Multiplier",
		"_Opacity",
		"_RimColor",
		"_RimFalloff",
		"_TC1Color",
		"_TC1MetalBlend",
		"_TC1Metalness",
		"_TC1SmoothBlend",
		"_TC1Smoothness",
		"_TC2Color",
		"_TC2MetalBlend",
		"_TC2Metalness",
		"_TC2SmoothBlend",
		"_TC2Smoothness",
		"_TemperatureColor",
		"_Tex",
		"_Tint",
		"_TintColor",
		"_subdiv",
		"localMatrix",
		"upMatrix"
	];

	private static readonly Dictionary<int, string> IdToName =
		CommonProperties.ToDictionary(Shader.PropertyToID, name => name);

	internal static string Get(int id) =>
		IdToName.TryGetValue(id, out var name) ? name : $"<{id}>";
}

internal abstract class Prop
{
	internal abstract void Write(int id, MaterialPropertyBlock mpb);
}

internal abstract class Prop<T>(T value) : Prop
{
	internal T Value = value;
	public override string ToString() => Value.ToString();
}

internal class PropColor(Color value) : Prop<Color>(value)
{
	internal override void Write(int id, MaterialPropertyBlock mpb) => mpb.SetColor(id, Value);
}

internal class PropFloat(float value) : Prop<float>(value)
{
	internal override void Write(int id, MaterialPropertyBlock mpb) => mpb.SetFloat(id, Value);
}

internal class PropInt(int value) : Prop<int>(value)
{
	internal override void Write(int id, MaterialPropertyBlock mpb) => mpb.SetInt(id, Value);
}

internal class PropTexture(Texture value) : Prop<Texture>(value)
{
	internal override void Write(int id, MaterialPropertyBlock mpb) => mpb.SetTexture(id, Value);
}

internal class PropVector(Vector4 value) : Prop<Vector4>(value)
{
	internal override void Write(int id, MaterialPropertyBlock mpb) => mpb.SetVector(id, Value);
}

public sealed class Props(int priority)
{
	public readonly int Priority = priority;

	private readonly Dictionary<int, Prop> props = [];

	internal bool Changed = false;

	private static uint _idCounter = 0;
	private static uint _nextId() => _idCounter++;
	private readonly uint uniqueId = _nextId();

	// Note that this is compatible with default object reference equality.
	public static readonly Comparer<Props> PriorityComparer = Comparer<Props>.Create((a, b) =>
	{
		var priorityCmp = a.Priority.CompareTo(b.Priority);
		return priorityCmp != 0 ? priorityCmp : a.uniqueId.CompareTo(b.uniqueId);
	});

	internal IEnumerable<int> ManagedIds => props.Keys;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void _internalSet<T, TProp>(int id, T value) where TProp : Prop<T>
	{
		if (props.TryGetValue(id, out var prop)) {
			if (prop is TProp typedProp) {
				if (EqualityComparer<T>.Default.Equals(value, typedProp.Value)) return;

				typedProp.Value = value;
				Changed = true;
				return;
			}

			MaterialPropertyManager.Instance.LogWarning(
				$"property {PropIdToName.Get(id)} has mismatched type; overwriting with {typeof(T).Name}!");
		}

		props[id] = (TProp)Activator.CreateInstance(typeof(TProp), value);
		Changed = true;
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

		MaterialPropertyManager.Instance.LogDebug(
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
}
