using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.Search {
	/// <summary>
	/// The image of a search result: either a remote url (downloaded lazily through a
	/// network image component, size aware and cached) or an already loaded
	/// <see cref="Texture2D"/>.
	/// Is a <see cref="Sources"/> holding a <see cref="string"/> and/or a <see cref="Texture2D"/>.
	/// </summary>
	public class ImageSource : Sources {
		public ImageSource(string url = null, Texture2D texture = null) : base(url, texture) { }

		/// <summary>The remote url of the image, or <c>null</c> when a texture is provided.</summary>
		public string Url
			=> TryGet<string>(out var url) ? url : null;

		/// <summary>The already loaded texture, or <c>null</c> when an url is provided.</summary>
		public Texture2D Texture
			=> TryGet<Texture2D>(out var texture) ? texture : null;

		/// <summary>Whether the image is backed by a non empty remote url.</summary>
		public bool HasUrl
			=> TryGet<string>(out var url) && !string.IsNullOrEmpty(url);

		/// <summary>Whether the image is backed by a texture.</summary>
		public bool HasTexture
			=> TryGet<Texture2D>(out var texture) && texture != null;

		/// <summary>An image backed by a remote url (may be null or empty).</summary>
		public static ImageSource FromUrl(string url)
			=> new(url);

		/// <summary>An image backed by an already loaded texture.</summary>
		public static ImageSource FromTexture(Texture2D texture)
			=> new(null, texture);

		/// <summary>An empty image: nothing to display.</summary>
		public new static ImageSource Empty
			=> new();

		public static implicit operator ImageSource(string url)
			=> new(url);

		public static implicit operator ImageSource(Texture2D texture)
			=> new(null, texture);
	}
}
