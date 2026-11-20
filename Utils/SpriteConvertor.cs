using UnityEngine;

namespace Nox.CCK.Utils {
	/// <summary>
	/// Builds a sprite from a runtime texture. <see cref="Sprite.Create(Texture2D, Rect, Vector2)"/> traces a tight
	/// mesh on every call (measured at 20 to 90 ms for the icons of a page), so it is always created as a
	/// full-rect quad: the result is a plain quad over the whole texture, with no mesh to trace.
	/// </summary>
	public static class SpriteConvertor {
		public static Sprite Convert(Texture2D texture) {
			if (!texture)
				return null;

			return Sprite.Create(
				texture,
				new Rect(0, 0, texture.width, texture.height),
				new Vector2(0.5f, 0.5f),
				100f,
				0,
				SpriteMeshType.FullRect
			);
		}
	}
}
