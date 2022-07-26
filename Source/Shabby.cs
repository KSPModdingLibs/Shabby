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
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Utils;
using Code = Mono.Cecil.Cil.Code;

namespace Shabby {

	struct Replacement
	{
		public Replacement(ConfigNode node)
		{
			name = node.GetValue(nameof(name));
			shader = node.GetValue(nameof(shader));
		}

		public string name;
		public string shader;
	}

	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class Shabby : MonoBehaviour
	{
		static Dictionary<string, Shader> loadedShaders;

		static readonly Dictionary<string, Replacement> nameReplacements = new Dictionary<string, Replacement>();

		public static void AddShader (Shader shader)
		{
			loadedShaders[shader.name] = shader;
		}

		static Shader FindLoadedShader(string shaderName)
		{
			Shader shader;
			if (loadedShaders.TryGetValue (shaderName, out shader)) {
				Debug.Log ($"[Shabby] custom shader: {shader.name}");
				return shader;
			}
			shader = Shader.Find(shaderName);
			//if (shader != null) {
			//	Debug.Log ($"[Shabby] stock shader: {shader.name}");
			//}
			return shader;
		}

		public static Shader FindShader(string shaderName)
		{
			Shader shader = null;
			if (nameReplacements.TryGetValue (shaderName, out var replacement))
			{
				shader = FindLoadedShader(replacement.shader);

				if (shader == null)
				{
					Debug.LogError($"[Shabby] failed to find shader {replacement.shader} to replace {shaderName}");
				}
			}

			if (shader == null)
			{
				shader = FindLoadedShader(shaderName);
			}

			return shader;
		}

		public static void ModuleManagerPostLoad()
		{
			var configNodes = GameDatabase.Instance.GetConfigNodes("SHABBY");
			foreach (var shabbyNode in configNodes)
			{
				var replacementNodes = shabbyNode.GetNodes("REPLACE");
				foreach (var replacementNode in replacementNodes)
				{
					Replacement replacement = new Replacement(replacementNode);
					
					nameReplacements[replacement.name] = replacement;
				}
			}
		}

		void Awake ()
		{
			if (loadedShaders == null) {
				loadedShaders = new Dictionary<string, Shader> ();

				var harmony = new Harmony("Shabby");
				harmony.PatchAll(Assembly.GetExecutingAssembly());

				Debug.Log($"[Shabby] hooked");
			}
		}


        private static MethodInfo mInfo_ShaderFind_Original;
        private static MethodInfo mInfo_ShaderFind_Replacement;

        private void Start()
        {
            string cecilMethodName = "UnityEngine.Shader UnityEngine.Shader::Find(System.String)";
            mInfo_ShaderFind_Original = AccessTools.Method(typeof(Shader), nameof(Shader.Find));
            mInfo_ShaderFind_Replacement = AccessTools.Method(typeof(Shabby), nameof(Shabby.FindShader));

            List<MethodBase> callSites = new List<MethodBase>();

            // Don't use appdomain, we don't want to accidentally patch Unity itself and this avoid
            // having to iterate on the BCL and Unity assemblies.
            foreach (AssemblyLoader.LoadedAssembly kspAssembly in AssemblyLoader.loadedAssemblies)
            {
                if (kspAssembly.assembly == Assembly.GetExecutingAssembly())
                    continue;

                if (string.IsNullOrEmpty(kspAssembly.assembly?.Location))
                    continue;

                AssemblyDefinition assemblyDef;
                try
                {
                    assemblyDef = AssemblyDefinition.ReadAssembly(kspAssembly.assembly.Location);

                    if (assemblyDef == null)
                        throw new FileLoadException($"Couldn't read assembly \"{kspAssembly.assembly.Location}\"");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Shabby] Replace failed for assembly {kspAssembly.name}\n{e}");
                    continue;
                }

                foreach (ModuleDefinition moduleDef in assemblyDef.Modules)
                {
                    foreach (TypeDefinition typeDef in moduleDef.GetAllTypes())
                    {
                        foreach (MethodDefinition methodDef in typeDef.Methods)
                        {
                            if (!methodDef.HasBody)
                                continue;

                            foreach (Instruction instruction in methodDef.Body.Instructions)
                            {
                                if (instruction.OpCode.Code == Code.Call
                                    && instruction.Operand is MethodReference mRef
                                    && mRef.FullName == cecilMethodName)
                                {
                                    MethodBase callSite;
                                    try
                                    {
                                        callSite = methodDef.ResolveReflection();

                                        if (callSite == null)
                                            throw new MemberAccessException();
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.LogWarning($"[Shabby] Failed to patch method {assemblyDef.Name}::{typeDef.Name}.{methodDef.Name}");
                                        break;
                                    }

                                    callSites.Add(callSite);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            Harmony harmony = new Harmony("Shabby");
            MethodInfo callSiteTranspiler = AccessTools.Method(typeof(Shabby), nameof(Shabby.CallSiteTranspiler));

            foreach (MethodBase callSite in callSites)
            {
                if (callSite == mInfo_ShaderFind_Replacement)
                    continue;

                Debug.Log($"[Shabby] Patching call site : {callSite.DeclaringType.Assembly.GetName().Name}::{callSite.DeclaringType}.{callSite.Name}");
                harmony.Patch(callSite, null, null, new HarmonyMethod(callSiteTranspiler));
            }
        }

        static IEnumerable<CodeInstruction> CallSiteTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && ReferenceEquals(instruction.operand, mInfo_ShaderFind_Original))
                    instruction.operand = mInfo_ShaderFind_Replacement;

                yield return instruction;
            }
        }
    }
}