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

	class MaterialPatch
	{
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

		static bool ReadMaterial4(BinaryReader br, ref Material __result)
		{
			string materialName = br.ReadString ();
			string shaderName = br.ReadString ();
			int numProperties = br.ReadInt32 ();

			Shader shader = Shabby.FindShader (shaderName);
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
						Shabby.ReadMaterialTexture (br, material, propertyName);
						break;
				}
			}
			__result = material;
			return false;
		}
	}
}
