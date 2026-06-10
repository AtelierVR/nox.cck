using System;

namespace Nox.CCK.Mods.Metadata {
	/// <summary>
	/// Flags controlling serialization format of mod metadata.
	/// </summary>
	[Flags]
	public enum ModMetadataFormat {
		/// <summary>Default: indented JSON, compact entrypoint strings.</summary>
		None = 0,

		/// <summary>
		/// Serialize entrypoints as objects { assembly?, namespace, class }
		/// instead of the default string format.
		/// </summary>
		EntryPointObject = 1 << 0,

		/// <summary>
		/// Output compact JSON (no indentation / line breaks).
		/// </summary>
		Compact = 1 << 1,
	}
}
