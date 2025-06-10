using System.Collections.Generic;
using KSPBuildTools;
using UnityEngine;

namespace Shabby.DynamicProperties;

[KSPAddon(KSPAddon.Startup.EveryScene, false)]
public sealed class MaterialPropertyManager : MonoBehaviour
{
	#region Fields

	public static MaterialPropertyManager Instance { get; private set; }

	private readonly Dictionary<Renderer, PropsCascade> rendererCascades = [];

	#endregion

	#region Lifecycle

	private MaterialPropertyManager()
	{
	}

	private void Awake()
	{
		if (Instance != null) {
			DestroyImmediate(this);
			return;
		}

		name = nameof(MaterialPropertyManager);
		Instance = this;
	}

	private void OnDestroy()
	{
		if (Instance != this) return;

		Instance = null;
		PropsCascade.ClearCache();

		// Poor man's GC :'(
		MaterialColorUpdaterPatch.temperatureColorProps.Clear();
		ModuleColorChangerPatch.mccProps.Clear();

		this.LogDebug("destroyed");
	}

	#endregion

	public bool Set(Renderer renderer, Props props)
	{
		if (renderer == null) {
			Log.LogError(this, $"cannot set property on null renderer {renderer.GetHashCode()}");
			return false;
		}

		if (!rendererCascades.TryGetValue(renderer, out var cascade)) {
			rendererCascades[renderer] = cascade = new PropsCascade(renderer);
		}

		return cascade.Add(props);
	}

	public bool Remove(Renderer renderer, Props props)
	{
		if (!rendererCascades.TryGetValue(renderer, out var cascade)) return false;
		return cascade.Remove(props);
	}

	public bool Remove(Renderer renderer)
	{
		return rendererCascades.Remove(renderer);
	}

	public bool Remove(Props props)
	{
		var removed = false;

		foreach (var cascade in rendererCascades.Values) removed |= cascade.Remove(props);

		PropsCascade.RemoveCacheEntriesWith(props);

		return removed;
	}
}
