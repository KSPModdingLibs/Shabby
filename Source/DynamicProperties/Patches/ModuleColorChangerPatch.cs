using System.Collections.Generic;
using HarmonyLib;

namespace Shabby.DynamicProperties;

[HarmonyPatch(typeof(ModuleColorChanger))]
internal class ModuleColorChangerPatch
{
	internal static readonly Dictionary<ModuleColorChanger, Props> mccProps = [];

	[HarmonyPostfix]
	[HarmonyPatch(nameof(ModuleColorChanger.OnStart))]
	private static void OnStart_Postfix(ModuleColorChanger __instance)
	{
		mccProps[__instance] = new Props(0);
	}

	[HarmonyPostfix]
	[HarmonyPatch("EditRenderers")]
	private static void EditRenderers_Postfix(ModuleColorChanger __instance)
	{
		var props = mccProps[__instance];
		foreach (var renderer in __instance.renderers) {
			MaterialPropertyManager.Instance?.Set(renderer, props);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch("UpdateColor")]
	public static bool UpdateColor_Prefix(ModuleColorChanger __instance)
	{
		mccProps[__instance].SetColor(__instance.shaderPropertyInt, __instance.color);
		return false;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Part), "OnDestroy")]
	private static void Part_OnDestroy_Postfix(Part __instance)
	{
		foreach (var mcc in __instance.FindModulesImplementing<ModuleColorChanger>()) {
			if (mccProps.Remove(mcc, out var props)) props.Dispose();
		}
	}

	// FIXME: are part modules destroyed in other places? Icon renderers? Drag cube renderers?
}
