using System;

namespace Nox.CCK.Utils {
	[Serializable]
	public struct ResourceIdentifier {
		public static ResourceIdentifier Invalid
			=> default;

		public const char PATH_SEPARATOR = '/';
		public const char PART_SEPARATOR = ':';
		public const string DEFAULT_NAMESPACE = "";

		public bool IsValid()
			=> !string.IsNullOrEmpty(@namespace) || path is { Length: > 0 };

		public string @namespace;
		public string[] path;

		public string Namespace
			=> @namespace;

		public string[] SplitPath
			=> path;

		public string Path
			=> string.Join(PATH_SEPARATOR, path);

		public ResourceIdentifier(string @namespace, string[] path) {
			this.@namespace = @namespace;
			this.path = path;
		}

		public ResourceIdentifier(string @namespace, string path) {
			this.@namespace = @namespace;
			this.path = Split(path);
		}

		private static string[] Split(string path) {
			if (string.IsNullOrEmpty(path))
				return Array.Empty<string>();

			var count = 0;
			var start = 0;

			for (var i = 0; i <= path.Length; i++)
				if (i == path.Length || path[i] == PATH_SEPARATOR) {
					if (i > start)
						count++;
					start = i + 1;
				}

			if (count == 0)
				return Array.Empty<string>();

			var parts = new string[count];
			var index = 0;
			start = 0;

			for (var i = 0; i <= path.Length; i++)
				if (i == path.Length || path[i] == PATH_SEPARATOR) {
					if (i > start)
						parts[index++] = path.Substring(start, i - start);
					start = i + 1;
				}

			return parts;
		}

		public static ResourceIdentifier Parse(string identifier) {
			if (string.IsNullOrEmpty(identifier))
				return Invalid;

			var separator = identifier.IndexOf(PART_SEPARATOR);

			return separator < 0
				? new ResourceIdentifier(DEFAULT_NAMESPACE, identifier)
				: new ResourceIdentifier(identifier[..separator], identifier[(separator + 1)..]);
		}

		public bool HasNamespace()
			=> !string.IsNullOrEmpty(@namespace);

		public bool HasPath()
			=> path.Length > 0;

		public override string ToString()
			=> $"{@namespace}{(HasPath() && HasNamespace() ? PART_SEPARATOR : "")}{string.Join(PATH_SEPARATOR, path)}";

		public static implicit operator string(ResourceIdentifier identifier)
			=> identifier.ToString();

		public static implicit operator ResourceIdentifier(string identifier)
			=> Parse(identifier);
	}
}
