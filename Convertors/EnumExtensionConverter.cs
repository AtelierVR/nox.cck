using System;
using System.Reflection;
using Newtonsoft.Json;

namespace Nox.CCK.Convertors {
	/// <summary>
	/// JSON converter for enums whose wire format is defined by a companion extension class
	/// exposing a <c>static TEnum FromString(string)</c> / <c>static string ToString(TEnum)</c> pair.
	/// <para>
	/// The converter takes the extension class (<c>typeof(MyExtensions)</c>) and calls the matching
	/// method: <c>FromString</c> when reading, <c>ToString</c> when writing. This keeps the wire
	/// values decoupled from the enum member names, and calls the extension explicitly (which also
	/// bypasses the fact that <c>ToString(this TEnum)</c> is shadowed by <see cref="object.ToString"/>).
	/// </para>
	/// <example>
	/// <code>
	/// [JsonConverter(typeof(EnumExtensionConverter&lt;UserStatus&gt;), typeof(UserStatusExtensions))]
	/// public UserStatus Status { get; private set; }
	/// </code>
	/// </example>
	/// </summary>
	public class EnumExtensionConverter<TEnum> : JsonConverter<TEnum>
		where TEnum : struct, Enum {
		private readonly Func<string, TEnum> _fromString;
		private readonly Func<TEnum, string> _toString;

		/// <param name="extension">The static extension class holding the conversion methods.</param>
		public EnumExtensionConverter(Type extension) {
			if (extension == null)
				throw new ArgumentNullException(nameof(extension));

			var from = Find(extension, "FromString", typeof(TEnum), typeof(string));
			if (from == null)
				throw new ArgumentException(
					$"{extension.Name} must expose a 'static {typeof(TEnum).Name} FromString(string)' method.",
					nameof(extension)
				);

			var to = Find(extension, "ToString", typeof(string), typeof(TEnum));
			if (to == null)
				throw new ArgumentException(
					$"{extension.Name} must expose a 'static string ToString({typeof(TEnum).Name})' method.",
					nameof(extension)
				);

			_fromString = (Func<string, TEnum>)Delegate.CreateDelegate(typeof(Func<string, TEnum>), from);
			_toString   = (Func<TEnum, string>)Delegate.CreateDelegate(typeof(Func<TEnum, string>), to);
		}

		public override TEnum ReadJson(JsonReader reader, Type objectType, TEnum existingValue, bool hasExistingValue, JsonSerializer serializer)
			=> reader.TokenType switch {
				JsonToken.Null or JsonToken.Undefined => default,
				JsonToken.Integer or JsonToken.Float   => (TEnum)Enum.ToObject(typeof(TEnum), Convert.ToInt64(reader.Value)),
				JsonToken.String                       => _fromString((string)reader.Value),
				_                                      => throw new JsonSerializationException(
					$"Invalid token '{reader.TokenType}' for {typeof(TEnum).Name}."
				)
			};

		public override void WriteJson(JsonWriter writer, TEnum value, JsonSerializer serializer)
			=> writer.WriteValue(_toString(value));

		/// <summary>
		/// Finds a public static method by name, return type and exact parameter types.
		/// </summary>
		private static MethodInfo Find(Type extension, string name, Type returnType, params Type[] parameters) {
			foreach (var method in extension.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
				if (method.Name != name || method.ReturnType != returnType)
					continue;

				var args = method.GetParameters();
				if (args.Length != parameters.Length)
					continue;

				var match = true;
				for (var i = 0; i < args.Length; i++)
					if (args[i].ParameterType != parameters[i]) {
						match = false;
						break;
					}

				if (match)
					return method;
			}

			return null;
		}
	}
}
