using System;
using System.Collections.Generic;

namespace Nox.CCK.Utils {
	/// <summary>
	/// Centralized library path conventions for platform, architecture, folder names and file extensions.
	/// </summary>
	public static class Library {
		/// <summary>
		/// Returns the library file extension for the given platform (e.g. ".dll", ".so", ".dylib").
		/// </summary>
		public static string GetExtension(Platform platform) {
			if (platform == Platform.Windows) return ".dll";
			if (platform == Platform.Linux)   return ".so";
			if (platform == Platform.MacOS)   return ".dylib";
			return null;
		}

        public static string CurrentLibraryExtension
			=> GetExtension(PlatformExtensions.CurrentPlatform);

		/// <summary>
		/// Returns the prioritized list of plugin sub-paths for the given platform and architecture,
		/// relative to the <c>Plugins</c> root, ordered from most specific to least:
		/// <list type="number">
		/// <item><description><c>&lt;platform&gt;/&lt;arch&gt;</c> — e.g. <c>windows/x64</c> (architecture-specific binaries)</description></item>
		/// <item><description><c>&lt;platform&gt;</c> — e.g. <c>windows</c> (binaries shared by every architecture of that platform)</description></item>
		/// <item><description><c>""</c> — the <c>Plugins</c> root itself (the "." fallback)</description></item>
		/// </list>
		/// Example for Windows x64: <c>["windows/x64", "windows", ""]</c>.
		/// Callers compose the full path with <c>Path.Combine(pluginsRoot, subPath)</c>; the empty
		/// sub-path resolves to <c>pluginsRoot</c> itself.
		/// </summary>
		public static string[] GetSubFolders(Platform platform, Architecture arch) {
			var folders = new List<string>();
			var platformName = platform.GetPlatformName();
			var archName = arch.GetArchitectureName();

			// 1. Platform + architecture (e.g. "windows/x64", "linux/arm64")
			if (!string.IsNullOrEmpty(platformName) && !string.IsNullOrEmpty(archName))
				folders.Add(platformName + "/" + archName);

			// 2. Platform only (e.g. "windows") — shared across all architectures
			if (!string.IsNullOrEmpty(platformName))
				folders.Add(platformName);

			// 3. Root fallback (".") — architecture/platform agnostic
			folders.Add("");

			return folders.ToArray();
		}

		/// <summary>
		/// Shortcut for <see cref="GetSubFolders"/> with the current platform and architecture.
		/// </summary>
		public static string[] CurrentSubFolders
			=> GetSubFolders(PlatformExtensions.CurrentPlatform, ArchitectureExtensions.CurrentArchitecture);

		/// <summary>
		/// Unity's own plugin folder names inside a <i>player build</i>, relative to the build's
		/// <c>&lt;dataPath&gt;/Plugins</c> folder, for the given platform and architecture.
		/// <para>
		/// Unity does not keep the editor's <c>&lt;platform&gt;/&lt;arch&gt;</c> layout in a build: it
		/// flattens every native plugin into <c>&lt;dataPath&gt;/Plugins/&lt;arch&gt;</c> using its own
		/// architecture names. Runtime code that looks for a plugin by walking
		/// <c>Plugins/&lt;platform&gt;/&lt;arch&gt;</c> (see <see cref="GetPluginSubFolders"/>) therefore
		/// has to probe these names as well, otherwise the binaries shipped by a mod
		/// (<c>&lt;mod&gt;/Plugins/windows/x64/foo.dll</c>) are never found in a player.</para>
		/// <para>macOS is not covered: its plugins live in <c>&lt;app&gt;.app/Contents/PlugIns</c>, i.e.
		/// outside <c>Application.dataPath</c>.</para>
		/// </summary>
		public static string[] GetRuntimePluginFolders(Platform platform, Architecture arch) {
			if (platform != Platform.Windows && platform != Platform.Linux)
				return Array.Empty<string>();

			var name = arch.GetArchitectureName();
			return string.IsNullOrEmpty(name) ? Array.Empty<string>() : new[] { name };
		}

		/// <summary>
		/// Full ordered list of sub-paths (relative to a <c>Plugins</c> root) where a native binary may
		/// live on the current platform, most specific first: <c>&lt;platform&gt;/&lt;arch&gt;</c>,
		/// <c>&lt;platform&gt;</c>, the <c>&lt;arch&gt;</c> folder of a player build
		/// (see <see cref="GetRuntimePluginFolders"/>), then <c>""</c> for the root itself.
		/// Example on Windows x64: <c>["windows/x64", "windows", "x64", ""]</c>.
		/// </summary>
		public static string[] GetPluginSubFolders(Platform platform, Architecture arch) {
			var folders = new List<string>(GetSubFolders(platform, arch));
			var runtime = GetRuntimePluginFolders(platform, arch);

			// Keep the empty (root) entry last: it is the last-resort fallback.
			if (runtime.Length > 0)
				folders.InsertRange(folders.Count - 1, runtime);

			return folders.ToArray();
		}

		/// <summary>
		/// Shortcut for <see cref="GetPluginSubFolders"/> with the current platform and architecture.
		/// </summary>
		public static string[] CurrentPluginSubFolders
			=> GetPluginSubFolders(PlatformExtensions.CurrentPlatform, ArchitectureExtensions.CurrentArchitecture);

		/// <summary>
		/// Infer the platform from a folder name (e.g. "win64" → Windows, "osx" → MacOS).
		/// </summary>
		public static Platform InferPlatform(string folder)
            => folder.GetPlatformFromName();
		public static Architecture InferArchitecture(string folder)
			=> folder.ToLowerInvariant() switch {
				"x86" or "win32"            => Architecture.X86,
				"x64" or "win64" or "x86_64" => Architecture.X64,
				"arm" or "armv7"             => Architecture.Arm,
				"arm64"                      => Architecture.Arm64,
				_                            => Architecture.None,
			};
	}
}
