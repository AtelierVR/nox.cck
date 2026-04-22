using System;
using UnityEngine;

namespace Nox.CCK.Utils
{
    public enum Engine : byte
    {
        None = 0,
        Unity = 1,
        Unreal = 2,
        Godot = 3,
        Source = 4
    }

    public static class EngineExtensions
    {
        public static string GetEngineName(this Engine engine) => engine switch
        {
            Engine.Unity => "unity",
            Engine.Unreal => "unreal",
            Engine.Godot => "godot",
            Engine.Source => "source",
            _ => null,
        };

        public static Engine GetEngineFromName(string name) => name switch
        {
            "unity" => Engine.Unity,
            "unreal" => Engine.Unreal,
            "godot" => Engine.Godot,
            "source" => Engine.Source,
            _ => Engine.None,
        };

        public static Engine CurrentEngine 
            => Engine.Unity;
        
        public static Version CurrentVersion
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                foreach (var c in Application.unityVersion)
                {
                    if (char.IsDigit(c) || c == '.') sb.Append(c);
                    else break;
                }
                return Version.TryParse(sb.ToString().TrimEnd('.'), out var v) ? v : new Version(0, 0, 0);
            }
        }
    }
}