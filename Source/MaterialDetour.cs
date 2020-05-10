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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Shabby {

	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class MaterialDetour : MonoBehaviour
	{
		static BindingFlags bindFlags = BindingFlags.NonPublic | BindingFlags.Static;
		void Awake ()
		{
			var asm = typeof(GameDatabase).Assembly;
			var pr = asm.GetType("PartReader");
			if (pr != null) {
				Debug.Log ($"[Shabby] shabbily found {pr}");
			}
			MethodInfo oldMaterial = pr.GetMethod("ReadMaterial4", bindFlags);
			MethodInfo newMaterial = typeof(MaterialDetour).GetMethod("ReadMaterial4", bindFlags);
			if (oldMaterial != null) {
				Debug.Log ($"[Shabby] shabbily found {oldMaterial}");
			}
			if (newMaterial != null) {
				Debug.Log ($"[Shabby] shabbily found {newMaterial}");
			}

			if (oldMaterial != null && newMaterial != null
				&& Detourer.TryDetourFromTo (oldMaterial, newMaterial)) {
				Debug.Log ("[Shabby] shabbily activated");
			}

			MethodInfo rmt = pr.GetMethod("ReadMaterialTexture", bindFlags);
			ReadMaterialTexture = (RMTdelegate) Delegate.CreateDelegate (typeof(RMTdelegate), rmt);
			if (ReadMaterialTexture != null) {
				Debug.Log ($"[Shabby] shabbily made {ReadMaterialTexture}");
			}
		}

		delegate void RMTdelegate (BinaryReader br, Material mat, string name);
		static RMTdelegate ReadMaterialTexture;

		static Color ReadColor (BinaryReader br)
		{
			float r = br.ReadSingle ();
			float g = br.ReadSingle ();
			float b = br.ReadSingle ();
			float a = br.ReadSingle ();
			return new Color (r, g, b, a);
		}

		static Vector4 ReadVector (BinaryReader br)
		{
			float x = br.ReadSingle ();
			float y = br.ReadSingle ();
			float z = br.ReadSingle ();
			float w = br.ReadSingle ();
			return new Vector4 (x, y, z, w);
		}

		static Material ReadMaterial4(BinaryReader br)
		{
			string materialName = br.ReadString ();
			string shaderName = br.ReadString ();
			int numProperties = br.ReadInt32 ();

			Shader shader = FindShader (shaderName);
			var material = new Material (shader);
			material.name = materialName;

			for (int i = 0; i < numProperties; i++) {
				string propertyName = br.ReadString ();
				int propertyType = br.ReadInt32 ();

				switch (propertyType) {
					case 0:	// Color
						material.SetColor (propertyName, ReadColor (br));
						break;
					case 1:	// Vector
						material.SetVector (propertyName, ReadVector (br));
						break;
					case 2:	// Float
					case 3:	// Range
						material.SetFloat (propertyName, br.ReadSingle ());
						break;
					case 4:	// TexEnv
						ReadMaterialTexture (br, material, propertyName);
						break;
				}
			}
			return material;
		}

		static Dictionary<string, Shader> loadedShaders;

		public static void AddShader (Shader shader)
		{
			if (loadedShaders == null) {
				loadedShaders = new Dictionary<string, Shader> ();
			}
			loadedShaders[shader.name] = shader;
		}

		static Shader FindShader (string shaderName)
		{
			var shader = Shader.Find(shaderName);
			if (shader != null) {
				Debug.Log ($"[Shabby] found (stock):{shader.name}");
				return shader;
			}
			if (loadedShaders.TryGetValue (shaderName, out shader)) {
				Debug.Log ($"[Shabby] found (mod):{shader.name}");
				return shader;
			}
			Debug.Log ($"[Shabby] shader not found:{shaderName}");
			return null;
		}
	}
}
