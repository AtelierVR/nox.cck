using UnityEngine;

namespace Nox.CCK.Theme {
	/// <summary>
	/// Look of a themed background, as data.
	/// <para>
	/// A theme only names a background; what it draws and what it animates lives in the type: one extension per look
	/// (<see cref="PerlinBackground"/>) and one per effect that wraps another one (<see cref="ParallaxBackground"/>).
	/// A mod ships a new look by deriving from this class, so nothing here knows the UI, the shader or the theme.
	/// </para>
	/// <para>
	/// An extension writes shader properties on the material instance of the panel, never on the asset the panels
	/// share (see <c>ThemedBackground</c>).
	/// </para>
	/// </summary>
	public abstract class Background : ScriptableObject {
		/// <summary>Id of the background, used by the built-in ones and by the log.</summary>
		public string id = "background";

		[Tooltip("Matériau du fond ; vide = garder celui du prefab.")]
		public Material material;

		/// <summary>
		/// How much of the accent hue the look takes when a theme adapts it, from 0 (the authored greys) to 1 (the full
		/// accent). Kept low on purpose: a background reads as a grey that belongs to the theme, not as the theme colour.
		/// </summary>
		[Range(0f, 1f)]
		[Tooltip("Part de la teinte du thème reprise par le fond (0 = gris d'origine).")]
		public float tint = 0.1f;

		/// <summary>
		/// Material the panel must draw with, resolved through the extensions: an effect that wraps another
		/// background takes the one of the look it decorates.
		/// </summary>
		public virtual Material Source
			=> material;

		/// <summary>Writes the look on the material the panel draws with.</summary>
		/// <param name="target">Instance the panel draws with, or <c>null</c>.</param>
		public virtual void Apply(Material target) { }

		/// <summary>
		/// Same look, brought towards the colours of a theme: what a themed panel draws while the theme is active.
		/// <para>
		/// The default returns the look itself, the right answer for a background that carries no colour (a material
		/// with its own texture, for instance). A look with parameters copies itself and tints the ones it owns, which
		/// keeps the asset a mod shipped untouched: the copy lives as long as the theme.
		/// </para>
		/// </summary>
		/// <param name="accent">Accent of the theme adapting the look.</param>
		public virtual Background Adapt(Color accent)
			=> this;

		/// <summary>
		/// A grey seen through the theme: the hue of the accent at the brightness of the grey, brought in by
		/// <see cref="tint"/> only. The luminance is kept, so the background keeps its place in the contrast of the panel.
		/// </summary>
		/// <param name="grey">Authored grey of the look.</param>
		/// <param name="accent">Accent of the theme.</param>
		protected Color Tinted(Color grey, Color accent)
			=> Color.Lerp(grey, ThemeAsset.Tint(grey, accent), tint);

		/// <summary>
		/// Backgrounds every install has, built once so that the built-in themes share the same instances. A look
		/// can be edited on its own (an asset created from the menu) and given to any theme.
		/// </summary>
		public static Background[] Builtin()
			=> new Background[] {
				Perlin("perlin", 0.149f, 0.2f),
				Perlin("dark", 0.035f, 0.055f),
				Perlin("light", 0.9f, 0.97f),
			};

		/// <summary>
		/// Noise of the shipped material, in the greys given: a monochrome theme draws a background of its own
		/// brightness, instead of a mid grey glowing around a black UI or staining a white one.
		/// </summary>
		private static Background Perlin(string id, float color1, float color2) {
			var perlin = CreateInstance<PerlinBackground>();
			perlin.id = id;
			perlin.color1 = new Color(color1, color1, color1, 1f);
			perlin.color2 = new Color(color2, color2, color2, 1f);
			return Create(perlin);
		}

		/// <summary>
		/// Names a background created in code and keeps it out of the assets: a <see cref="ScriptableObject"/> is never
		/// built with <c>new</c>, Unity creates it through <c>CreateInstance</c>.
		/// </summary>
		private static T Create<T>(T background) where T : Background {
			background.name = $"[Background_{background.id}]";
			background.hideFlags = HideFlags.DontSave;
			return background;
		}
	}
}
