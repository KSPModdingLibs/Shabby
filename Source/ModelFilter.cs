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
using UnityEngine;

namespace Shabby
{
public class ModelFilter
{
	public HashSet<string> targetMaterials;
	public HashSet<string> targetTransforms;
	public bool blanketApply;

	public HashSet<string> ignoredMeshes;

	public ModelFilter(ConfigNode node)
	{
		targetMaterials = node.GetValuesList("targetMaterial").ToHashSet();
		targetTransforms = node.GetValuesList("targetTransform").ToHashSet();

		if (targetMaterials.Count > 0 && targetTransforms.Count > 0) {
			Debug.LogError("[Shabby] model filter may not specify both materials and transforms");
			targetTransforms.Clear();
		}

		blanketApply = targetMaterials.Count == 0 && targetTransforms.Count == 0;

		ignoredMeshes = node.GetValuesList("ignoreMesh").ToHashSet();
	}

	public bool MatchMaterial(Renderer renderer) => targetMaterials.Contains(renderer.sharedMaterial.name);
	public bool MatchTransform(Transform transform) => targetTransforms.Contains(transform.name);

	public bool MatchIgnored(Renderer renderer) => ignoredMeshes.Contains(renderer.transform.name);
}

}
