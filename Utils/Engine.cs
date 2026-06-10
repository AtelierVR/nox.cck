using System;
using UnityEngine;

namespace Nox.CCK.Utils
{
    public readonly struct Engine : IEquatable<Engine>
    {
        public string Name    { get; }
        public string Display { get; }

        private Engine(string name, string display) {
            Name    = name;
            Display = display ?? name;
        }

        public static Engine None   => new("none",   "None");
        public static Engine Unity  => new("unity",  "Unity");
        public static Engine Unreal => new("unreal", "Unreal");
        public static Engine Godot  => new("godot",  "Godot");
        public static Engine Source => new("source", "Source");

        public static Engine From(string name, string display = null)
            => new(name, display);

        public bool Equals(Engine other)
            => string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj)
            => obj is Engine other && Equals(other);

        public override int GetHashCode()
            => Name?.ToLowerInvariant().GetHashCode() ?? 0;

        public static bool operator ==(Engine a, Engine b) => a.Equals(b);
        public static bool operator !=(Engine a, Engine b) => !a.Equals(b);
    }

    public static class EngineExtensions
    {
        public static string GetEngineName(this Engine engine)
            => engine.Name;

        public static Engine GetEngineFromName(this string name) {
            if (string.IsNullOrEmpty(name)) return Engine.None;
            if (name.Equals(Engine.Unity.Name, StringComparison.OrdinalIgnoreCase))  return Engine.Unity;
            if (name.Equals(Engine.Unreal.Name, StringComparison.OrdinalIgnoreCase)) return Engine.Unreal;
            if (name.Equals(Engine.Godot.Name, StringComparison.OrdinalIgnoreCase))  return Engine.Godot;
            if (name.Equals(Engine.Source.Name, StringComparison.OrdinalIgnoreCase))  return Engine.Source;
            return Engine.From(name);
        }

        public static Engine CurrentEngine
            => Engine.Unity;

        public static Version CurrentVersion
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                foreach (var c in Application.unityVersion) {
                    if (char.IsDigit(c) || c == '.') sb.Append(c);
                    else break;
                }
                return Version.TryParse(sb.ToString().TrimEnd('.'), out var v) ? v : new Version(0, 0, 0);
            }
        }

        public static bool IsCompatible(string name, string version = null)
            => CurrentEngine.GetEngineName() == name
            && (version == null || CurrentVersion.ToString() == version);
    }
}