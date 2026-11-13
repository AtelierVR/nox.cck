using UnityEngine;
using UnityEngine.UI;

namespace Nox.CCK.Theme {
	/// <summary>
	/// Slides a background with the camera: the offset property follows how far the camera moved since the effect
	/// became active, scaled by a strength the theme writes through a <see cref="BackgroundTemplate"/>.
	/// <para>
	/// It knows nothing about the look: only two shader properties, both configurable, so it works with any
	/// background shader exposing them. A material that carries no strength (or a zero one) leaves it idle, which is
	/// why the component can sit on every background: the theme decides whether the effect runs.
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/Theme/Background Parallax")]
	[DisallowMultipleComponent]
	public class BackgroundParallax : MonoBehaviour {
		[Tooltip("Graphic qui dessine le fond. Vide = le Graphic de cet objet.")]
		public Graphic graphic;

		[Tooltip("Propriété (Vector) du décalage d'UV écrit à chaque frame.")]
		public string offsetProperty = "_Parallax";

		[Tooltip("Propriété (Vector) de la force du parallaxe, écrite par le thème. Nulle = pas d'effet.")]
		public string strengthProperty = "_ParallaxStrength";

		[Tooltip("Vitesse de rattrapage (1/s). 0 = instantané.")]
		public float smoothing = 6f;

		private Vector2 _offset;
		private Vector3 _origin;
		private bool _started;

		private void LateUpdate() {
			var material = graphic ? graphic.material : null;
			if (!material || !material.HasProperty(offsetProperty))
				return;

			if (!TryGetStrength(material, out var strength) || strength == Vector2.zero) {
				_started = false;
				return;
			}

			var position = CameraPosition();

			if (!_started) {
				_origin = position;
				_started = true;
			}

			var delta = position - _origin;
			var target = new Vector2(delta.x * strength.x, delta.y * strength.y);
			var factor = smoothing > 0f
				? 1f - Mathf.Exp(-smoothing * Time.deltaTime)
				: 1f;

			_offset = Vector2.Lerp(_offset, target, factor);

			material.SetVector(offsetProperty, new Vector4(_offset.x, _offset.y, 0f, 0f));
		}

		/// <summary>Strength written by the theme, or <c>false</c> when the material exposes none.</summary>
		private bool TryGetStrength(Material material, out Vector2 strength) {
			strength = Vector2.zero;

			if (!material.HasProperty(strengthProperty))
				return false;

			var value = material.GetVector(strengthProperty);
			strength = new Vector2(value.x, value.y);
			return true;
		}

		/// <summary>Position of the camera the panel is looked through, or the origin while there is none.</summary>
		private static Vector3 CameraPosition() {
			var camera = Camera.main;
			if (!camera) {
				var cameras = Camera.allCameras;
				if (cameras.Length == 0)
					return Vector3.zero;

				camera = cameras[0];
			}

			return camera.transform.position;
		}
	}
}
