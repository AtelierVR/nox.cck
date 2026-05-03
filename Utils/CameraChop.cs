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
		[Tooltip("The camera that will NOT see the chop bones."), NonSerialized]
		public static Camera[] WatchedCamera = Array.Empty<Camera>();

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

		[Tooltip("Bones whose localScale is set to zero before rendering the watched camera.")]
		[SerializeField] private Transform[] _bones = Array.Empty<Transform>();

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
			Bones = Bones; // Cache original scales
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
			RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
		}

		protected virtual void OnDisable()
		{
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
