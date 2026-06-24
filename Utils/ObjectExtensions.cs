using System;
using System.Globalization;
using UnityEngine;

namespace Nox.CCK.Utils {
	/// <summary>
	/// Extension methods for objects.
	/// </summary>
	public static class ObjectExtensions {
		/// <summary>
		/// Returns a visual string representation of the object, showing its value with type-appropriate formatting.
		/// </summary>
		public static string ToVisualString(this object value) 
			=> value switch {
				null                => "null",
				string s            => $"\"{s}\"",
				bool b              => b ? "true" : "false",
				char c              => $"'{c}'",
				float f             => f.ToString(CultureInfo.InvariantCulture),
				double d            => d.ToString(CultureInfo.InvariantCulture) + "d",
				decimal m           => m.ToString(CultureInfo.InvariantCulture) + "m",
				int i               => i.ToString(CultureInfo.InvariantCulture),
				uint ui             => ui.ToString(CultureInfo.InvariantCulture) + "u",
				long l              => l.ToString(CultureInfo.InvariantCulture) + "l",
				ulong ul            => ul.ToString(CultureInfo.InvariantCulture) + "ul",
				short s2            => s2.ToString(CultureInfo.InvariantCulture) + "s",
				ushort us           => us.ToString(CultureInfo.InvariantCulture) + "us",
				byte by             => by.ToString(CultureInfo.InvariantCulture) + "b",
				sbyte sb            => sb.ToString(CultureInfo.InvariantCulture) + "sb",
                _ when value.GetType().IsArray => ToVisualStringArray(value as Array),
				_                   => $"{value} <{value.GetType().Name}>"
			};

        private static string ToVisualStringArray(Array array) {
            var len = array.Length > 10 ? 10 : array.Length;
            var truncated = array.Length - len;
            var elements = new string[len];
            for (int i = 0; i < len; i++) 
                elements[i] = array.GetValue(i).ToVisualString();
            return $"{array.GetType().Name}[{array.Length}] {{ {string.Join(", ", elements)}{(truncated > 0 ? $", ... ({truncated} more)" : "")} }}";
        }
	}
}
