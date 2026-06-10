using System;

namespace Nox.CCK.Mods.Metadata {
	/// <summary>
	/// Flags controlling how entrypoints are serialized to JSON.
	/// </summary>
	[Flags]
	public enum EntryPointFormat {
		/// <summary>Default: use absolute string format "Assembly:Namespace.Class" (or "Namespace.Class" if no assembly).</summary>
		None = 0,

		/// <summary>
		/// Force object format: { "assembly": ..., "namespace": ..., "class": ... }.
		/// When absent, the absolute string format is used.
		/// </summary>
		EntryPointObject = 1 << 0,
	}
}
