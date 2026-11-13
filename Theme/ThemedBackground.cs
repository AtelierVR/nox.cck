using UnityEngine;
using UnityEngine.UI;

namespace Nox.CCK.Theme {
	/// <summary>
	/// Background of a themed panel: the graphic that draws it, whose material and shader properties follow the
	/// <see cref="Background"/> of the theme in use.
	/// <para>
	/// The authored material is remembered, so a theme that names no background leaves the panel as it is and a theme
	/// change replaces the previous look instead of stacking on top of it. The material is drawn through a fresh
	/// instance: a background writes its properties on it, and must never touch the asset every panel shares.
	/// </para>
	/// <para>
	/// Nothing here knows what the look is: an animated noise, a plain colour or anything else is a shader and a
	/// <see cref="Background"/> extension. An effect that reacts to the player (a parallax, for instance) is its own
	/// component next to this one, driven by the properties the background writes.
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/Theme/Themed Background")]
	[DisallowMultipleComponent]
	public class ThemedBackground : MonoBehaviour {
		[Tooltip("Graphic qui dessine le fond. Vide = le Graphic de cet objet.")]
		public Graphic graphic;

		/// <summary>Graphic the background is drawn by, resolved once from <see cref="graphic"/>.</summary>
		private Graphic _graphic;

		/// <summary>Material authored by the prefab: the one every theme starts from.</summary>
		private Material _authored;

		/// <summary>Instance the graphic draws with, or <see cref="_authored"/> when nothing is themed.</summary>
		private Material _material;

		private bool _captured;

		/// <summary>Paints the background with the background of a theme.</summary>
		/// <param name="theme">Theme to apply.</param>
		public void Apply(ThemeAsset theme) {
			Capture();

			if (!_graphic || !_authored)
				return;

			var background = theme?.background;

			// No background in the theme: the panel keeps the look the prefab authored.
			if (background == null) {
				Restore();
				return;
			}

			SetSource(background.Source ? background.Source : _authored);
			background.Apply(_material);
		}

		/// <summary>Puts the material of the prefab back.</summary>
		public void Restore() {
			if (!_captured)
				return;

			if (_graphic)
				_graphic.material = _authored;

			Release();

			_material = _authored;
		}

		private void OnDestroy()
			=> Release();

		/// <summary>Remembers the authored material once: it is the one every theme starts from.</summary>
		private void Capture() {
			if (_captured)
				return;

			if (!graphic)
				graphic = GetComponent<Graphic>();

			_graphic = graphic;

			if (_graphic) {
				_authored = _graphic.material;
				_material = _authored;
			}

			_captured = true;
		}

		/// <summary>
		/// Draws the graphic with a fresh instance of the given material. It is rebuilt on every apply, so a property
		/// written by the previous background (a parallax, an effect) cannot survive a theme change, and the shared
		/// asset keeps the look the prefab authored: the instance is the only place a background writes.
		/// </summary>
		private void SetSource(Material source) {
			if (!_graphic || !source)
				return;

			Release();

			_material = new Material(source) {
				name = $"{source.name} (themed)",
				hideFlags = HideFlags.DontSave,
			};

			_graphic.material = _material;
		}

		/// <summary>Destroys the instance, so a panel that changed theme many times keeps a single material.</summary>
		private void Release() {
			if (!_material || _material == _authored)
				return;

			Destroy(_material);
			_material = null;
		}
	}
}
