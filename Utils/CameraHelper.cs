using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nox.CCK.Utils {
	/// <summary>How a billboard derives its facing direction from a camera.</summary>
	public enum CameraFacingMode {
		/// <summary>Follow the camera forward axis: every billboard stays parallel to the view.</summary>
		Forward,

		/// <summary>Look at the camera position: every billboard turns toward the viewer.</summary>
		Position,
	}

	/// <summary>
	/// Shared camera helpers: billboarding (nameplates, portal cards), camera resolution and mirror
	/// clip planes. Used by nox.mirror, nox.nameplate and nox.portals.
	/// </summary>
	public static class CameraHelper {
		public static Vector4 SpacePlane(Matrix4x4 matrix, Vector3 pos, Vector3 normal, float offset) {
			var offsetPos = pos + (normal * offset);
			var cPos      = matrix.MultiplyPoint(offsetPos);
			var cNormal   = matrix.MultiplyVector(normal).normalized;
			return new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPos, cNormal));
		}

		/// <summary>
		/// Camera-space plane of a mirror clip plane, with an optional side sign. Same convention as
		/// the planar mirror: <paramref name="normal"/> points toward the viewer.
		/// </summary>
		public static Vector4 CameraSpacePlane(
			Camera camera,
			Vector3 pos,
			Vector3 normal,
			float clipPlaneOffset,
			float sideSign = 1f
		) {
			if (!camera)
				return Vector4.zero;

			var offsetPos  = pos + (normal * clipPlaneOffset);
			var worldToCam = camera.worldToCameraMatrix;
			var cPos       = worldToCam.MultiplyPoint(offsetPos);
			var cNormal    = worldToCam.MultiplyVector(normal).normalized * sideSign;
			return new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPos, cNormal));
		}

		/// <summary>
		/// Camera to use: the preferred one, else <see cref="Camera.main"/>, else the first enabled
		/// camera (sample scenes often have an untagged camera).
		/// </summary>
		public static Camera Resolve(Camera preferred = null) {
			if (preferred)
				return preferred;

			if (Camera.main)
				return Camera.main;

			var cameras = Camera.allCameras;
			return cameras.Length > 0 ? cameras[0] : null;
		}

		/// <summary>
		/// Rotation that turns <paramref name="position"/> toward <paramref name="camera"/>.
		/// <paramref name="faceNegativeZ"/> flips the result for visuals whose readable side is -Z
		/// (portal cards); disable it for +Z-facing visuals.
		/// </summary>
		public static bool TryGetFacing(
			Vector3 position,
			Camera camera,
			out Quaternion rotation,
			CameraFacingMode mode = CameraFacingMode.Position,
			bool yawOnly = true,
			bool faceNegativeZ = false,
			Vector3 eulerOffset = default
		) {
			rotation = Quaternion.identity;

			if (!camera)
				return false;

			var direction = mode == CameraFacingMode.Forward
				? camera.transform.forward
				: camera.transform.position - position;

			if (yawOnly)
				direction.y = 0f;

			if (direction.sqrMagnitude < 0.0001f)
				return false;

			direction.Normalize();

			if (faceNegativeZ)
				direction = -direction;

			rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(eulerOffset);
			return true;
		}

		/// <summary>
		/// Same as <see cref="TryGetFacing"/>, returning <see cref="Quaternion.identity"/> when no
		/// facing could be computed.
		/// </summary>
		public static Quaternion Facing(
			Vector3 position,
			Camera camera,
			CameraFacingMode mode = CameraFacingMode.Position,
			bool yawOnly = true,
			bool faceNegativeZ = false,
			Vector3 eulerOffset = default
		)
			=> TryGetFacing(position, camera, out var rotation, mode, yawOnly, faceNegativeZ, eulerOffset)
				? rotation
				: Quaternion.identity;

		/// <summary>Frame-rate independent rotation smoothing. A non-positive speed snaps.</summary>
		public static Quaternion Smooth(Quaternion current, Quaternion target, float speed, float deltaTime)
			=> speed <= 0f
				? target
				: Quaternion.Slerp(current, target, 1f - Mathf.Exp(-speed * Mathf.Max(0f, deltaTime)));

		/// <summary>
		/// Turns <paramref name="target"/> toward the camera. Returns <c>false</c> (and leaves the
		/// transform untouched) when no camera or no usable direction is available.
		/// </summary>
		public static bool FaceCamera(
			Transform target,
			Camera camera = null,
			CameraFacingMode mode = CameraFacingMode.Position,
			bool yawOnly = true,
			bool faceNegativeZ = false,
			float rotationSpeed = 0f,
			Vector3 eulerOffset = default
		) {
			if (!target)
				return false;

			camera = Resolve(camera);

			if (!camera)
				return false;

			if (!TryGetFacing(target.position, camera, out var rotation, mode, yawOnly, faceNegativeZ, eulerOffset))
				return false;

			target.rotation = Smooth(target.rotation, rotation, rotationSpeed, Time.deltaTime);
			return true;
		}

		// ── Camera getters ────────────────────────────────────────────────────────

		/// <summary>
		/// The main camera (<c>Camera.main</c>, i.e. the enabled camera tagged <c>MainCamera</c>),
		/// or <c>null</c> when none is tagged. Assigning makes the camera main (see <see cref="SetMain"/>).
		/// </summary>
		public static Camera Main {
			get => Camera.main;
			set => SetMain(value);
		}

		/// <summary>
		/// The camera used by the last active Scene view (editor), or <c>null</c> at runtime.
		/// Also the camera of a prefab stage, which reuses the Scene view.
		/// </summary>
		public static Camera SceneView {
			get {
				#if UNITY_EDITOR
				var view = UnityEditor.SceneView.lastActiveSceneView;
				return view ? view.camera : null;
				#else
				return null;
				#endif
			}
		}

		/// <summary>
		/// The camera currently rendering (<see cref="Camera.current"/>). Only meaningful inside a
		/// rendering callback (e.g. <c>beginCameraRendering</c>); <c>null</c> elsewhere.
		/// </summary>
		public static Camera Current
			=> Camera.current;

		/// <summary>Every enabled camera, in rendering order.</summary>
		public static Camera[] All
			=> Camera.allCameras;

		/// <summary>
		/// Makes <paramref name="camera"/> the main camera: it becomes <see cref="Camera.current"/> /
		/// <c>Camera.main</c> and the other cameras lose the <c>MainCamera</c> tag (controller switch).
		/// Returns <c>false</c> when no camera was given.
		/// </summary>
		public static bool SetMain(Camera camera, bool untagOthers = true) {
			if (!camera)
				return false;

			Camera.SetupCurrent(camera);
			camera.tag = "MainCamera";

			if (untagOthers)
				foreach (var other in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
					if (other != camera && other.CompareTag("MainCamera"))
						other.tag = "Untagged";

			return true;
		}

		/// <summary>
		/// Camera driving pointer events for a canvas: its <c>worldCamera</c>, or the resolved camera
		/// for world/VR canvases. <c>null</c> for a screen-space overlay canvas (events use the screen).
		/// </summary>
		public static Camera EventCamera(Canvas canvas) {
			if (!canvas)
				return null;

			if (canvas.worldCamera)
				return canvas.worldCamera;

			return canvas.renderMode == RenderMode.ScreenSpaceOverlay
				? null
				: Resolve();
		}

		/// <summary>
		/// Ray through a screen position, from <paramref name="camera"/> (resolved when omitted).
		/// Returns <c>false</c> when no camera is available.
		/// </summary>
		public static bool TryScreenRay(Vector2 screenPosition, out Ray ray, Camera camera = null) {
			ray = default;

			camera = Resolve(camera);

			if (!camera)
				return false;

			ray = camera.ScreenPointToRay(screenPosition);
			return true;
		}
	}
}