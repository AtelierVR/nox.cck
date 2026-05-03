using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nox.CCK.Utils
{
	/// <summary>
	/// Hides a set of bones from a specific camera during rendering by scaling them to
	/// <see cref="Vector3.zero"/> before each render and restoring their original scale
	/// after. Because it operates on bone transforms rather than renderers, it works
	/// correctly for single-mesh skinned avatars where toggling <c>Renderer.enabled</c>
	/// would hide the entire body.
	/// Requires <see cref="SkinnedMeshRenderer.forceMatrixRecalculationPerRender"/> to be
	/// enabled on the affected renderers so bone-matrix changes are picked up every frame.
	/// </summary>
	[DisallowMultipleComponent]
	public class CameraChop : MonoBehaviour
	{
		[Tooltip("Camera(s) to hide the bones from. Can be registered at runtime using the static API."), NonSerialized]
		public static Camera[] WatchedCamera = Array.Empty<Camera>();

		[Tooltip("All active CameraChop instances."), NonSerialized]
		public static CameraChop[] Instances = Array.Empty<CameraChop>();

		public static void RegisterCamera(Camera cam)
		{
			if (cam == null || Array.IndexOf(WatchedCamera, cam) >= 0) return;
			Array.Resize(ref WatchedCamera, WatchedCamera.Length + 1);
			WatchedCamera[^1] = cam;
			Logger.LogDebug($"Camera {cam.name} registered", tag: nameof(CameraChop));
		}

		public static void UnregisterCamera(Camera cam)
		{
			if (cam == null) return;
			int index = Array.IndexOf(WatchedCamera, cam);
			if (index < 0) return;
			for (int i = index; i < WatchedCamera.Length - 1; i++)
				WatchedCamera[i] = WatchedCamera[i + 1];
			Array.Resize(ref WatchedCamera, WatchedCamera.Length - 1);
			Logger.LogDebug($"Camera {cam.name} unregistered", tag: nameof(CameraChop));
		}

		public static bool IsCameraWatched(Camera cam)
			=> cam && Array.IndexOf(WatchedCamera, cam) >= 0;

		// ── Instance registry ───────────────────────────────────────────────────

		private static void AddInstance(CameraChop instance)
		{
			if (instance == null || Array.IndexOf(Instances, instance) >= 0) return;
			Array.Resize(ref Instances, Instances.Length + 1);
			Instances[^1] = instance;
			Logger.LogDebug($"CameraChop instance added (total: {Instances.Length})", tag: nameof(CameraChop));
		}

		private static void RemoveInstance(CameraChop instance)
		{
			if (instance == null) return;
			int index = Array.IndexOf(Instances, instance);
			if (index < 0) return;
			for (int i = index; i < Instances.Length - 1; i++)
				Instances[i] = Instances[i + 1];
			Array.Resize(ref Instances, Instances.Length - 1);
			Logger.LogDebug($"CameraChop instance removed (total: {Instances.Length})", tag: nameof(CameraChop));
		}

		/// <summary>
		/// Restores bone scales on every active <see cref="CameraChop"/> instance.
		/// Call this before rendering a secondary camera (e.g. a mirror) so it sees
		/// the full avatar, then call <see cref="HideAllForCamera"/> afterwards to
		/// re-apply hiding for the primary first-person camera.
		/// </summary>
		public static void RestoreAllScales()
		{
			foreach (var instance in Instances)
				instance.RestoreScales();
		}

		/// <summary>
		/// Re-hides the chop bones on every active instance for the given watched camera.
		/// No-op if <paramref name="cam"/> is not a watched camera.
		/// </summary>
		public static void HideAllForCamera(Camera cam)
		{
			if (!IsCameraWatched(cam)) return;
			foreach (var instance in Instances)
				for (int i = 0; i < instance._bones.Length; i++)
					if (instance._bones[i]) instance._bones[i].localScale = Vector3.zero;
		}

		[Tooltip("Bones whose localScale is set to zero before rendering the watched camera."), SerializeField]
		private Transform[] _bones = Array.Empty<Transform>();

		[NonSerialized]
		private Vector3[] _originalScales = Array.Empty<Vector3>();

		// ── Public API ──────────────────────────────────────────────────────────

		public Transform[] Bones
		{
			get => _bones;
			set
			{
				_bones = value ?? Array.Empty<Transform>();
				CacheBoneScales();
			}
		}

		// ── Internal ────────────────────────────────────────────────────────────

		private void CacheBoneScales()
		{
			_originalScales = new Vector3[_bones.Length];
			for (int i = 0; i < _bones.Length; i++)
				_originalScales[i] = _bones[i] ? _bones[i].localScale : Vector3.one;
		}

		private void RestoreScales()
		{
			for (int i = 0; i < _bones.Length; i++)
				if (_bones[i] && i < _originalScales.Length)
					_bones[i].localScale = _originalScales[i];
		}

		// ── Lifecycle ───────────────────────────────────────────────────────────

		protected virtual void OnEnable()
		{
			AddInstance(this);
			Bones = Bones; // Cache original scales
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
			RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
		}

		protected virtual void OnDisable()
		{
			RemoveInstance(this);
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
			RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
			RestoreScales();
		}

		// ── Camera callbacks ────────────────────────────────────────────────────

		private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
		{
			if (!IsCameraWatched(cam)) return;
			for (int i = 0; i < _bones.Length; i++)
				if (_bones[i]) _bones[i].localScale = Vector3.zero;
		}

		private void OnEndCameraRendering(ScriptableRenderContext ctx, Camera cam)
		{
			if (!IsCameraWatched(cam)) return;
			RestoreScales();
		}
	}
}
