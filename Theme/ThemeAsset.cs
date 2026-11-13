using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nox.CCK.Theme {
	/// <summary>
	/// One theme: the colour swaps that tint the UI, as a single asset.
	/// <para>
	/// One asset per theme, so a theme can be added, edited or removed without touching the others: a mod
	/// ships its own assets, the built-in themes are created in code. A swap only replaces the RGB of an
	/// authored colour, its alpha is kept: the alpha carries the strength of a surface, not its hue.
	/// </para>
	/// <para>
	/// A texture cannot be recoloured by a tint, so the swaps only reach what the UI draws with a tint: the
	/// accents, the overlays and the text. What a tint cannot reach is described separately, by
	/// <see cref="background"/>: a <see cref="BackgroundTemplate"/> the panel draws with, so the look of the
	/// background is data a mod can ship, edit and share instead of a field of the theme.
	/// </para>
	/// </summary>
	[CreateAssetMenu(fileName = "Theme", menuName = "Nox/Theme", order = 2)]
	public class ThemeAsset : ScriptableObject {
		/// <summary>Distance under which two channels are the same colour (one step of 8 bits).</summary>
		private const float MATCH = 1f / 255f;

		/// <summary>Brightness kept for a themed highlight: the glow is HDR, far above white.</summary>
		private const float HIGHLIGHT_GAIN = 2.2f;

		[Serializable]
		public class Swap {
			public Color from = Color.white;
			public Color to = Color.white;

			/// <summary>
			/// Lowest alpha the authored colour must have for the swap to apply, or a negative value to ignore the
			/// alpha. It is what lets a theme darken the text (an opaque white) without also darkening the washes
			/// authored in the same white at a lower alpha.
			/// </summary>
			public float alphaMin = -1f;

			/// <summary>
			/// Highest alpha the authored colour may have, or a negative value to ignore the alpha: the same white is used
			/// for a control and for the border drawn on it, and a light or a dark theme draws the two differently.
			/// </summary>
			public float alphaMax = -1f;
		}

		/// <summary>
		/// Background of a themed panel: the <see cref="Theme.Background"/> the panel draws with, or <c>null</c> to leave
		/// the prefab alone.
		/// <para>
		/// A theme only names a background: what it draws and what it animates lives in the extension
		/// (<see cref="PerlinBackground"/>, <see cref="ParallaxBackground"/>...), so a look can be edited on its own,
		/// shared by several themes, and written by a mod without touching the theme it plugs into.
		/// </para>
		/// </summary>
		public Background background;

		/// <summary>Id of the theme, stored in the settings.</summary>
		public string id = "nox";

		public List<Swap> colors = new();

		/// <summary>
		/// Grey a monochrome theme pulls the colours it has no swap for: 0 for black, 1 for white, and a negative value
		/// to leave them alone. A hue theme lists every surface it knows, but a light or a dark scheme has to cover the
		/// ones the theme was never told about (a panel scrim, a border, a widget), or they keep the colour of the
		/// previous one. The accents and the text are still swapped, so only the surfaces take the tone.
		/// </summary>
		public float tone = -1f;

		/// <summary>How far a monochrome theme pulls the colours it does not swap (see <see cref="tone"/>).</summary>
		[Range(0f, 1f)]
		public float sweep = 0.8f;

		/// <summary>
		/// Colour the glyphs authored in white (the text and the icons) are drawn with. A light theme sets a dark ink; a
		/// theme that leaves it alone keeps the white of the UI.
		/// <para>
		/// Only the glyphs take it: the same white is also the neutral tint of the panels, of the bars and of the
		/// gradients, which follow the swaps and the tone instead. Inking everything would paint the panels dark, which
		/// is the opposite of what a light theme wants.
		/// </para>
		/// </summary>
		public Color ink = Color.white;

		/// <summary>
		/// Language key of the name of the theme. A pack ships the translation (see the settings pack) and a
		/// pack written by a mod can translate it too; a key no pack defines stays visible as
		/// <c>[theme.&lt;id&gt;]</c>, the usual marker of a missing translation.
		/// </summary>
		public string LabelKey
			=> $"theme.{id}";

		/// <summary>Colour of the theme for an authored colour, or the authored colour itself.</summary>
		/// <param name="source">Colour written on the graphic or in the colour block.</param>
		/// <param name="glyph">
		/// Whether the graphic is a text or an icon, so the white of the UI means ink rather than a neutral surface.
		/// </param>
		public Color Map(Color source, bool glyph = false) {
			if (glyph && Same(source, Ink))
				return new Color(ink.r, ink.g, ink.b, source.a);

			foreach (var swap in colors) {
				if (!Same(swap.from, source))
					continue;

				if (swap.alphaMin >= 0f && source.a < swap.alphaMin - MATCH)
					continue;

				if (swap.alphaMax >= 0f && source.a > swap.alphaMax + MATCH)
					continue;

				return new Color(swap.to.r, swap.to.g, swap.to.b, source.a);
			}

			return tone >= 0f
				? Shade(source, tone, sweep)
				: source;
		}

		/// <summary>Swaps an accent towards another one, keeping its variations.</summary>
		/// <param name="from">Colour the UI is authored with.</param>
		/// <param name="to">Colour the theme draws instead.</param>
		/// <param name="alphaMin">Lowest alpha the authored colour must have, or a negative value for any.</param>
		/// <param name="alphaMax">Highest alpha the authored colour may have, or a negative value for any.</param>
		public void Add(Color from, Color to, float alphaMin = -1f, float alphaMax = -1f)
			=> colors.Add(new Swap { from = from, to = to, alphaMin = alphaMin, alphaMax = alphaMax });

		private static bool Same(Color a, Color b)
			=> Mathf.Abs(a.r - b.r) < MATCH
			   && Mathf.Abs(a.g - b.g) < MATCH
			   && Mathf.Abs(a.b - b.b) < MATCH;

		/// <summary>Accent of the UI: the blue every panel draws its selected state with.</summary>
		public static readonly Color Accent = new(0.125f, 0.588f, 0.953f, 1f);

		/// <summary>Soft variation of the accent, used for the washes behind a hovered item.</summary>
		public static readonly Color SoftAccent = new(0.392f, 0.584f, 0.929f, 1f);

		/// <summary>Second accent, used by the highlights of the radial menu.</summary>
		public static readonly Color BrightAccent = new(0f, 0.600f, 1f, 1f);

		/// <summary>
		/// Hover glow of the grabbables (<c>OutlineGlowFeature</c>), in HDR: it is a renderer colour, not a tint of
		/// the UI, so a theme swaps it as well to keep the highlight in the hue of the accent.
		/// </summary>
		public static readonly Color Highlight = new(2.5f, 0.9f, 0.1f, 1f);

		/// <summary>
		/// Surface of the panels and of the orbiters: the grey the layout is authored with. A theme swaps it so the
		/// panels, the orbits and the containers follow the accent instead of staying grey next to a coloured UI.
		/// </summary>
		public static readonly Color Surface = new(0.18039216f, 0.18039216f, 0.18039216f, 1f);

		/// <summary>Darker surface, behind the content (radial menu, rows of a profile).</summary>
		public static readonly Color DeepSurface = new(0.14901961f, 0.14901961f, 0.14901961f, 1f);

		/// <summary>
		/// Surface under the cursor: the hovered variation of <see cref="Surface"/>, the colour a round button of an
		/// orbiter takes when it is pointed at. Without a swap it stays grey and stands out of a themed UI.
		/// </summary>
		public static readonly Color HoverSurface = new(0.13333334f, 0.13333334f, 0.13333334f, 1f);

		/// <summary>Surface held down: the pressed variation of <see cref="Surface"/>.</summary>
		public static readonly Color PressedSurface = new(0.38431373f, 0.38431373f, 0.38431373f, 1f);

		/// <summary>Wash of a container: drawn with a low alpha, which a swap keeps.</summary>
		public static readonly Color ContainerSurface = new(0.4f, 0.4f, 0.4f, 0.1254902f);

		/// <summary>Bar above a panel (header).</summary>
		public static readonly Color HeaderSurface = new(0.3f, 0.3f, 0.3f, 1f);

		/// <summary>Overlay drawn over the world (foreground, widget).</summary>
		public static readonly Color OverlaySurface = new(0.1f, 0.1f, 0.1f, 1f);

		/// <summary>
		/// Moving part of a control: the knob of a toggle, the handle of a slider. It is nearly white, so it takes the
		/// hue of the theme without losing the contrast that makes it read as the grip.
		/// </summary>
		public static readonly Color Handle = new(0.89411765f, 0.89411765f, 0.89411765f, 1f);

		/// <summary>
		/// Text and icons: the UI authors them in plain white, which is also the neutral tint of its panels. A theme
		/// reaches the first through <see cref="ink"/> and the second through <see cref="Scrim"/> and the tone, so the
		/// two never have to be told apart by their colour.
		/// </summary>
		public static readonly Color Ink = new(1f, 1f, 1f, 1f);

		/// <summary>
		/// Backdrop of the panels: the near-black scrim they are drawn with, opaque enough to hide the world behind
		/// them. A theme decides how dark a panel is, so this is the swatch to swap to make a panel light.
		/// </summary>
		public static readonly Color Scrim = new(0f, 0f, 0f, 1f);

		/// <summary>Accent of the black theme: near-white, so a selected state still reads on the black panels.</summary>
		private static readonly Color NightAccent = new(0.92f, 0.94f, 0.98f, 1f);

		/// <summary>Panel of the black theme: #121212, the page, not a hole.</summary>
		private static readonly Color NightPanel = new(0.0706f, 0.0706f, 0.0706f, 1f);

		/// <summary>
		/// Surface of the black theme: the white the UI tints its controls with, lifted above the panel (#38 on #12) so a
		/// button reads as a button while staying in the background.
		/// </summary>
		private static readonly Color NightSurface = new(0.22f, 0.22f, 0.23f, 1f);

		/// <summary>
		/// Outline of the black theme: the translucent white the UI draws its borders with, kept light so a control still
		/// reads as a shape when it sits on the panel.
		/// </summary>
		private static readonly Color NightEdge = new(0.55f, 0.55f, 0.57f, 1f);

		/// <summary>Accent of the light theme: near-black, the only ink that reads on a white panel.</summary>
		private static readonly Color DayAccent = new(0.04f, 0.04f, 0.05f, 1f);

		/// <summary>Panel of the light theme: white, slightly cool so the pure white of the content still reads.</summary>
		private static readonly Color DayPanel = new(0.902f, 0.902f, 0.918f, 1f);

		/// <summary>Ink of the light theme: the text and the icons, dark enough to read on a white panel.</summary>
		private static readonly Color DayInk = new(0.08f, 0.08f, 0.09f, 1f);

		/// <summary>
		/// Surface of the light theme: the white tint of the controls, greyed to a visible shape (#9E) on a panel almost
		/// white, so a button is not lost in the page.
		/// </summary>
		private static readonly Color DaySurface = new(0.62f, 0.62f, 0.65f, 1f);

		/// <summary>Outline of the light theme: the translucent white of the borders, pulled to a grey that reads on white.</summary>
		private static readonly Color DayEdge = new(0.35f, 0.35f, 0.37f, 1f);

		/// <summary>
		/// Themes every install has. The accents are the only hues the UI does not take from a texture, so a
		/// theme swaps them; <c>nox</c> is the identity, so the default look never depends on the mapping.
		/// <para>
		/// The last two are monochrome on purpose: they drop the hue and pull every surface to black or to white,
		/// which is all a scheme without an accent needs.
		/// </para>
		/// </summary>
		public static ThemeAsset[] Builtin()
			=> new[] {
				Create("nox", Accent, Look("perlin")),
				Create("mint", new Color(0.129f, 0.831f, 0.631f, 1f), Look("perlin")),
				Create("violet", new Color(0.627f, 0.396f, 0.969f, 1f), Look("perlin")),
				Create("amber", new Color(0.980f, 0.706f, 0.239f, 1f), Look("perlin")),
				Night(),
				Day(),
			};

		/// <summary>
		/// Black theme: the panels become the page (#121212), the white surfaces of the controls are dimmed to sit just
		/// above it, the text and the icons stay white, and the accent is the only light left.
		/// </summary>
		private static ThemeAsset Night() {
			var theme = Create("black", NightAccent, Look("dark"), 0f, 0.5f);
			theme.Add(Scrim, Scrim, alphaMax: 0.85f);
			theme.Add(Scrim, NightPanel, alphaMin: 0.85f);
			theme.Add(Ink, NightEdge, alphaMax: 0.5f);
			theme.Add(Ink, NightSurface);
			return theme;
		}

		/// <summary>
		/// Light theme (<c>eyeburning</c>): the panels and the scrims pulled to white, the text and the icons inked to
		/// dark, and the near-white grip darkened, or the content would vanish in the panel.
		/// </summary>
		private static ThemeAsset Day() {
			var theme = Create("eyeburning", DayAccent, Look("light"), 1f, 0.62f);
			theme.Add(Scrim, Scrim, alphaMax: 0.85f);
			theme.Add(Scrim, DayPanel, alphaMin: 0.85f);
			theme.Add(Ink, DayEdge, alphaMax: 0.5f);
			theme.Add(Ink, DaySurface);
			theme.Add(Handle, new Color(0.55f, 0.55f, 0.57f, 1f));
			theme.ink = DayInk;
			return theme;
		}

		/// <summary>Built-in backgrounds, built once: the themes share the same instances.</summary>
		private static Background[] _looks;

		/// <summary>Built-in background of an id, or <c>null</c>.</summary>
		/// <param name="id">Id of the background.</param>
		private static Background Look(string id) {
			_looks ??= Background.Builtin();

			foreach (var look in _looks)
				if (look.id == id)
					return look;

			return null;
		}

		private static ThemeAsset Create(
			string id, Color accent, Background background = null, float? tone = null, float sweep = 0.8f) {
			var theme = CreateInstance<ThemeAsset>();
			theme.name = $"[Theme_{id}]";
			theme.hideFlags = HideFlags.DontSave;
			theme.id = id;
			theme.background = background;
			theme.tone = tone ?? -1f;
			theme.sweep = sweep;

			if (accent == Accent)
				return theme;

			theme.background = background
				? background.Adapt(accent)
				: null;
			theme.Add(Accent, accent);
			theme.Add(SoftAccent, Color.Lerp(accent, Color.white, 0.35f));
			theme.Add(BrightAccent, accent);

			theme.Add(Highlight, new Color(accent.r * HIGHLIGHT_GAIN, accent.g * HIGHLIGHT_GAIN, accent.b * HIGHLIGHT_GAIN, 1f));

			if (tone.HasValue)
				return theme;

			theme.Add(Surface, Tint(Surface, accent));
			theme.Add(DeepSurface, Tint(DeepSurface, accent));
			theme.Add(HoverSurface, Tint(HoverSurface, accent));
			theme.Add(PressedSurface, Tint(PressedSurface, accent));
			theme.Add(ContainerSurface, Tint(ContainerSurface, accent));
			theme.Add(HeaderSurface, Tint(HeaderSurface, accent));
			theme.Add(OverlaySurface, Tint(OverlaySurface, accent));

			return theme;
		}

		/// <summary>
		/// Grey a theme draws a surface with: pulled towards <paramref name="tone"/> by <paramref name="sweep"/> (0 =
		/// black, 1 = white). The alpha of the surface is kept, it carries the strength of the wash.
		/// </summary>
		/// <param name="surface">Authored colour of the surface.</param>
		/// <param name="tone">Grey the surfaces are pulled to.</param>
		/// <param name="sweep">How far they are pulled.</param>
		private static Color Shade(Color surface, float tone, float sweep)
			=> new(
				Mathf.Lerp(surface.r, tone, sweep),
				Mathf.Lerp(surface.g, tone, sweep),
				Mathf.Lerp(surface.b, tone, sweep),
				surface.a
			);

		/// <summary>
		/// Tints a grey surface with the hue of an accent, keeping its brightness: a panel keeps its place in the
		/// contrast of the layout, it only stops being grey. The alpha of the surface is kept, it carries its strength.
		/// </summary>
		/// <param name="surface">Authored grey the layout draws with.</param>
		/// <param name="accent">Accent of the theme.</param>
		public static Color Tint(Color surface, Color accent) {
			var accentLuminance = Mathf.Max(0.02f, accent.grayscale);
			var scale = surface.grayscale / accentLuminance;
			return new Color(
				accent.r * scale,
				accent.g * scale,
				accent.b * scale,
				surface.a
			);
		}
	}
}
