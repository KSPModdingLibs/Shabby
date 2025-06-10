using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Shabby.DynamicProperties;

[HarmonyPatch(typeof(Part))]
internal static class PartPatch
{
	private static readonly Dictionary<Part, Props> rimHighlightProps = [];

	[HarmonyPostfix]
	[HarmonyPatch("Awake")]
	private static void Awake_Postfix(Part __instance)
	{
		rimHighlightProps[__instance] = new Props(int.MinValue + 1);
	}

	[HarmonyPostfix]
	[HarmonyPatch("CreateRendererLists")]
	private static void CreateRendererLists_Postfix(Part __instance)
	{
		var props = rimHighlightProps[__instance];
		props.SetFloat(PropertyIDs._RimFalloff, 2f);
		props.SetColor(PropertyIDs._RimColor, Part.defaultHighlightNone);
		foreach (var renderer in __instance.HighlightRenderer) {
			MaterialPropertyManager.Instance?.Set(renderer, props);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(nameof(Part.SetOpacity))]
	private static bool SetOpacity_Prefix(Part __instance, float opacity)
	{
		__instance.CreateRendererLists();
		__instance.mpb.SetFloat(PropertyIDs._Opacity, opacity);
		rimHighlightProps[__instance].SetFloat(PropertyIDs._Opacity, opacity);
		return false;
	}

	private static void Highlight_SetRimColor(Part part, Color color)
	{
		rimHighlightProps[part].SetColor(PropertyIDs._RimColor, color);
	}

	[HarmonyTranspiler]
	[HarmonyPatch(nameof(Part.Highlight), typeof(Color))]
	private static IEnumerable<CodeInstruction> Highlight_Transpiler(
		IEnumerable<CodeInstruction> insns)
	{
		var MPB_SetColor = AccessTools.Method(
			typeof(MaterialPropertyBlock),
			nameof(MaterialPropertyBlock.SetColor),
			[typeof(int), typeof(Color)]);
		var Part_get_mpb = AccessTools.PropertyGetter(typeof(Part), nameof(Part.mpb));
		var Part_highlightRenderer =
			AccessTools.Field(typeof(Part), nameof(Part.highlightRenderer));
		var PropertyIDs__RimColor =
			AccessTools.Field(typeof(PropertyIDs), nameof(PropertyIDs._RimColor));
		var Renderer_SetPropertyBlock = AccessTools.Method(
			typeof(Renderer),
			nameof(Renderer.SetPropertyBlock),
			[typeof(MaterialPropertyBlock)]);

		CodeMatch[] matchDupPop = [new(OpCodes.Dup), new(OpCodes.Pop)];

		// mpb.SetColor(PropertyIDs._RimColor, value);
		// IL_0049: ldarg.0      // this
		// IL_004a: call         instance class UnityEngine.MaterialPropertyBlock Part::get_mpb()
		// IL_004f: ldsfld       int32 PropertyIDs::_RimColor
		// IL_0054: ldloc.0      // color
		// IL_0055: callvirt     instance void UnityEngine.MaterialPropertyBlock::SetColor(int32, valuetype UnityEngine.Color)
		CodeMatch[] matchSetRimColor = [
			new(OpCodes.Ldarg_0),
			new(OpCodes.Call, Part_get_mpb),
			new(OpCodes.Ldsfld, PropertyIDs__RimColor),
			new(OpCodes.Ldloc_0),
			new(OpCodes.Callvirt, MPB_SetColor)
		];

		// highlightRenderer[count].SetPropertyBlock(mpb);
		// IL_008a: ldarg.0      // this; jump target
		// IL_008b: ldfld        class System.Collections.Generic.List`1<class UnityEngine.Renderer> Part::highlightRenderer
		// IL_0090: ldloc.1      // count
		// IL_0091: callvirt     instance !0/*class UnityEngine.Renderer*/ class System.Collections.Generic.List`1<class UnityEngine.Renderer>::get_Item(int32)
		// IL_0096: ldarg.0      // this
		// IL_0097: call         instance class UnityEngine.MaterialPropertyBlock Part::get_mpb()
		// IL_009c: callvirt     instance void UnityEngine.Renderer::SetPropertyBlock(class UnityEngine.MaterialPropertyBlock)
		CodeMatch[] matchSetMpb = [
			new(OpCodes.Ldarg_0),
			new(OpCodes.Ldfld, Part_highlightRenderer),
			new(OpCodes.Ldloc_1),
			new(OpCodes.Callvirt), // can't easily specify indexer...
			new(OpCodes.Ldarg_0),
			new(OpCodes.Call, Part_get_mpb),
			new(OpCodes.Callvirt, Renderer_SetPropertyBlock)
		];

		var matcher = new CodeMatcher(insns);
		matcher
			.MatchStartForward(matchDupPop)
			.Repeat(cm => cm.RemoveInstructions(matchDupPop.Length))
			.Start()
			.MatchStartForward(matchSetRimColor)
			.ThrowIfNotMatch("failed to find MPB set _RimColor call")
			.RemoveInstructions(matchSetRimColor.Length)
			.InsertAndAdvance(
				// PartPatch.Highlight_SetRimColor(this, value);
				new CodeInstruction(OpCodes.Ldarg_0), // `this`
				new CodeInstruction(OpCodes.Ldloc_0), // `value`
				CodeInstruction.Call(() => Highlight_SetRimColor(default, default)))
			.MatchStartForward(matchSetMpb)
			.ThrowIfNotMatch("failed to find Renderer.SetMPB call")
			// No need to replace application, since that is automatic.
			.SetAndAdvance(OpCodes.Nop, null) // preserve label
			.RemoveInstructions(matchSetMpb.Length - 1);
		return matcher.InstructionEnumeration();
	}

	[HarmonyPostfix]
	[HarmonyPatch("OnDestroy")]
	private static void OnDestroy_Postfix(Part __instance)
	{
		if (rimHighlightProps.Remove(__instance, out var props)) {
			props.Dispose();
		}
	}
}
