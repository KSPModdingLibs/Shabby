using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using Highlighting;
using KSPBuildTools;
using UnityEngine;

namespace Shabby;

// [HarmonyPatch(typeof(Renderer))]
// internal static class MaterialAccessWatchdog
// {
// 	[HarmonyPatch(nameof(Renderer.materials), MethodType.Getter)]
// 	[HarmonyPostfix]
// 	internal static void Renderer_materials_get_Postfix()
// 	{
// 		var trace = new StackTrace();
// 		foreach (var frame in trace.GetFrames()!) {
// 			var type = frame.GetMethod()?.DeclaringType;
// 			if (type == typeof(KSP.UI.Screens.EditorPartIcon) ||
// 			    type == typeof(PSystemManager) ||
// 			    type == typeof(PSystemSetup) ||
// 			    type == typeof(Upgradeables.UpgradeableObject) ||
// 			    type == typeof(KSP.UI.Screens.Flight.NavBall)) {
// 				return;
// 			}
// 		}
//
// 		Log.Debug($"Called `Renderer.materials`\n{trace}");
// 	}
// }

[HarmonyPatch]
internal static class NoDuplicateMaterials
{
	private static readonly MethodInfo mInfo_Renderer_material_get =
		AccessTools.PropertyGetter(typeof(Renderer), nameof(Renderer.material));

	private static readonly MethodInfo mInfo_Renderer_materials_get =
		AccessTools.PropertyGetter(typeof(Renderer), nameof(Renderer.materials));

	private static readonly MethodInfo mInfo_Renderer_sharedMaterial_get =
		AccessTools.PropertyGetter(typeof(Renderer), nameof(Renderer.sharedMaterial));

	private static readonly MethodInfo mInfo_Renderer_sharedMaterials_get =
		AccessTools.PropertyGetter(typeof(Renderer), nameof(Renderer.sharedMaterials));

	private static IEnumerable<MethodBase> TargetMethods() => [
		AccessTools.Method(typeof(Highlighter), "GrabRenderers"),
		AccessTools.Method(typeof(MaterialColorUpdater), "CreateRendererList"),
		AccessTools.Method(typeof(ModuleColorChanger), "ProcessMaterialsList"),
		AccessTools.Method(
			typeof(GameObjectExtension), nameof(GameObjectExtension.SetLayerRecursive),
			[typeof(GameObject), typeof(int), typeof(bool), typeof(int)])
	];

	[HarmonyTranspiler]
	internal static IEnumerable<CodeInstruction> MaterialToSharedMaterialTranspiler(
		MethodBase targetMethod, IEnumerable<CodeInstruction> instructions)
	{
		foreach (var insn in instructions) {
			if (insn.Calls(mInfo_Renderer_material_get)) {
				insn.operand = mInfo_Renderer_sharedMaterial_get;
				Log.Debug("patched `Renderer.material` getter");
			} else if (insn.Calls(mInfo_Renderer_materials_get)) {
				insn.operand = mInfo_Renderer_sharedMaterials_get;
				Log.Debug("patched `Renderer.materials` getter");
			}

			yield return insn;
		}
	}
}
