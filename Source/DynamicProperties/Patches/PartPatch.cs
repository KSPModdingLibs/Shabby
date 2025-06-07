using System.Collections.Generic;
using HarmonyLib;

namespace Shabby;

[HarmonyPatch(typeof(Part))]
internal static class PartPatch
{
	private static readonly Dictionary<Part, Props> highlightProperties = [];

	[HarmonyPostfix]
	[HarmonyPatch("Awake")]
	private static void Awake_Postfix(Part __instance)
	{
		highlightProperties[__instance] = new Props(int.MinValue + 1);
	}

	[HarmonyPostfix]
	[HarmonyPatch("CreateRendererLists")]
	private static void CreateRendererLists_Postfix(Part __instance)
	{
		var props = highlightProperties[__instance];
		props.SetFloat(PropertyIDs._RimFalloff, 2f);
		props.SetColor(PropertyIDs._RimColor, Part.defaultHighlightNone);
		foreach (var renderer in __instance.HighlightRenderer) {
			MaterialPropertyManager.Instance.Set(renderer, props);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(nameof(Part.SetOpacity))]
	private static bool SetOpacity_Prefix(Part __instance, float opacity)
	{
		__instance.CreateRendererLists();
		__instance.mpb.SetFloat(PropertyIDs._Opacity, opacity);
		highlightProperties[__instance].SetFloat(PropertyIDs._Opacity, opacity);
		return false;
	}

	[HarmonyPostfix]
	[HarmonyPatch("OnDestroy")]
	private static void OnDestroy_Postfix(Part __instance)
	{
		highlightProperties.Remove(__instance);
	}
}
