using System.Text.RegularExpressions;
using System;

namespace Nox.CCK.Utils {
	public static class VersionExtensions {
		/// <summary>
		/// Parse a version string supporting wildcard placeholders (x, X, *),
		/// replacing them with 0. CI pipelines often use 1.0.x as a placeholder.
		/// </summary>
		public static Version From(string raw) {
			var sanitized = Regex.Replace(raw, @"[xX*]", "0");
			return new Version(sanitized);
		}
	}
}
