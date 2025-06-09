using System.Collections.Generic;
using KSPBuildTools;
using UnityEngine;

namespace Shabby.DynamicProperties;

[KSPAddon(KSPAddon.Startup.EveryScene, false)]
public sealed class MaterialPropertyManager : MonoBehaviour
{
	#region Fields

	public static MaterialPropertyManager Instance { get; private set; }

	private readonly Dictionary<Renderer, CompiledProps> compiledProperties = [];

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

	private void LateUpdate() => Refresh();

	private void OnDestroy()
	{
		if (Instance != this) return;

		Instance = null;
		CompiledProps.ClearCache();
		this.LogDebug("destroyed");
	}

	#endregion

	public bool Set(Renderer renderer, Props props)
	{
		if (!compiledProperties.TryGetValue(renderer, out var compiledProps)) {
			compiledProperties[renderer] = compiledProps = new CompiledProps();
		}

		return compiledProps.Add(props);
	}

	public bool Remove(Renderer renderer, Props props)
	{
		if (!compiledProperties.TryGetValue(renderer, out var compiledProps)) return false;
		return compiledProps.Remove(props);
	}

	private static readonly List<Renderer> _deadRenderers = [];

	private void Refresh()
	{
		CompiledProps.RefreshChangedProps();

		foreach (var (renderer, compiledProps) in compiledProperties) {
			if (renderer == null) {
				this.LogDebug($"dead renderer {renderer.GetHashCode()}");
				_deadRenderers.Add(renderer);
				continue;
			}

			if (!renderer.gameObject.activeInHierarchy) continue;

			if (compiledProps.GetIfChanged(out var mpb)) {
				this.LogDebug($"set mpb on renderer {renderer.name} {renderer.GetHashCode()}\n");
				renderer.SetPropertyBlock(mpb);
			}
		}

		foreach (var dead in _deadRenderers) compiledProperties.Remove(dead);
		_deadRenderers.Clear();
	}
}
