using System.Text;

namespace Nox.CCK.Utils {
	public static class Format {
		public static string ToSnakeCase(this string input) {
			if (string.IsNullOrEmpty(input))
				return input;

			var sb = new StringBuilder(input.Length + 4);

			var lastWasUnderscore = false;

			for (var i = 0; i < input.Length; i++) {
				var c = input[i];

				if (c >= 'A' && c <= 'Z') {
					if (i > 0 && !lastWasUnderscore)
						sb.Append('_');
					sb.Append((char)(c + 32));
					lastWasUnderscore = false;
				} else if (c < 128) {
					if (c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == '\f' || c == '\v') {
						if (lastWasUnderscore) continue;
						sb.Append('_');
						lastWasUnderscore = true;
					} else {
						sb.Append(c);
						lastWasUnderscore = false;
					}
				} else if (char.IsUpper(c)) {
					if (i > 0 && !lastWasUnderscore)
						sb.Append('_');
					sb.Append(char.ToLowerInvariant(c));
					lastWasUnderscore = false;
				} else if (char.IsWhiteSpace(c)) {
					if (lastWasUnderscore) continue;
					sb.Append('_');
					lastWasUnderscore = true;
				} else {
					sb.Append(c);
					lastWasUnderscore = false;
				}
			}

			return sb.ToString().Trim('_');
		}
	}
}