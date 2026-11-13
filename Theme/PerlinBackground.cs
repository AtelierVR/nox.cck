using UnityEngine;

namespace Nox.CCK.Theme {
	/// <summary>
	/// Animated noise background: the perlin of <c>FluidBackground</c>, as a look a theme can pick and edit.
	/// <para>
	/// The extension owns the parameters of the shader, so the look is a background asset and not a field spread over
	/// the theme: two themes can share one, and a variant is a copy of the asset with other colours.
	/// </para>
	/// </summary>
	[CreateAssetMenu(fileName = "PerlinBackground", menuName = "Nox/Background/Perlin", order = 3)]
	public class PerlinBackground : Background {
		private static readonly int _color1Property = Shader.PropertyToID("_Color1");
		private static readonly int _color2Property = Shader.PropertyToID("_Color2");
		private static readonly int _speedProperty = Shader.PropertyToID("_Speed");
		private static readonly int _scaleProperty = Shader.PropertyToID("_Scale");
		private static readonly int _complexityProperty = Shader.PropertyToID("_Complexity");
		private static readonly int _fluidProperty = Shader.PropertyToID("_FluidStrength");
		private static readonly int _stepsProperty = Shader.PropertyToID("_Steps");
		private static readonly int _pixelNoiseProperty = Shader.PropertyToID("_PixelNoiseStrength");

		[Header("Perlin")]
		[Tooltip("Couleur de la première moitié de la séparation du bruit.")]
		public Color color1 = new(0.149f, 0.149f, 0.149f, 1f);

		[Tooltip("Couleur de la seconde moitié de la séparation du bruit.")]
		public Color color2 = new(0.2f, 0.2f, 0.2f, 1f);

		[Tooltip("Vitesse de dérive du bruit.")]
		public float speed = 0.15f;

		[Tooltip("Échelle du bruit : plus grand = motifs plus petits.")]
		public float scale = 10f;

		[Range(1, 5)]
		[Tooltip("Nombre d'octaves du bruit.")]
		public int complexity = 3;

		[Range(0.1f, 2f)]
		[Tooltip("Force de la déformation du fluide.")]
		public float fluidStrength = 1f;

		[Range(1, 10)]
		[Tooltip("Nombre de bandes entre les deux couleurs.")]
		public int steps = 3;

		[Range(0f, 1f)]
		[Tooltip("Grain par pixel ajouté par-dessus le bruit.")]
		public float pixelNoise = 0.002f;

		public override void Apply(Material target) {
			if (!target)
				return;

			if (target.HasProperty(_color1Property))
				target.SetColor(_color1Property, color1);

			if (target.HasProperty(_color2Property))
				target.SetColor(_color2Property, color2);

			if (target.HasProperty(_speedProperty))
				target.SetFloat(_speedProperty, speed);

			if (target.HasProperty(_scaleProperty))
				target.SetFloat(_scaleProperty, scale);

			if (target.HasProperty(_complexityProperty))
				target.SetFloat(_complexityProperty, complexity);

			if (target.HasProperty(_fluidProperty))
				target.SetFloat(_fluidProperty, fluidStrength);

			if (target.HasProperty(_stepsProperty))
				target.SetFloat(_stepsProperty, steps);

			if (target.HasProperty(_pixelNoiseProperty))
				target.SetFloat(_pixelNoiseProperty, pixelNoise);
		}

		/// <summary>
		/// Same noise, in the greys of a theme: both colours keep their brightness and only take a hint of the accent
		/// (see <see cref="Background.tint"/>). The look is copied, so the asset a mod ships keeps its authored greys.
		/// </summary>
		public override Background Adapt(Color accent) {
			var look = CreateInstance<PerlinBackground>();
			look.name = $"{name} ({ColorUtility.ToHtmlStringRGB(accent)})";
			look.hideFlags = HideFlags.DontSave;
			look.id = id;
			look.material = material;
			look.tint = tint;
			look.color1 = Tinted(color1, accent);
			look.color2 = Tinted(color2, accent);
			look.speed = speed;
			look.scale = scale;
			look.complexity = complexity;
			look.fluidStrength = fluidStrength;
			look.steps = steps;
			look.pixelNoise = pixelNoise;
			return look;
		}
	}
}
