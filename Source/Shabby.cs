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
using System.Linq;
using System.Reflection;
using HarmonyLib;
using KSPBuildTools;
using UnityEngine;

namespace Shabby;

[KSPAddon(KSPAddon.Startup.Instantly, true)]
public class Shabby : MonoBehaviour
{
	private static Harmony harmony;

	private static readonly Dictionary<string, Shader> shabShaders = [];
	private static readonly Dictionary<string, string> nameReplacements = [];
	private static readonly Dictionary<string, string> iconReplacements = [];

	public static void AddShader(Shader shader)
	{
		shabShaders[shader.name] = shader;
	}

	private static Shader FindLoadedShader(string shaderName)
	{
		if (shabShaders.TryGetValue(shaderName, out var shader)) {
			Log.Debug($"custom shader: `{shader.name}`");
			return shader;
		}

		return Shader.Find(shaderName);
	}

	public static Shader FindShader(string shaderName)
	{
		if (nameReplacements.TryGetValue(shaderName, out var replacementName)) {
			var replacementShader = FindLoadedShader(replacementName);
			if (replacementShader != null) {
				return replacementShader;
			}

			Log.Error($"failed to find replacement shader `{replacementName}` for `{shaderName}`");
		}

		return FindLoadedShader(shaderName);
	}

	public static Shader TryFindIconShader(string shaderName)
	{
		if (!iconReplacements.TryGetValue(shaderName, out var iconShaderName)) {
			return null;
		}

		var iconShader = FindShader(iconShaderName);
		Log.Debug($"custom icon shader `{shaderName}` -> `{iconShader.name}`");
		return iconShader;
	}

	private void Awake()
	{
		harmony = new Harmony("Shabby");
		Log.Message("Harmony patching");
		foreach (var type in Assembly.GetExecutingAssembly().GetTypes()) {
			try {
				PatchClassProcessor processor = new(harmony, type);
				if (processor.Patch() is not List<MethodInfo> patchedMethods) continue;
				if (patchedMethods.Count == 0) {
					Log.Message($"`{type.Name}` skipped");
					continue;
				}

				Log.Message(
					$"`{type.Name}` patched methods {string.Join(", ", patchedMethods.Select(m => $"`{m.Name}`"))}");
			} catch (Exception e) {
				Log.Error($"encountered exception while applying `{type.Name}`:\n{e}");
			}
		}

		// Register as an explicit MM callback such that it is run before all reflected
		// callbacks (as used by most mods), which may wish to access the MaterialDef library.
		var addPostPatchCB =
			AccessTools.Method("ModuleManager.MMPatchLoader:AddPostPatchCallback");
		var delegateType = addPostPatchCB.GetParameters()[0].ParameterType;
		var callbackDelegate =
			Delegate.CreateDelegate(delegateType, typeof(Shabby), nameof(MMPostLoad));
		addPostPatchCB.Invoke(null, [callbackDelegate]);
	}

	private static void LoadConfigs(Dictionary<string, string> library,
		ConfigNode entries, string entryName, string fromKey, string toKey)
	{
		foreach (var entry in entries.GetNodes(entryName)) {
			var from = entry.GetValue(fromKey);
			var to = entry.GetValue(toKey);
			if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) {
				Log.Error($"invalid {entryName} specification `{from}` ->`{to}`");
			} else {
				library[from] = to;
			}
		}
	}

	private static void MMPostLoad()
	{
		foreach (var shabbyNode in GameDatabase.Instance.GetConfigNodes("SHABBY")) {
			LoadConfigs(nameReplacements, shabbyNode, "REPLACE", "name", "shader");
			LoadConfigs(iconReplacements, shabbyNode, "ICON_SHADER", "shader", "iconShader");
		}

		MaterialReplacement.MaterialDefLibrary.Load();
	}

	private void Start()
	{
		ShaderFindOverride.ReplaceCallSites(harmony);
	}
}
