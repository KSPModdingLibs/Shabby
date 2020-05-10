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
using UnityEngine;

using Harmony;

namespace Shabby {

	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	class Shabby : MonoBehaviour
	{
		const BindingFlags bindFlags = BindingFlags.NonPublic | BindingFlags.Static;
		public delegate void RMTdelegate (BinaryReader br, Material mat, string name);
		public static RMTdelegate ReadMaterialTexture { get; private set; }

		static Dictionary<string, Shader> loadedShaders;

		public static void AddShader (Shader shader)
		{
			loadedShaders[shader.name] = shader;
		}

		public static Shader FindShader (string shaderName)
		{
			Shader shader;
			if (loadedShaders.TryGetValue (shaderName, out shader)) {
				Debug.Log ($"[Shabby] custom shader :{shader.name}");
				return shader;
			}
			shader = Shader.Find(shaderName);
			return shader;
		}

		void HookReadMaterial(HarmonyInstance harmony)
		{
			var asm = typeof(GameDatabase).Assembly;
			var pr = asm.GetType("PartReader");
			var mp = typeof (MaterialPatch);
			var rmt = pr.GetMethod("ReadMaterialTexture", bindFlags);
			ReadMaterialTexture = (RMTdelegate) Delegate.CreateDelegate (typeof (RMTdelegate), rmt);

			var original = pr.GetMethod("ReadMaterial4", bindFlags);
			var prefix = mp.GetMethod("ReadMaterial4", bindFlags);
			harmony.Patch (original, new HarmonyMethod (prefix), null);
		}

		void Awake ()
		{
			if (loadedShaders == null) {
				loadedShaders = new Dictionary<string, Shader> ();

				HarmonyInstance harmony = HarmonyInstance.Create ("Shabby");
				harmony.PatchAll (Assembly.GetExecutingAssembly ());

				HookReadMaterial (harmony);
			}
		}
	}
}
