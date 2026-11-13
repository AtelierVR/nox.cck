using UnityEngine;

namespace Nox.CCK.Theme {
	/// <summary>
	/// Background effect: the look it decorates slides against the camera instead of staying pinned to the screen.
	/// <para>
	/// It is an extension, not a field of the theme, so any look can be given the effect without knowing about it: the
	/// motion itself is <c>BackgroundParallax</c>, which reads the strength written here and moves the shader offset
	/// with the camera.
	/// </para>
	/// </summary>
	[CreateAssetMenu(fileName = "ParallaxBackground", menuName = "Nox/Background/Parallax", order = 3)]
	public class ParallaxBackground : Background {
		private static readonly int _strengthProperty = Shader.PropertyToID("_ParallaxStrength");

		[Tooltip("Fond mis en parallaxe : son matériau et ses couleurs. Vide = celui du prefab.")]
		public Background look;

		[Tooltip("Décalage d'UV par mètre parcouru par la caméra.")]
		public Vector2 strength = new(0.12f, 0.12f);

		/// <inheritdoc/>
		public override Material Source
			=> look ? look.Source : base.Source;

		public override void Apply(Material target) {
			look?.Apply(target);

			if (!target || !target.HasProperty(_strengthProperty))
				return;

			target.SetVector(
				_strengthProperty,
				new Vector4(strength.x, strength.y, 0f, 0f)
			);
		}

		/// <summary>Same motion, wrapping the look brought towards the colours of the theme.</summary>
		public override Background Adapt(Color accent) {
			var parallax = CreateInstance<ParallaxBackground>();
			parallax.name = $"{name} ({ColorUtility.ToHtmlStringRGB(accent)})";
			parallax.hideFlags = HideFlags.DontSave;
			parallax.id = id;
			parallax.material = material;
			parallax.tint = tint;
			parallax.look = look ? look.Adapt(accent) : null;
			parallax.strength = strength;
			return parallax;
		}
	}
}
