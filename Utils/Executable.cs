using System;
using System.IO;
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
using System.Runtime.InteropServices;
#endif

namespace Nox.CCK.Utils {
	/// <summary>
	/// Helpers for managing native executables downloaded at runtime
	/// (setting Unix execute permissions without spawning external processes).
	/// </summary>
	public static class Executable {
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
		// mode bits for chmod +x (0755): owner rwx, group r-x, other r-x
		private const int Mode0755 = 0x1C0 /* S_IRWXU */ | 0x28 /* S_IRGRP|S_IXGRP */ | 0x5 /* S_IROTH|S_IXOTH */;

		/// <summary>libc chmod.</summary>
		[DllImport("libc", EntryPoint = "chmod", SetLastError = true, CharSet = CharSet.Ansi)]
		private static extern int chmod(string path, int mode);
#endif

		/// <summary>
		/// Sets execute permission (equivalent to <c>chmod +x</c>) on Unix platforms.
		/// No-op on Windows. Does nothing if the file does not exist.
		/// </summary>
		public static void MakeExecutable(string path) {
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
				return;

#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
			if (PlatformExtensions.RuntimePlatform != Platform.Linux)
				return;

			try {
				if (chmod(path, Mode0755) != 0)
					Logger.LogWarning($"chmod failed for {path} (errno {Marshal.GetLastWin32Error()})");
			} catch (Exception e) {
				// best effort — ignore unsupported platforms
				Logger.LogWarning($"Failed to set execute permission on {path}: {e.Message}");
			}
#endif
		}
	}
}
