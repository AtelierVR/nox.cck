using System;
using System.Runtime.InteropServices;
using UnityEngine;
using URuntimePlatform = UnityEngine.RuntimePlatform;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nox.CCK.Utils
{
    public readonly struct Platform : IEquatable<Platform>
    {
        public string Name    { get; }
        public string Display { get; }

        private Platform(string name, string display) {
            Name    = name;
            Display = display ?? name;
        }

        public static Platform None     
            => new("none",     "None");

        public static Platform Windows  
            => new("windows",  "Windows");

        public static Platform Linux    
            => new("linux",    "Linux");

        public static Platform MacOS    
            => new("macos",    "macOS");

        public static Platform Android  
            => new("android",  "Android");

        public static Platform IOS      
            => new("ios",      "iOS");

        public static Platform VisionOS 
            => new("visionos", "visionOS");

        public static Platform From(string name, string display = null)
            => new(name, display);

        /// <summary>
        /// Clé de comparaison et de persistance. <c>default(Platform)</c> (aucun nom : valeur d'un
        /// asset non encore migré) est volontairement assimilé à <see cref="None"/> afin de conserver
        /// la sémantique de l'ancien <c>enum Platform</c> où <c>None == 0</c>. Jamais <c>null</c>.
        /// </summary>
        public string Key
            => string.IsNullOrEmpty(Name) ? None.Name : Name;

        public bool Equals(Platform other)
            => string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj)
            => obj is Platform other && Equals(other);

        public override int GetHashCode()
            => Key.ToLowerInvariant().GetHashCode();

        public override string ToString()
            => Display;

        public static bool operator ==(Platform a, Platform b) => a.Equals(b);
        public static bool operator !=(Platform a, Platform b) => !a.Equals(b);
    }

    public static class PlatformExtensions
    {
        public static Platform[] All
            => new[]
            {
                Platform.None, Platform.Windows, Platform.Linux, Platform.MacOS,
                Platform.Android, Platform.IOS, Platform.VisionOS,
            };

        public static string GetPlatformName(this Platform platform)
            => platform.Name;

        /// <summary>
        /// Convertit en <see cref="Platform"/> une chaîne : un nom, un alias (<c>windows</c>,
        /// <c>win64</c>, <c>osx</c>, <c>mac</c>, <c>xros</c>…), un libellé affiché
        /// (<see cref="Platform.Display"/>) ou, pour les scènes/prefabs antérieurs au passage de
        /// l'<c>enum Platform</c>, son ancien identifiant (<c>"0"</c> = None … <c>"6"</c> = VisionOS,
        /// dans l'ordre de <see cref="All"/>). Toute valeur inconnue donne <see cref="Platform.None"/> :
        /// les plateformes forment un ensemble fermé, contrairement aux moteurs (<see cref="Engine.From"/>).
        /// </summary>
        public static Platform GetPlatformFromName(this string target) {
            if (string.IsNullOrEmpty(target)) return Platform.None;

            foreach (var platform in All) // noms canoniques et libellés affichés
                if (platform.Name.Equals(target, StringComparison.OrdinalIgnoreCase)
                    || platform.Display.Equals(target, StringComparison.OrdinalIgnoreCase))
                    return platform;

            return target.ToLowerInvariant() switch { // alias
                "win" or "win32" or "win64" => Platform.Windows,
                "osx" or "mac" => Platform.MacOS,
                "xros" => Platform.VisionOS,
                _ => Platform.None,
            };
        }

        public static Platform CurrentPlatform
        {
            get
#if UNITY_EDITOR
                => CurrentTarget.GetPlatform();
#else
				=> RuntimePlatform;
#endif
#if UNITY_EDITOR
            set
            {
                var target = value.GetBuildTarget();
                if (IsSupported(target))
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(target), target);
                    Logger.Log($"Switched Unity build target to {target}");
                }
                else
                {
                    Logger.LogError($"Build target {target} is not supported");
                }
            }
#endif
        }

        public static Platform GetPlatform(this OSPlatform target)
        {
            if (OSPlatform.Windows == target)
                return Platform.Windows;
            if (OSPlatform.Linux == target)
                return Platform.Linux;
            if (OSPlatform.OSX == target)
                return Platform.MacOS;
            return Platform.None;
        }

        public static OSPlatform GetOSPlatform(this Platform platform) {
            if (platform == Platform.Windows) return OSPlatform.Windows;
            if (platform == Platform.Linux)   return OSPlatform.Linux;
            if (platform == Platform.MacOS)   return OSPlatform.OSX;
            return default;
        }

        public static Platform GetPlatform(this URuntimePlatform target)
            => target switch
            {
                URuntimePlatform.OSXEditor => Platform.MacOS,
                URuntimePlatform.OSXPlayer => Platform.MacOS,
                URuntimePlatform.WindowsEditor => Platform.Windows,
                URuntimePlatform.WindowsPlayer => Platform.Windows,
                URuntimePlatform.LinuxEditor => Platform.Linux,
                URuntimePlatform.LinuxPlayer => Platform.Linux,
                URuntimePlatform.LinuxServer => Platform.Linux,
                URuntimePlatform.LinuxHeadlessSimulation => Platform.Linux,
                // URuntimePlatform.EmbeddedLinuxArm32      => Platform.Linux,
                URuntimePlatform.EmbeddedLinuxArm64 => Platform.Linux,
                URuntimePlatform.EmbeddedLinuxX64 => Platform.Linux,
                URuntimePlatform.IPhonePlayer => Platform.IOS,
                URuntimePlatform.Android => Platform.Android,
                URuntimePlatform.VisionOS => Platform.VisionOS,
                _ => Platform.None
            };

        public static Platform RuntimePlatform
            => Application.platform.GetPlatform();

#if UNITY_EDITOR
        public static Platform GetPlatform(this BuildTarget target)
            => target switch
            {
                BuildTarget.StandaloneWindows64 => Platform.Windows,
                BuildTarget.StandaloneLinux64 => Platform.Linux,
                BuildTarget.StandaloneOSX => Platform.MacOS,
                BuildTarget.Android => Platform.Android,
                BuildTarget.iOS => Platform.IOS,
                BuildTarget.VisionOS => Platform.VisionOS,
                _ => Platform.None,
            };

        public static BuildTarget GetBuildTarget(this Platform platform) {
            if (platform == Platform.Windows)  return BuildTarget.StandaloneWindows64;
            if (platform == Platform.Linux)    return BuildTarget.StandaloneLinux64;
            if (platform == Platform.MacOS)    return BuildTarget.StandaloneOSX;
            if (platform == Platform.Android)  return BuildTarget.Android;
            if (platform == Platform.IOS)      return BuildTarget.iOS;
            if (platform == Platform.VisionOS) return BuildTarget.VisionOS;
            return BuildTarget.NoTarget;
        }

        private static bool IsSupported(this BuildTarget target)
            => BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target);

        private static BuildTarget CurrentTarget
            => EditorUserBuildSettings.activeBuildTarget;

        public static bool IsSupported(this Platform platform)
            => IsSupported(GetBuildTarget(platform));
#endif
        public static bool IsCompatible(string constraint)
            => constraint == CurrentPlatform.GetPlatformName();
    }
}