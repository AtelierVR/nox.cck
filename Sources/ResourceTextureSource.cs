using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.Search {
	/// <summary>
	/// Texture a CCK editor uses as its icon, given either already loaded (<see cref="Texture2D"/>) or as the
	/// resource it lives in (<c>"ui:icons/custom_menu.png"</c>).
	/// <para>
	/// This only describes the source: resolving the resource through the assets of the loaded mods is up to
	/// the consumer (<c>Nox.CCK.Editor.InspectorIcon</c> for the inspectors).
	/// </para>
	/// </summary>
	public class ResourceTextureSource {
		/// <summary>The texture itself, or <c>null</c> when the source names a <see cref="Resource"/>.</summary>
		public Texture2D Texture { get; }

		/// <summary>The resource to resolve, or <see cref="ResourceIdentifier.Invalid"/> for a direct texture.</summary>
		public ResourceIdentifier Resource { get; }

		public ResourceTextureSource(Texture2D texture)
			=> Texture = texture;

		public ResourceTextureSource(ResourceIdentifier resource)
			=> Resource = resource;

		/// <summary>Whether <see cref="Texture"/> is already available, i.e. nothing has to be resolved.</summary>
		public bool HasTexture
			=> Texture != null;

		/// <summary>Whether the source names a resource to resolve.</summary>
		public bool HasResource
			=> Resource.IsValid();

		public static implicit operator ResourceTextureSource(Texture2D texture)
			=> new(texture);

		public static implicit operator ResourceTextureSource(ResourceIdentifier resource)
			=> new(resource);

		/// <summary>Reads the identifier the CCK uses everywhere (<c>"namespace:path/to/asset.ext"</c>).</summary>
		public static implicit operator ResourceTextureSource(string resource)
			=> new(ResourceIdentifier.Parse(resource));

		public override string ToString()
			=> HasResource ? Resource.ToString() : Texture ? Texture.name : "none";
	}
}
