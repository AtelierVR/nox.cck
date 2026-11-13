using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nox.CCK.Utils {
	public enum RenderPhase {
		Input = -100,
		Default = 0,
		Capture = 100
	}

	public static class RenderOrder {
		public delegate void CameraCallback(Camera camera);

		private readonly struct Entry {
			public readonly CameraCallback Callback;
			public readonly RenderPhase Phase;

			public Entry(CameraCallback callback, RenderPhase phase) {
				Callback = callback;
				Phase = phase;
			}
		}

		private static Entry[] _entries = Array.Empty<Entry>();
		private static bool _installed;

		private static Camera _camera;
		private static Matrix4x4 _view;

		/// <summary>
		/// World to camera matrix of <paramref name="camera"/> as it was before the handlers of this frame
		/// ran, i.e. the pose the render pipeline latched its per eye matrices with.
		/// </summary>
		public static bool TryGetFrameViewMatrix(Camera camera, out Matrix4x4 worldToCamera) {
			if (!camera || camera != _camera) {
				worldToCamera = default;
				return false;
			}

			worldToCamera = _view;
			return true;
		}

		public static void Subscribe(CameraCallback callback, RenderPhase phase = RenderPhase.Default) {
			if (callback == null)
				return;

			Unsubscribe(callback);

			var index = _entries.Length;
			for (var i = 0; i < _entries.Length; i++)
				if (_entries[i].Phase > phase) {
					index = i;
					break;
				}

			var entries = new Entry[_entries.Length + 1];
			Array.Copy(_entries, 0, entries, 0, index);
			entries[index] = new Entry(callback, phase);
			Array.Copy(_entries, index, entries, index + 1, _entries.Length - index);

			_entries = entries;
			Install();
		}

		public static void Unsubscribe(CameraCallback callback) {
			if (callback == null)
				return;

			var index = -1;
			for (var i = 0; i < _entries.Length; i++)
				if (_entries[i].Callback == callback) {
					index = i;
					break;
				}

			if (index < 0)
				return;

			var entries = new Entry[_entries.Length - 1];
			Array.Copy(_entries, 0, entries, 0, index);
			Array.Copy(_entries, index + 1, entries, index, entries.Length - index);

			_entries = entries;

			if (entries.Length == 0)
				Uninstall();
		}

		private static void Install() {
			if (_installed)
				return;

			_installed = true;
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}

		private static void Uninstall() {
			if (!_installed)
				return;

			_installed = false;
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		}

		private static void OnBeginCameraRendering(ScriptableRenderContext _, Camera camera) {
			var pc = _camera;
			var pv = _view;

			_camera = camera;
			_view = camera
				? camera.worldToCameraMatrix
				: default;

			try {
				var entries = _entries;
				for (var i = 0; i < entries.Length; i++)
					entries[i].Callback?.Invoke(camera);
			} finally {
				_camera = pc;
				_view = pv;
			}
		}
	}
}
