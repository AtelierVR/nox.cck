using UnityEngine;
using UnityEngine.UI;

namespace Nox.CCK.Theme {
	/// <summary>
	/// Keeps the authored colours of a UI element and repaints them with the theme in use.
	/// <para>
	/// It is added by <see cref="ThemeManager"/> on the elements it repaints, and it remembers the colours the
	/// prefab gives, so a theme change replaces them instead of stacking on top of the previous theme.
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/Theme/Themed Element")]
	[DisallowMultipleComponent]
	public class ThemedElement : MonoBehaviour {
		/// <summary>
		/// Largest side of an icon, in UI pixels: a simple image smaller than this is a glyph drawn on a surface, a
		/// bigger one (or a sliced one) is the surface itself. Both are authored in the same white, so the size is
		/// what tells an icon from the panel it is drawn on.
		/// </summary>
		private const float GLYPH_SIZE = 64f;

		private Graphic _graphic;
		private Selectable _selectable;
		private Color _color;
		private ColorBlock _colors;
		private bool _captured;
		private bool _hasTint;
		private Color _tint;

		/// <summary>Whether the graphic is a glyph (a text or an icon) rather than a surface.</summary>
		private bool _glyph;

		/// <summary>
		/// Whether the graphic draws a <see cref="ThemedBackground"/>: its colour is the neutral multiplier of the
		/// material the background paints, so a theme must leave it alone (the look owns the colours).
		/// </summary>
		private bool _background;

		/// <summary>Repaints the element with a theme.</summary>
		/// <param name="theme">Theme to apply.</param>
		public void Apply(ThemeAsset theme) {
			Capture();

			if (_hasTint) {
				ApplyTint();
				return;
			}

			if (theme == null)
				return;

			if (_graphic && !_background)
				_graphic.color = theme.Map(_color, _glyph);

			if (!_selectable)
				return;

			var colors = _colors;
			colors.normalColor = theme.Map(colors.normalColor);
			colors.highlightedColor = theme.Map(colors.highlightedColor);
			colors.pressedColor = theme.Map(colors.pressedColor);
			colors.selectedColor = theme.Map(colors.selectedColor);
			colors.disabledColor = theme.Map(colors.disabledColor);
			_selectable.colors = colors;
		}

		/// <summary>Replaces the themed colour of the element with a fixed one.</summary>
		public void SetTint(Color tint) {
			_tint    = tint;
			_hasTint = true;
		}

		/// <summary>Forgets the fixed colour and lets the theme drive the element again.</summary>
		public void ClearTint()
			=> _hasTint = false;

		/// <summary>Paints the authored surfaces with the fixed colour, keeping the strength of the shape.</summary>
		private void ApplyTint() {
			if (_graphic && !_background)
				_graphic.color = new Color(_tint.r, _tint.g, _tint.b, _color.a);

			if (!_selectable)
				return;

			var colors = _colors;
			colors.normalColor      = _tint;
			colors.highlightedColor = Color.Lerp(_tint, Color.black, 0.2f);
			colors.pressedColor     = Color.Lerp(_tint, Color.white, 0.25f);
			colors.selectedColor    = _tint;
			colors.disabledColor    = new Color(_tint.r, _tint.g, _tint.b, _colors.disabledColor.a);
			_selectable.colors = colors;
		}

		/// <summary>Puts the colours of the prefab back.</summary>
		public void Restore() {
			if (!_captured)
				return;

			if (_graphic)
				_graphic.color = _color;

			if (_selectable)
				_selectable.colors = _colors;
		}

		/// <summary>Remembers the authored colours once: they are the ones every theme starts from.</summary>
		private void Capture() {
			if (_captured)
				return;

			_graphic = GetComponent<Graphic>();
			_selectable = GetComponent<Selectable>();
			_glyph = IsGlyph(_graphic);
			_background = GetComponent<ThemedBackground>() != null;

			if (_graphic)
				_color = _graphic.color;

			if (_selectable)
				_colors = _selectable.colors;

			_captured = true;
		}

		/// <summary>A text, or an icon small enough to be drawn on a surface instead of being one.</summary>
		private static bool IsGlyph(Graphic graphic) {
			if (!graphic)
				return false;

			if (graphic is Text || graphic is TMPro.TMP_Text)
				return true;

			var image = graphic as Image;

			if (!image || image.type != Image.Type.Simple)
				return false;

			var rect = image.rectTransform.rect;

			return rect.width <= GLYPH_SIZE && rect.height <= GLYPH_SIZE;
		}
	}
}
