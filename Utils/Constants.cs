using System;
using System.IO;

namespace Nox.CCK.Utils {
	public class Constants {
		public static Version ProtocolVersion
			=> new("0.0.9");

		public static string ProtocolIdentifier
			=> "Nox";

		private static string _appPath;
		private static string _cachePath;
		private static string _configPath;

		public static string AppPath {
			get {
				if (_appPath != null) return _appPath;
				var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
					+ (PlatformExtensions.CurrentPlatform == Platform.Windows ? "/.nox/" : "/nox/");
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
				return _appPath = dir;
			}
		}

		public static string CachePath {
			get {
				if (_cachePath != null) return _cachePath;
				var dir = Path.Combine(AppPath, "cache");
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
				return _cachePath = dir;
			}
		}

		public static string ConfigPath {
			get {
				if (_configPath != null) return _configPath;
				var dir = Path.Combine(AppPath, "config");
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
				return _configPath = dir;
			}
		}

		public static float  IntervalLantencyRequest = 2.5f;
		public static float  DefaultGravity          = -9.81f;
		public static float  DefaultJumpForce        = 2.65f;
		public static double DefaultWalkSpeed        = 2.5f;
		public static double DefaultCrouchMultiplier = 0.5f;
		public static double DefaultSprintMultiplier = 2f;
		public static float  DefaultFlySpeed         = .1f;
	}
}