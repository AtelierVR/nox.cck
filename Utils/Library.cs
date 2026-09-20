using System.Collections.Generic;

namespace Nox.CCK.Utils {
	/// <summary>
	/// Centralized library path conventions for platform, architecture, folder names and file extensions.
	/// </summary>
	public static class Library {
		/// <summary>
		/// Returns the library file extension for the given platform (e.g. ".dll", ".so", ".dylib").
		/// </summary>
		public static string GetExtension(Platform platform)
			=> platform switch {
				Platform.Windows => ".dll",
				Platform.Linux   => ".so",
				Platform.MacOS   => ".dylib",
				_                => null,
			};

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
		/// Infer the platform from a folder name (e.g. "win64" → Windows, "osx" → MacOS).
		/// </summary>
		public static Platform InferPlatform(string folder)
			=> folder.ToLowerInvariant() switch {
				"linux"                            => Platform.Linux,
				"osx" or "macos" or "mac"          => Platform.MacOS,
				"win" or "win32" or "win64" or "windows" => Platform.Windows,
				"android"                          => Platform.Android,
				"ios"                              => Platform.IOS,
				"visionos" or "xros"               => Platform.VisionOS,
				_                                  => Platform.None,
			};

		/// <summary>
		/// Infer the architecture from a folder name (e.g. "win64" → X64, "win32" → X86).
		/// </summary>
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
