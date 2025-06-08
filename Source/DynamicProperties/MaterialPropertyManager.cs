using System.Collections.Generic;
using UnityEngine;

namespace Shabby.DynamicProperties;

[KSPScenario(
	createOptions: ScenarioCreationOptions.AddToAllGames,
	tgtScenes: [GameScenes.LOADING, GameScenes.EDITOR, GameScenes.FLIGHT])]
public sealed class MaterialPropertyManager : ScenarioModule
{
	#region Fields

	public static MaterialPropertyManager Instance { get; private set; }

	private readonly Dictionary<Renderer, CompiledProps> compiledProperties = [];

	#endregion

	#region Lifecycle

	public override void OnAwake()
	{
		name = nameof(MaterialPropertyManager);
		Instance = this;
	}

	private void LateUpdate() => Refresh();

	public void OnDestroy()
	{
		Instance = null;
		CompiledProps.Clear();
	}

	#endregion

	public void Set(Renderer renderer, Props props)
	{
		if (!compiledProperties.TryGetValue(renderer, out var compiledProps)) {
			compiledProperties[renderer] = compiledProps = new CompiledProps();
		}

		compiledProps.Add(props);
	}

	private static readonly List<Renderer> _deadRenderers = [];

	private void Refresh()
	{
		CompiledProps.RefreshChangedProps();

		foreach (var (renderer, compiledProps) in compiledProperties) {
			if (renderer == null) {
				_deadRenderers.Add(renderer);
				continue;
			}

			if (compiledProps.GetIfChanged(out var mpb)) {
				renderer.SetPropertyBlock(mpb);
			}
		}

		foreach (var dead in _deadRenderers) {
			compiledProperties.Remove(dead);
		}
	}
}
