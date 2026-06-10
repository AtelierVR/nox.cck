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
		/// Returns the prioritized list of plugin subfolder names for the given platform
		/// and architecture, ordered from most specific to least.
		/// Example for Windows x64: ["win64", "x86_64", "x64"]
		/// </summary>
		public static string[] GetSubFolders(Platform platform, Architecture arch) {
			var folders = new List<string>();

			// 1. Platform-specific folder (e.g. "win64", "linux", "osx")
			if (arch != Architecture.None) {
				var archSuffix = arch switch {
					Architecture.X86 => "32",
					Architecture.X64 => "64",
					_                => "",
				};
				var platformFolder = platform switch {
					Platform.Windows  => "win" + archSuffix,
					Platform.Linux    => "linux",
					Platform.MacOS    => "osx",
					Platform.Android  => "android",
					Platform.IOS      => "ios",
					Platform.VisionOS => "visionos",
					_                 => null,
				};
				if (!string.IsNullOrEmpty(platformFolder))
					folders.Add(platformFolder);
			}

			// 2. Architecture-only aliases (e.g. "x86_64", "x64")
			if (arch == Architecture.X64)
				folders.Add("x86_64");
			var archName = arch.GetArchitectureName();
			if (!string.IsNullOrEmpty(archName))
				folders.Add(archName);

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
