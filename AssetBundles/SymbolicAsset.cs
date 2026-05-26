using UnityEngine;

namespace Nox.CCK {
	/// <summary>
	/// A ScriptableObject that acts as a symbolic link to another asset.
	/// When the asset system resolves an asset and finds a SymbolicAsset,
	/// it transparently redirects the request to the target path.
	/// </summary>
	/// <example>
	/// Instead of duplicating a file, create a SymbolicAsset at
	/// <c>settings:icons/avatar</c> pointing to <c>avatars:ui/icons/avatar.png</c>.
	/// </example>
	[CreateAssetMenu(fileName = "symbolic", menuName = "Nox/SymbolicAsset")]
	public class SymbolicAsset : ScriptableObject {
		[Tooltip("Target resource path, e.g. 'avatars:ui/icons/avatar.png'")]
		[SerializeField] private string target;

		/// <summary>
		/// The resource identifier this asset redirects to.
		/// </summary>
		public string Target => target;
	}
}
