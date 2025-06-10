using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Shabby.DynamicProperties;

[HarmonyPatch(typeof(MaterialColorUpdater))]
public class MaterialColorUpdaterPatch
{
	internal static readonly Dictionary<MaterialColorUpdater, Props> temperatureColorProps = [];

	[HarmonyPostfix]
	[HarmonyPatch(MethodType.Constructor, typeof(Transform), typeof(int), typeof(Part))]
	private static void MaterialColorUpdater_Ctor_Postfix(MaterialColorUpdater __instance)
	{
		temperatureColorProps[__instance] = new Props(int.MinValue + 1);
	}

	[HarmonyPostfix]
	[HarmonyPatch("CreateRendererList")]
	private static void MaterialColorUpdater_CreateRendererList_Postfix(
		MaterialColorUpdater __instance)
	{
		var props = temperatureColorProps[__instance];
		foreach (var renderer in __instance.renderers) {
			MaterialPropertyManager.Instance?.Set(renderer, props);
		}
	}

	private static void Update_SetProperty(MaterialColorUpdater mcu)
	{
		temperatureColorProps[mcu].SetColor(mcu.propertyID, mcu.setColor);
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

			// IL_0022: ldarg.0      // this
			// IL_0023: ldfld        class UnityEngine.MaterialPropertyBlock MaterialColorUpdater::mpb
			// IL_0028: ldarg.0      // this
			// IL_0029: ldfld        int32 MaterialColorUpdater::propertyID
			// IL_002e: ldarg.0      // this
			// IL_002f: ldfld        valuetype UnityEngine.Color MaterialColorUpdater::setColor
			// IL_0034: callvirt     instance void UnityEngine.MaterialPropertyBlock::SetColor(int32, valuetype UnityEngine.Color)
			if (insn.Calls(MPB_SetColor)) break;
		}

		CodeInstruction[] replace = [
			new(OpCodes.Ldarg_0), // this
			CodeInstruction.Call(() => Update_SetProperty(default)),
			new(OpCodes.Ret)
		];
		foreach (var insn in replace) yield return insn;
	}

	private static void DisposeIfExists(MaterialColorUpdater mcu)
	{
		if (mcu == null) return;
		if (temperatureColorProps.TryGetValue(mcu, out var props)) props.Dispose();
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

	// FIXME: write a transpiler for ModuleJettison.Jettison.

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ModuleJettison), "OnDestroy")]
	private static void ModuleJettison_OnDestroy_Postfix(ModuleJettison __instance)
	{
		DisposeIfExists(__instance.jettisonTemperatureRenderer);
	}
}
