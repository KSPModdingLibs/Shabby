/*
This file is part of Shabby.

Shabby is free software: you can redistribute it and/or
modify it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Shabby is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Shabby.  If not, see
<http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using HarmonyLib;
using KSPBuildTools;
using UnityEngine;

namespace Shabby.MaterialReplacement;

public static class MaterialDefLibrary
{
	public static readonly Dictionary<string, MaterialDef> items = new();

	public static void Load()
	{
		foreach (var node in GameDatabase.Instance.GetConfigNodes("SHABBY_MATERIAL_DEF")) {
			var def = new MaterialDef(node);
			if (string.IsNullOrEmpty(def.name) || !def.isValid) {
				Log.Error($"[MaterialDef {def.name}] removing invalid definition");
			} else {
				items[def.name] = def;
			}
		}
	}
}

public class MaterialDef : ILogContextProvider
{
	[Persistent] public string name;

	[Persistent(name = nameof(displayName))]
	private string _displayName = null;

	public string displayName => _displayName ?? name;

	[Persistent] public bool updateExisting = true;

	[Persistent(name = "shader")] public string shaderName = null;
	public readonly Shader shader = null;

	[Persistent] public bool preserveRenderQueue = false;

	public readonly Dictionary<string, bool> keywords;
	public readonly Dictionary<string, float> floats;
	public readonly Dictionary<string, Color> colors;
	public readonly Dictionary<string, Vector4> vectors;
	public readonly Dictionary<string, Texture> textures;

	public readonly bool isValid = true;

	public MaterialDef(ConfigNode node)
	{
		ConfigNode.LoadObjectFromConfig(this, node);

		if (shaderName != null) {
			shader = Shabby.FindShader(shaderName);
			if (shader == null) {
				this.LogError($"failed to find shader {shaderName}");
				isValid = false;
			}
		}

		if (!updateExisting && shader == null) {
			this.LogError($"from-scratch material must define a valid shader");
			isValid = false;
		}

		keywords = LoadDictionary<bool>(node, "KEYWORD");
		floats = LoadDictionary<float>(node, "FLOAT");
		colors = LoadDictionary<Color>(
			node, "COLOR",
			value => ParseColor(value, out var color) ? color : null);
		vectors = LoadDictionary<Vector4>(node, "VECTOR");
		textures = LoadDictionary<Texture>(
			node, "TEXTURE",
			value => GameDatabase.Instance.GetTexture(value, asNormalMap: false));
	}

	private static readonly Func<Type, string, object> ReadValue =
		AccessTools.MethodDelegate<Func<Type, string, object>>(
			AccessTools.DeclaredMethod(typeof(ConfigNode), "ReadValue"));

	private Dictionary<string, T> LoadDictionary<T>(ConfigNode defNode, string propKind,
		Func<string, object> parser = null)
	{
		var items = new Dictionary<string, T>();

		var propNode = defNode.GetNode(propKind);
		if (propNode == null) return items;

		foreach (ConfigNode.Value item in propNode.values) {
			var value = parser != null ? parser(item.value) : ReadValue(typeof(T), item.value);
			if (value is T parsed) {
				items[item.name] = parsed;
			} else {
				this.LogError($"failed to load {propKind} property {item.name} = {item.value}");
			}
		}

		this.LogMessage($"loaded {items.Count} {propKind} properties");
		return items;
	}

	public static bool ParseColor(string value, out Color color)
	{
		if (ColorUtility.TryParseHtmlString(value, out color)) return true;
		if (ParseExtensions.TryParseColor(value, out color)) return true;
		return false;
	}

	private bool CheckProperty(Material mat, string propName)
	{
		var exists = mat.HasProperty(propName);
		if (!exists) this.LogWarning($"shader {mat.shader.name} does not have property {propName}");
		return exists;
	}

	/// <summary>
	/// Create a new material based on this definition. The material name is copied from the
	/// passed reference material. In update-existing mode, all properties are also copied from
	/// the reference material.
	/// </summary>
	public Material Instantiate(Material referenceMaterial)
	{
		if (!isValid) return new Material(referenceMaterial);

		Material material;
		if (updateExisting) {
			material = new Material(referenceMaterial);
			if (shader != null) material.shader = shader;
		} else {
			material = new Material(shader) { name = referenceMaterial.name };
		}

		// Replacing the shader resets the render queue to the shader's default.
		if (preserveRenderQueue) material.renderQueue = referenceMaterial.renderQueue;

		foreach (var kvp in keywords) {
			if (!CheckProperty(material, kvp.Key)) continue;
			if (kvp.Value) material.EnableKeyword(kvp.Key);
			else material.DisableKeyword(kvp.Key);
		}

		foreach (var kvp in floats) {
			if (CheckProperty(material, kvp.Key)) material.SetFloat(kvp.Key, kvp.Value);
		}

		foreach (var kvp in colors) {
			if (CheckProperty(material, kvp.Key)) material.SetColor(kvp.Key, kvp.Value);
		}

		foreach (var kvp in vectors) {
			if (CheckProperty(material, kvp.Key)) material.SetVector(kvp.Key, kvp.Value);
		}

		foreach (var kvp in textures) {
			if (CheckProperty(material, kvp.Key)) material.SetTexture(kvp.Key, kvp.Value);
		}

		return material;
	}

	public string context() => $"MaterialDef {name}";
}
