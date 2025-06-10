using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shabby.DynamicProperties;

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

	internal abstract bool UpdateIfChanged(T value);
	public override string ToString() => Value.ToString();
}

internal class PropColor(Color value) : Prop<Color>(value)
{
	internal override bool UpdateIfChanged(Color value)
	{
		if (Utils.ApproxEquals(value, Value)) return false;
		Value = value;
		return true;
	}

	internal override void Write(int id, MaterialPropertyBlock mpb) => mpb.SetColor(id, Value);
}

internal class PropFloat(float value) : Prop<float>(value)
{
	internal override bool UpdateIfChanged(float value)
	{
		if (Utils.ApproxEqualsRel(value, Value)) return false;
		Value = value;
		return true;
	}

	internal override void Write(int id, MaterialPropertyBlock mpb) => mpb.SetFloat(id, Value);
}

internal class PropInt(int value) : Prop<int>(value)
{
	internal override bool UpdateIfChanged(int value)
	{
		if (value == Value) return false;
		Value = value;
		return true;
	}

	internal override void Write(int id, MaterialPropertyBlock mpb) => mpb.SetInt(id, Value);
}

internal class PropTexture(Texture value) : Prop<Texture>(value)
{
	internal override bool UpdateIfChanged(Texture value)
	{
		if (ReferenceEquals(value, Value)) return false;
		Value = value;
		return true;
	}

	internal override void Write(int id, MaterialPropertyBlock mpb) => mpb.SetTexture(id, Value);
}

internal class PropVector(Vector4 value) : Prop<Vector4>(value)
{
	internal override bool UpdateIfChanged(Vector4 value)
	{
		if (Utils.ApproxEqualsRel(value, Value)) return false;
		Value = value;
		return true;
	}

	internal override void Write(int id, MaterialPropertyBlock mpb) => mpb.SetVector(id, Value);
}
