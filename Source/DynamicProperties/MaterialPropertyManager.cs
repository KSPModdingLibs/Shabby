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

	private readonly List<Props> propsLateUpdateQueue = [];

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
		foreach (var cascade in rendererCascades.Values) cascade.Dispose();
		MpbCompilerCache.CheckCleared();

		// Poor man's GC :'(
		PartPatch.ClearOnSceneSwitch();
		MaterialColorUpdaterPatch.ClearOnSceneSwitch();
		ModuleColorChangerPatch.ClearOnSceneSwitch();
		FairingPanelPatch.ClearOnSceneSwitch();

		this.LogMessage("destroyed");
	}

	#endregion

	#region Public API

	public bool Set(Renderer renderer, Props props)
	{
		if (!CheckRendererAlive(renderer)) return false;

		if (!rendererCascades.TryGetValue(renderer, out var cascade)) {
			rendererCascades[renderer] = cascade = new PropsCascade(renderer);
		}

		return cascade.Add(props);
	}

	public bool Unset(Renderer renderer, Props props)
	{
		if (!CheckRendererAlive(renderer)) return false;
		if (!rendererCascades.TryGetValue(renderer, out var cascade)) return false;
		return cascade.Remove(props);
	}

	public bool Unregister(Renderer renderer)
	{
		if ((object)renderer == null) return false;
		if (!rendererCascades.Remove(renderer, out var cascade)) return false;
		if (renderer == null) this.LogDebug($"dead renderer {renderer.GetHashCode()}");
		cascade.Dispose();
		return true;
	}

	public static void RegisterPropertyNamesForDebugLogging(params string[] properties)
	{
		foreach (var property in properties) PropIdToName.Register(property);
	}

	#endregion

	private bool CheckRendererAlive(Renderer renderer)
	{
		if (renderer != null) return true;
		this.LogWarning($"cannot modify null renderer {renderer?.GetHashCode()}");
		if ((object)renderer != null) Unregister(renderer);
		return false;
	}

	private readonly List<Renderer> _deadRenderers = [];

	internal void CheckRemoveDeadRenderers()
	{
		foreach (var renderer in rendererCascades.Keys) {
			if (renderer == null) _deadRenderers.Add(renderer);
		}

		foreach (var deadRenderer in _deadRenderers) Unregister(deadRenderer);
		_deadRenderers.Clear();
	}

	/// Public API equivalent is calling `Props.Dispose`.
	internal void Unregister(Props props)
	{
		foreach (var (renderer, cascade) in rendererCascades) {
			if (renderer != null) cascade.Remove(props);
		}

		CheckRemoveDeadRenderers();
	}

	private bool _propRefreshScheduled = false;
	private static readonly WaitForEndOfFrame WfEoF = new();

	private IEnumerator<YieldInstruction> Co_propsLateUpdate()
	{
		yield return WfEoF;

		foreach (var props in propsLateUpdateQueue) {
			if (props.NeedsEntriesUpdate) {
				props.OnEntriesChanged?.Invoke(props);
			} else if (props.NeedsValueUpdate) {
				props.OnValueChanged?.Invoke(props, null);
			}

			props.SuppressEagerUpdate =
				props.NeedsEntriesUpdate = props.NeedsValueUpdate = false;
		}

		propsLateUpdateQueue.Clear();
		_propRefreshScheduled = false;
	}

	internal void ScheduleLateUpdate(Props props)
	{
		propsLateUpdateQueue.Add(props);
		if (_propRefreshScheduled) return;
		StartCoroutine(Co_propsLateUpdate());
		_propRefreshScheduled = true;
	}
}
