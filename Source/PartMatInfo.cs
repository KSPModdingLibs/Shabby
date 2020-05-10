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
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using Harmony;

namespace Shabby {

	[HarmonyPatch(typeof(PartMaterialInfo))]
	[HarmonyPatch("Load")]
	class PartMaterialInfo_Load
	{
		static bool Prefix (ref ConfigNode node, ref Shader ___shader)
		{
			var orig = node;
			node = orig.CreateCopy ();
			node.RemoveValues ("shader");
			var shaders = orig.GetValues("shader");
			if (shaders.Length > 0) {
				___shader = Shabby.FindShader (shaders[shaders.Length - 1]);
			}
			return true;
		}
	}
}
