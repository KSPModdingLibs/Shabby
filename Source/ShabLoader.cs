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
using KSPBuildTools;
using UnityEngine;

namespace Shabby
{
	[DatabaseLoaderAttrib(new string[] { "shab" })]
	public class DatabaseLoaderTexture_SHAB : DatabaseLoader<GameDatabase.TextureInfo>
	{
		public override IEnumerator Load(UrlDir.UrlFile urlFile, FileInfo file)
		{
			Log.Message($"loading '{urlFile.fullPath}'");
			var bundle = AssetBundle.LoadFromFile(urlFile.fullPath);
			if (!bundle) {
				Log.Warning($"could not load {urlFile.fullPath}");
			} else {
				Shader[] shaders = bundle.LoadAllAssets<Shader>();
				foreach (Shader shader in shaders) {
					Log.Debug($"adding {shader.name}");
					Shabby.AddShader(shader);
				}
			}

			yield break;
		}
	}
}