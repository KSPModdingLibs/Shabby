using System.Collections.Generic;
using KSPBuildTools;

namespace Shabby.DynamicProperties;

internal abstract class StockPatchBase<T>
{
	internal static readonly Dictionary<T, Props> Props = [];

	internal static void CheckCleared()
	{
		if (Props.Count == 0) return;

		Log.Message($"cleared {Props.Count} Props instances", $"[{typeof(T).Name} MPM Patch]");
		Props.Clear();
	}
}
