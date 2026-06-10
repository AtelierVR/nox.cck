using System;

namespace Nox.CCK.Mods.Metadata {
	/// <summary>
	/// Represents a parsed entrypoint with optional explicit assembly.
	/// Supports three JSON formats:
	///   - "Namespace.Class"                  (assembly auto-discovered)
	///   - "Assembly:Namespace.Class"         (absolute)
	///   - { assembly, namespace, class }     (object)
	/// </summary>
	public class EntryPointElement {
		/// <summary>Optional assembly name. When null, the assembly is auto-discovered.</summary>
		public string Assembly;

		/// <summary>Full namespace (e.g. "Nox.Avatars.Runtime").</summary>
		public string Namespace;

		/// <summary>Class name (e.g. "Main").</summary>
		public string Class;

		/// <summary>Reconstructed full type name: Namespace.Class.</summary>
		public string FullName 
            => string.IsNullOrEmpty(Namespace) 
                ? Class 
                : $"{Namespace}.{Class}";

		/// <summary>Reconstructed absolute string: Assembly:Namespace.Class (or Namespace.Class if no assembly).</summary>
		public string AbsoluteName 
            => string.IsNullOrEmpty(Assembly) 
                ? FullName 
                : $"{Assembly}:{FullName}";

		public override string ToString()
			=> AbsoluteName;

		/// <summary>
		/// Parse a string in one of the supported formats:
		///   "Namespace.Class"           → assembly = null
		///   "Assembly:Namespace.Class"  → assembly = "Assembly"
		/// </summary>
		public static EntryPointElement Parse(string value) {
			if (string.IsNullOrEmpty(value))
				return null;

			var element = new EntryPointElement();

			// Format: "Assembly:Namespace.Class"
			var colonIdx = value.IndexOf(':');
			if (colonIdx > 0) {
				element.Assembly = value.Substring(0, colonIdx);
				value = value.Substring(colonIdx + 1);
			}

			// Format: "Namespace.Class"
			var lastDot = value.LastIndexOf('.');
			if (lastDot > 0) {
				element.Namespace = value.Substring(0, lastDot);
				element.Class     = value.Substring(lastDot + 1);
			} else {
				element.Class = value;
			}

			return element;
		}

		/// <summary>Legacy string format: "Namespace.Class" (backward compatible).</summary>
		public static implicit operator string(EntryPointElement e)
			=> e?.FullName;

		/// <summary>Parse from legacy string format.</summary>
		public static implicit operator EntryPointElement(string s)
			=> Parse(s);
	}
}
