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
				Debug.Log($"[Shabby]: invalid shader normal map property specification {shader} = {property}");
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

	[Persistent(name = "shader")] public string shaderName;
	public Shader shader;

	public Dictionary<string, bool> keywords;
	public Dictionary<string, float> floats;
	public Dictionary<string, Color> colors;
	public Dictionary<string, Texture> textures = new Dictionary<string, Texture>();

	public MaterialDef(ConfigNode node)
	{
		ConfigNode.LoadObjectFromConfig(this, node);

		shader = Shabby.FindShader(shaderName);
		if (shader == null) {
			Debug.LogError($"[Shabby]: failed to find shader {shaderName}");
		}

		keywords = LoadDictionary<bool>(node.GetNode("Keyword"));

		floats = LoadDictionary<float>(node.GetNode("Float"));

		colors = LoadDictionary<Color>(node.GetNode("Color"));

		var textureNames = LoadDictionary<string>(node.GetNode("Texture"));
		foreach (var kvp in textureNames) {
			var texInfo = GameDatabase.Instance.GetTextureInfo(kvp.Value);
			if (texInfo == null)
			{
				Debug.LogError($"[Shabby]: failed to find texture {kvp.Value}");
				continue;
			}

			MaterialDefLibrary.normalMapProperties.TryGetValue(shaderName, out var nrmPropertyName);
			var isNormalMap = kvp.Key == (nrmPropertyName ?? "_BumpMap");

			textures[kvp.Key] = isNormalMap ? texInfo.normalMap : texInfo.texture;
		}
	}

	static readonly Func<Type, string, object> ReadValue =
		AccessTools.MethodDelegate<Func<Type, string, object>>(
			AccessTools.DeclaredMethod(typeof(ConfigNode), "ReadValue"));

	static Dictionary<string, T> LoadDictionary<T>(ConfigNode node)
	{
		var items = new Dictionary<string, T>();
		if (node == null) return items;

		foreach (ConfigNode.Value item in node.values) {
			object value = ReadValue(typeof(T), item.value);
			if (value is T parsed) {
				items[item.name] = parsed;
			} else {
				Debug.LogError($"[Shabby]: failed to parse property {item.name} = {item.value} as a {typeof(T).Name}");
			}
		}

		return items;
	}

	public void ApplyTo(Material material)
	{
		if (shader != null) material.shader = shader;

		foreach (var kvp in keywords) {
			if (kvp.Value) material.EnableKeyword(kvp.Key);
			else material.DisableKeyword(kvp.Key);
		}

		foreach (var kvp in floats) material.SetFloat(kvp.Key, kvp.Value);

		foreach (var kvp in colors) material.SetColor(kvp.Key, kvp.Value);

		foreach (var kvp in textures) material.SetTexture(kvp.Key, kvp.Value);
	}
}

}
