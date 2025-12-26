using System;
using System.Linq;

namespace Nox.CCK.Utils {
	[Serializable]
	public struct ResourceIdentifier {
		public static ResourceIdentifier Invalid
			=> default;

		public const char   PathSeparator    = '/';
		public const char   PartSeparator    = ':';
		public const string DefaultNamespace = "";

		public bool IsValid()
			=> !string.IsNullOrEmpty(@namespace) || path is { Length: > 0 };

		public string   @namespace;
		public string[] path;

		public string Namespace
			=> @namespace;

		public string[] SplitPath
			=> path;

		public string Path
			=> string.Join(PathSeparator, path);

		public ResourceIdentifier(string @namespace, string[] path) {
			this.@namespace = @namespace;
			this.path       = path;
		}

		public ResourceIdentifier(string @namespace, string path) {
			this.@namespace = @namespace;
			this.path = path.Split(PathSeparator)
				.Where(p => !string.IsNullOrEmpty(p))
				.ToArray();
		}

		public static ResourceIdentifier Parse(string identifier) {
			if (string.IsNullOrEmpty(identifier))
				return Invalid;

			var groupAndPath = identifier.Split(PartSeparator, 2);
			var group        = groupAndPath.Length > 1 ? groupAndPath[0] : DefaultNamespace;
			var path         = groupAndPath.Length > 1 ? groupAndPath[1] : groupAndPath[0];

			return new ResourceIdentifier(group, path);
		}

		public bool HasNamespace()
			=> !string.IsNullOrEmpty(@namespace);

		public bool HasPath()
			=> path.Length > 0;

		public override string ToString()
			=> $"{@namespace}{(HasPath() && HasNamespace() ? PartSeparator : "")}{string.Join(PathSeparator, path)}";

		public static implicit operator string(ResourceIdentifier identifier)
			=> identifier.ToString();

		public static implicit operator ResourceIdentifier(string identifier)
			=> Parse(identifier);
	}
}