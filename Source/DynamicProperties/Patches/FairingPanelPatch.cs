using HarmonyLib;
using ProceduralFairings;

namespace Shabby.DynamicProperties;

[HarmonyPatch(typeof(FairingPanel))]
internal class FairingPanelPatch : StockPatchBase<FairingPanel>
{
	[HarmonyPrefix]
	[HarmonyPatch(nameof(FairingPanel.SetOpacity))]
	private static bool SetOpacity_Transpiler(FairingPanel __instance, float o)
	{
		__instance.opacity = o;

		if (!Props.TryGetValue(__instance, out var props)) {
			props = Props[__instance] = new Props(0);
			MaterialPropertyManager.Instance?.Set(__instance.mr, props);
			if (__instance.attachedFlagParts is { Count: > 0 }) {
				foreach (var flagPart in __instance.attachedFlagParts) {
					foreach (var flagRenderer in flagPart.flagMeshRenderers) {
						MaterialPropertyManager.Instance?.Set(flagRenderer, props);
					}
				}
			}
		}

		props.SetFloat(PropertyIDs._Opacity, o);

		return false;
	}

	[HarmonyPostfix]
	[HarmonyPatch(nameof(FairingPanel.Despawn))]
	private static void FairingPanel_Despawn(FairingPanel __instance)
	{
		if (Props.Remove(__instance, out var props)) props.Dispose();
	}
}
