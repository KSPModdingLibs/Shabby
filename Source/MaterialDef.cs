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
using UnityEngine;

namespace Shabby
{

public static class MaterialDefLibrary
{
	public static readonly Dictionary<string, string> normalMapProperties = new Dictionary<string, string>();
	public static readonly Dictionary<string, MaterialDef> items = new Dictionary<string, MaterialDef>();

	public static void Load()
	{
		foreach (var node in GameDatabase.Instance.GetConfigNodes("SHABBY_SHADER_NORMAL_MAP_PROPERTY")) {
			var shader = node.GetValue("shader");
			var property = node.GetValue("property");

			if (string.IsNullOrEmpty(shader) || string.IsNullOrEmpty(property)) {
				Debug.Log($"[Shabby] invalid shader normal map property specification {shader} = {property}");
			} else {
				normalMapProperties[shader] = property;
			}
		}

		foreach (var node in GameDatabase.Instance.GetConfigNodes("SHABBY_MATERIAL_DEF")) {
			var def = new MaterialDef(node);
			items[def.name] = def;
		}
	}
}

public class MaterialDef
{
	[Persistent] public string name;
	[Persistent(name = nameof(displayName))] private string _displayName = null;
	public string displayName => _displayName ?? name;

	[Persistent] public bool updateExisting = true;

	[Persistent(name = "shader")] public string shaderName = null;
	public Shader shader = null;

	[Persistent] public bool preserveRenderQueue = false;

	public Dictionary<string, bool> keywords;
	public Dictionary<string, float> floats;
	public Dictionary<string, Color> colors;
	public Dictionary<string, Vector4> vectors;
	public Dictionary<string, string> textureNames;

	readonly Dictionary<string, Texture> textures = new Dictionary<string, Texture>();

	public readonly bool isValid = true;

	public MaterialDef(ConfigNode node)
	{
		ConfigNode.LoadObjectFromConfig(this, node);

		if (shaderName != null) {
			shader = Shabby.FindShader(shaderName);
			if (shader == null) {
				Debug.LogError($"[Shabby][MaterialDef {name}] failed to find shader {shaderName}");
				isValid = false;
			}
		}

		if (!updateExisting && shader == null) {
			Debug.LogError($"[Shabby][MaterialDef {name}] from-scratch material must define a valid shader");
			isValid = false;
		}

		keywords = LoadDictionary<bool>(node.GetNode("Keyword"));
		floats = LoadDictionary<float>(node.GetNode("Float"));
		colors = LoadDictionary<Color>(node.GetNode("Color"), ParseColor);
		vectors = LoadDictionary<Vector4>(node.GetNode("Vector"));
		textureNames = LoadDictionary<string>(node.GetNode("Texture"));
	}

	static readonly Func<Type, string, object> ReadValue =
		AccessTools.MethodDelegate<Func<Type, string, object>>(
			AccessTools.DeclaredMethod(typeof(ConfigNode), "ReadValue"));

	Dictionary<string, T> LoadDictionary<T>(ConfigNode node, Func<string, object> parser = null)
	{
		var items = new Dictionary<string, T>();
		if (node == null) return items;

		foreach (ConfigNode.Value item in node.values) {
			object value = parser != null ? parser(item.value) : ReadValue(typeof(T), item.value);
			if (value is T parsed) {
				items[item.name] = parsed;
			} else {
				Debug.LogError($"[Shabby][MaterialDef {name}] failed to parse property {item.name} = {item.value} as a {typeof(T).Name}");
			}
		}

		return items;
	}

	static object ParseColor(string value)
	{
		if (ColorUtility.TryParseHtmlString(value, out var color)) return color;
		if (ParseExtensions.TryParseColor(value, out color)) return color;
		return null;
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
			if (kvp.Value) material.EnableKeyword(kvp.Key);
			else material.DisableKeyword(kvp.Key);
		}

		foreach (var kvp in floats) material.SetFloat(kvp.Key, kvp.Value);

		foreach (var kvp in colors) material.SetColor(kvp.Key, kvp.Value);

		foreach (var kvp in vectors) material.SetVector(kvp.Key, kvp.Value);

		foreach (var kvp in textureNames) {
			var (propName, texName) = (kvp.Key, kvp.Value);
			if (!textures.TryGetValue(texName, out var texture)) {
				var texInfo = GameDatabase.Instance.GetTextureInfo(texName);
				if (texInfo == null)
				{
					Debug.LogError($"[Shabby] failed to find texture {texName}");
					continue;
				}

				MaterialDefLibrary.normalMapProperties.TryGetValue(material.shader.name, out var nrmPropName);
				var isNormalMap = propName == (nrmPropName ?? "_BumpMap");

				texture = isNormalMap ? texInfo.normalMap : texInfo.texture;
				textures[texName] = texture;
			}

			material.SetTexture(propName, texture);
		}

		return material;
	}
}

}
