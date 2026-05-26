#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.CCK.Mods.Assets {
	/// <summary>
	/// Editor-only static registry of <see cref="IAssetAPI"/> instances.
	/// Allows resolving assets by <see cref="ResourceIdentifier"/> from any editor context
	/// (inspectors, editor windows, etc.) without a mod reference.
	/// </summary>
	public static class EditorGlobalAsset {
		private static readonly List<IAssetAPI> _apis = new();

        public static IEnumerable<IAssetAPI> Registered 
			=> _apis.AsReadOnly();

		/// <summary>Registers an <see cref="IAssetAPI"/> so it participates in global lookups.</summary>
		public static void Register(IAssetAPI api) {
			if (!_apis.Contains(api)) _apis.Add(api);
		}

		/// <summary>Removes a previously registered <see cref="IAssetAPI"/>.</summary>
		public static void Unregister(IAssetAPI api) => _apis.Remove(api);

		// ── Asset ─────────────────────────────────────────────────────────────

		public static bool HasAsset<T>(ResourceIdentifier path)
			where T : Object
			=> _apis.Any(api => api.HasAsset<T>(path));

		public static T GetAsset<T>(ResourceIdentifier path)
			where T : Object
			=> _apis.Select(api => api.GetAsset<T>(path)).FirstOrDefault(a => a != null);

		// ── Internal asset ────────────────────────────────────────────────────

		public static bool HasInternalAsset<T>(ResourceIdentifier path)
			where T : Object
			=> _apis.Any(api => api.HasInternalAsset<T>(path));

		public static T GetInternalAsset<T>(ResourceIdentifier path)
			where T : Object
			=> _apis.Select(api => api.GetInternalAsset<T>(path)).FirstOrDefault(a => a != null);
	}
}
#endif
