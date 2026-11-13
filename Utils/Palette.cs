using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.CCK {
	/// <summary>
	/// Deterministic colours derived from a value: a parameter type, a category, an identifier... The same
	/// value always gives the same colour, so a badge keeps its colour between two sessions and two panels.
	/// <para>
	/// The colour is generated in HSV (fixed saturation and brightness) and is meant for a badge or a
	/// swatch: <see cref="TextColorFor"/> gives the text colour that stays readable on it.
	/// </para>
	/// </summary>
	public static class Palette {
		/// <summary>Saturation of the generated colours: muted enough to stay readable behind a text.</summary>
		private const float Saturation = 0.55f;

		/// <summary>Value (brightness) of the generated colours.</summary>
		private const float Brightness = 0.80f;

		/// <summary>Colour of a value that carries none (a null value).</summary>
		public static Color Neutral
			=> new(0.5f, 0.5f, 0.5f);

		/// <summary>
		/// Colour of any value: a <see cref="Color"/> or a <see cref="Color32"/> is used as-is, a null value
		/// gets the <see cref="Neutral"/> one, and everything else gets a colour derived from its text
		/// (stable across runs and platforms, see <see cref="Hash.CRC32(string)"/>).
		/// </summary>
		public static Color ColorOf(object value) {
			switch (value) {
				case null:
					return Neutral;
				case Color color:
					return color;
				case Color32 color32:
					return color32;
				default:
					var hash = Hash.CRC32(value.ToString() ?? string.Empty);
					var hue  = (hash & 0x7FFFFFFF) % 360 / 360f;
					return Color.HSVToRGB(hue, Saturation, Brightness);
			}
		}

		/// <summary>
		/// Text colour that stays readable on <paramref name="background"/>: the light or the dark text,
		/// whichever has the best contrast ratio against it (WCAG relative luminance).
		/// </summary>
		public static Color TextColorFor(Color background) {
			var luminance = Luminance(background);

			var contrastWithLight = 1.05f / (luminance + 0.05f);
			var contrastWithDark  = (luminance + 0.05f) / 0.05f;

			return contrastWithLight >= contrastWithDark
				? new Color(0.93f, 0.95f, 1.00f)
				: new Color(0.09f, 0.10f, 0.12f);
		}

		/// <summary>WCAG relative luminance of a colour: its sRGB channels linearized, then weighted.</summary>
		private static float Luminance(Color color)
			=> 0.2126f * Linear(color.r)
				+ 0.7152f * Linear(color.g)
				+ 0.0722f * Linear(color.b);

		/// <summary>Linearizes one sRGB channel (the transfer function of the sRGB standard).</summary>
		private static float Linear(float channel)
			=> channel <= 0.04045f
				? channel / 12.92f
				: Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
	}
}
