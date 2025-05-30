using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using KSPBuildTools;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using MonoMod.Utils;
using UnityEngine;
using Code = Mono.Cecil.Cil.Code;

namespace Shabby;

internal static class ShaderFindOverride
{
	private const string cecilMethodName =
		"UnityEngine.Shader UnityEngine.Shader::Find(System.String)";

	private static readonly MethodInfo mInfo_ShaderFind_Original =
		AccessTools.Method(typeof(Shader), nameof(Shader.Find));

	private static readonly MethodInfo mInfo_ShaderFind_Replacement =
		AccessTools.Method(typeof(Shabby), nameof(Shabby.FindShader));

	private static readonly HarmonyMethod hMethod_CallSiteTranspiler =
		new(AccessTools.Method(typeof(ShaderFindOverride), nameof(CallSiteTranspiler)));

	internal static void ReplaceCallSites(Harmony harmony)
	{
		var callSites = new List<MethodBase>();

		Log.Debug("Beginning search for `Shader.Find` callsites");

		// Don't use appdomain, we don't want to accidentally patch Unity itself and this avoid
		// having to iterate on the BCL and Unity assemblies.
		foreach (var kspAssembly in AssemblyLoader.loadedAssemblies) {
			if (kspAssembly.assembly == Assembly.GetExecutingAssembly())
				continue;

// alternative implementation using Harmony instead of Cecil, but this is like 4x slower
#if false
			foreach (var type in kspAssembly.assembly.GetTypes()) {
				var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
				                              BindingFlags.Instance | BindingFlags.Static |
				                              BindingFlags.DeclaredOnly);
				foreach (var method in methods) {
					try {
						if (method.HasMethodBody() && !method.ContainsGenericParameters &&
						    !method.IsGenericMethod) {
							foreach (var instruction in PatchProcessor.ReadMethodBody(method)) {
								if (instruction.Key == OpCodes.Call) {
									if (ReferenceEquals(instruction.Value,
										    mInfo_ShaderFind_Original)) {
										callSites.Add(method);
										break;
									}
								}
							}
						}
					} catch (Exception ex) {
						Log.Error($"exception while patching {method.Name}: {ex}");
					}
				}
			}
#else
			if (string.IsNullOrEmpty(kspAssembly.assembly?.Location))
				continue;

			AssemblyDefinition assemblyDef;
			try {
				assemblyDef = AssemblyDefinition.ReadAssembly(kspAssembly.assembly.Location);

				if (assemblyDef == null)
					throw new FileLoadException(
						$"Couldn't read assembly \"{kspAssembly.assembly.Location}\"");
			} catch (Exception e) {
				Log.Warning($"Replace failed for assembly {kspAssembly.name}\n{e}");
				continue;
			}

			foreach (var moduleDef in assemblyDef.Modules) {
				foreach (var typeDef in moduleDef.GetAllTypes()) {
					foreach (var methodDef in typeDef.Methods) {
						if (!methodDef.HasBody)
							continue;

						foreach (var instruction in methodDef.Body.Instructions) {
							if (instruction.OpCode.Code == Code.Call
							    && instruction.Operand is MethodReference mRef
							    && mRef.FullName == cecilMethodName) {
								MethodBase callSite;
								try {
									callSite = methodDef.ResolveReflection();

									if (callSite == null)
										throw new MemberAccessException();
								} catch {
									Log.Warning(
										$"Failed to patch method {assemblyDef.Name}::{typeDef.Name}.{methodDef.Name}");
									break;
								}

								callSites.Add(callSite);
								break;
							}
						}
					}
				}
			}

			assemblyDef.Dispose();
#endif
		}

		foreach (var callSite in callSites) {
			if (callSite == mInfo_ShaderFind_Replacement)
				continue;

			try {
				harmony.Patch(callSite, null, null, hMethod_CallSiteTranspiler);
				Log.Debug(
					$"Patching call site: {callSite.DeclaringType.Assembly.GetName().Name}::{callSite.DeclaringType}.{callSite.Name}");
			} catch (Exception e) {
				Log.Warning(
					$"Failed to patch call site: {callSite.DeclaringType.Assembly.GetName().Name}::{callSite.DeclaringType}.{callSite.Name}\n{e.Message}\n{e.StackTrace}");
			}
		}
	}

	private static IEnumerable<CodeInstruction> CallSiteTranspiler(
		IEnumerable<CodeInstruction> instructions)
	{
		foreach (var instruction in instructions) {
			if (instruction.opcode == OpCodes.Call &&
			    ReferenceEquals(instruction.operand, mInfo_ShaderFind_Original))
				instruction.operand = mInfo_ShaderFind_Replacement;

			yield return instruction;
		}
	}
}
