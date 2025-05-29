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

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using KSPBuildTools;
using UnityEngine;

namespace Shabby;

public class MaterialReplacement : ModelFilter
{
	public readonly MaterialDef materialDef = null;
	private readonly Dictionary<Material, Material> replacedMaterials = new();

	public MaterialReplacement(ConfigNode node) : base(node)
	{
		var defName = node.GetValue("materialDef");
		if (string.IsNullOrEmpty(defName)) {
			Log.Error("material replacement must reference a material definition");
			return;
		}

		if (!MaterialDefLibrary.items.TryGetValue(defName, out materialDef)) {
			Log.Error($"failed to find valid material definition {defName}");
		}
	}

	public void ApplyToSharedMaterialIfNotIgnored(Renderer renderer)
	{
		if (MatchIgnored(renderer)) return;
		var sharedMat = renderer.sharedMaterial;
		if (sharedMat == null) return;
		if (!replacedMaterials.TryGetValue(sharedMat, out var replacementMat)) {
			replacementMat = materialDef.Instantiate(sharedMat);
			replacedMaterials[sharedMat] = replacementMat;
		}

		renderer.sharedMaterial = replacementMat;
	}
}

[HarmonyPatch(typeof(PartLoader), "CompileModel")]
internal class MaterialReplacementPatch
{
	private static void Postfix(ref GameObject __result, ConfigNode partCfg)
	{
		const string replacementNodeName = "SHABBY_MATERIAL_REPLACE";
		if (!partCfg.HasNode(replacementNodeName)) return;

		var replacements = new List<MaterialReplacement>();
		foreach (ConfigNode node in partCfg.nodes) {
			if (node.name != replacementNodeName) continue;
			var replacement = new MaterialReplacement(node);
			if (replacement.materialDef != null) replacements.Add(replacement);
		}

		// Apply blanket replacements or material name replacements.
		foreach (var renderer in __result.GetComponentsInChildren<Renderer>()) {
			foreach (var replacement in replacements) {
				if (!replacement.blanketApply && !replacement.MatchMaterial(renderer)) continue;
				replacement.ApplyToSharedMaterialIfNotIgnored(renderer);
				break;
			}
		}

		// Apply transform replacements.
		if (replacements.Any(rep => rep.targetTransforms.Count > 0)) {
			foreach (var transform in __result.GetComponentsInChildren<Transform>()) {
				foreach (var replacement in replacements) {
					if (!replacement.MatchTransform(transform)) continue;
					foreach (var renderer in transform.GetComponentsInChildren<Renderer>()) {
						replacement.ApplyToSharedMaterialIfNotIgnored(renderer);
					}

					break;
				}
			}
		}

		var replacementNames = string.Join(", ", replacements.Select(rep => rep.materialDef.name));
		Log.Debug($"applied material replacements {replacementNames}");
	}
}