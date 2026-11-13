using Nox.CCK.Mods.Assets;
using Nox.Search;
using UnityEditor;
using UnityEngine;

namespace Nox.CCK.Editor {
	/// <summary>
	/// Resolution of the <see cref="ResourceTextureSource"/> declared by a CCK editor as its icon.
	/// <para>
	/// The lookup goes through the assets of the loaded mods: <see cref="EditorGlobalAsset"/> memoizes it, so an
	/// inspector calling this on every repaint costs one dictionary hit.
	/// </para>
	/// </summary>
	public static class InspectorIcon {
		/// <summary>
		/// The texture of <paramref name="source"/>: the one it holds, or the resource it names. <c>null</c> when
		/// there is nothing to draw (no source, or a resource that does not exist).
		/// </summary>
		public static Texture2D Resolve(ResourceTextureSource source) {
			if (source == null)
				return null;

			return source.HasResource 
                ? EditorGlobalAsset.GetAsset<Texture2D>(source.Resource) 
                : source.Texture;
		}

		/// <summary>
		/// Gives the icon to <paramref name="target"/>: the header of the inspector window and the hierarchy then
		/// draw it instead of the script icon. Does nothing when the source resolves to nothing.
		/// </summary>
		public static void Apply(Object target, ResourceTextureSource source) {
			if (target == null)
				return;

			if (Resolve(source) is { } icon)
				EditorGUIUtility.SetIconForObject(target, icon);
		}
	}
}
