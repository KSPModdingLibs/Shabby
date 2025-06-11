using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Shabby.DynamicProperties;

[HarmonyPatch(typeof(MaterialColorUpdater))]
internal class MaterialColorUpdaterPatch : StockPatchBase<MaterialColorUpdater>
{
	[HarmonyPostfix]
	[HarmonyPatch("CreateRendererList")]
	private static void MaterialColorUpdater_CreateRendererList_Postfix(
		MaterialColorUpdater __instance)
	{
		var props = Props[__instance] = new Props(int.MinValue + 1);
		foreach (var renderer in __instance.renderers) {
			MaterialPropertyManager.Instance?.Set(renderer, props);
		}
	}

	private static void Update_SetProperty(MaterialColorUpdater mcu)
	{
		Props[mcu].SetColor(mcu.propertyID, mcu.setColor);
	}

	[HarmonyTranspiler]
	[HarmonyPatch(nameof(MaterialColorUpdater.Update))]
	private static IEnumerable<CodeInstruction> Update_Transpiler(
		IEnumerable<CodeInstruction> insns)
	{
		var MPB_SetColor = AccessTools.Method(
			typeof(MaterialPropertyBlock),
			nameof(MaterialPropertyBlock.SetColor),
			[typeof(int), typeof(Color)]);

		foreach (var insn in insns) {
			yield return insn;

			// this.mpb.SetColor(this.propertyID, this.setColor);
			// IL_0022: ldarg.0      // this
			// IL_0023: ldfld        class UnityEngine.MaterialPropertyBlock MaterialColorUpdater::mpb
			// IL_0028: ldarg.0      // this
			// IL_0029: ldfld        int32 MaterialColorUpdater::propertyID
			// IL_002e: ldarg.0      // this
			// IL_002f: ldfld        valuetype UnityEngine.Color MaterialColorUpdater::setColor
			// IL_0034: callvirt     instance void UnityEngine.MaterialPropertyBlock::SetColor(int32, valuetype UnityEngine.Color)
			if (insn.Calls(MPB_SetColor)) break;
			// Remaining code applies MPB to renderers.
		}

		// MaterialColorUpdaterPatch.Update_SetProperty(this);
		// return;
		CodeInstruction[] updateProp = [
			new(OpCodes.Ldarg_0), // this
			CodeInstruction.Call(() => Update_SetProperty(default)),
			new(OpCodes.Ret)
		];
		foreach (var insn in updateProp) yield return insn;
	}

	private static void DisposeIfExists(MaterialColorUpdater mcu)
	{
		if (mcu == null) return;
		if (Props.TryGetValue(mcu, out var props)) props.Dispose();
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Part), nameof(Part.ResetMPB))]
	private static void Part_ResetMPB_Prefix(Part __instance)
	{
		DisposeIfExists(__instance.temperatureRenderer);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Part), "OnDestroy")]
	private static void Part_OnDestroy_Postfix(Part __instance)
	{
		DisposeIfExists(__instance.temperatureRenderer);
	}

	[HarmonyTranspiler]
	[HarmonyPatch(typeof(ModuleJettison), nameof(ModuleJettison.Jettison))]
	private static IEnumerable<CodeInstruction> ModuleJettison_Jettison_Transpiler(
		IEnumerable<CodeInstruction> insns)
	{
		var ModuleJettison_jettisonTemperatureRenderer = AccessTools.Field(
			typeof(ModuleJettison), nameof(ModuleJettison.jettisonTemperatureRenderer));

		// this.jettisonTemperatureRenderer = null;
		// IL_0327: ldarg.0      // this
		// IL_0328: ldnull
		// IL_0329: stfld        class MaterialColorUpdater ModuleJettison::jettisonTemperatureRenderer
		CodeMatch[] matchSetTempRendererNull = [
			new(OpCodes.Ldarg_0),
			new(OpCodes.Ldnull),
			new(OpCodes.Stfld, ModuleJettison_jettisonTemperatureRenderer)
		];

		var matcher = new CodeMatcher(insns);

		matcher
			.MatchStartForward(matchSetTempRendererNull)
			.ThrowIfNotMatch("failed to find set temp renderer null")
			.Insert(
				// MaterialColorUpdaterPatch.DisposeIfExists(this.jettisonTemperatureRenderer);
				new CodeInstruction(OpCodes.Ldarg_0), // this
				new CodeInstruction(OpCodes.Ldfld, ModuleJettison_jettisonTemperatureRenderer),
				CodeInstruction.Call(() => DisposeIfExists(default))
			);

		return matcher.InstructionEnumeration();
	}

	// FIXME: write a transpiler for ModuleJettison.Jettison.

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ModuleJettison), "OnDestroy")]
	private static void ModuleJettison_OnDestroy_Postfix(ModuleJettison __instance)
	{
		DisposeIfExists(__instance.jettisonTemperatureRenderer);
	}
}
