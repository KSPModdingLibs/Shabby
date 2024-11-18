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
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using KSPBuildTools;
using UnityEngine;

namespace Shabby
{
	[HarmonyPatch(typeof(PartLoader), "SetPartIconMaterials")]
	class SetPartIconMaterialsPatch
	{
		static MethodInfo mInfo_ShaderFind = AccessTools.Method(typeof(Shader), nameof(Shader.Find));

		static MethodInfo mInfo_FindOverrideIconShader =
			AccessTools.Method(typeof(SetPartIconMaterialsPatch), nameof(FindOverrideIconShader));

		static Shader FindOverrideIconShader(Material material)
		{
			if (Shabby.iconShaders.TryGetValue(material.shader.name, out var shader)) {
				Log.Debug($"custom icon shader {material.shader.name} -> {shader.name}");
				return shader;
			}

			return Shabby.FindShader("KSP/ScreenSpaceMask");
		}

		/// <summary>
		/// The stock method iterates through every material in the icon prefab and replaces some
		/// stock shaders with 'ScreenSpaceMask'-prefixed ones. All shaders not explicitly checked,
		/// including custom shaders, are replaced with 'KSP/ScreenSpaceMask'.
		/// This transpiler inserts logic to check for additional replacements.
		/// </summary>
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var code = instructions.ToList();
			object locMaterial = null;

			for (var i = 0; i < code.Count; ++i) {
				// Material material = sharedMaterials[j];
				// IL_002C ldloc.3
				// IL_002D ldloc.s 4
				// IL_002F ldelem.ref
				// IL_0030 stloc.s 6
				if (locMaterial == null
				    && code[i].opcode == OpCodes.Ldloc_3
				    && code[i + 1].opcode == OpCodes.Ldloc_S
				    && code[i + 2].opcode == OpCodes.Ldelem_Ref
				    && code[i + 3].opcode == OpCodes.Stloc_S) {
					// Extract the stack index of the material local.
					locMaterial = code[i + 3].operand;
				}

				// material2 = new Material(Shader.Find("KSP/ScreenSpaceMask"));
				// IL_0191 ldstr "KSP/ScreenSpaceMask"
				// IL_0196 call class UnityEngine.Shader UnityEngine.Shader::Find(string)
				// IL_019D newobj instance void UnityEngine.Material::.ctor(class UnityEngine.Shader)
				// IL_01A2 stloc.s 7
				if (code[i].Is(OpCodes.Ldstr, "KSP/ScreenSpaceMask") && code[i + 1].Calls(mInfo_ShaderFind)) {
					// Replace the call to Shader.Find with FindOverrideIconShader(material).
					if (locMaterial == null) break;
					code[i].opcode = OpCodes.Ldloc_S;
					code[i].operand = locMaterial;
					code[i + 1].operand = mInfo_FindOverrideIconShader;
					Log.Debug("patched part icon shader replacement");
					return code;
				}
			}

			Log.Error("failed to patch part icon shader replacement");
			return code;
		}
	}
}