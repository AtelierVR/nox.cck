using System;
using Newtonsoft.Json;
using Nox.CCK.Utils;

namespace Nox.CCK.Convertors {
	/// <summary>
	/// A custom JSON converter that converts between a string and an Identifier object.
	/// </summary>
	public class StringToIdentifierConverter : JsonConverter<Identifier> {

		/// <summary>
		/// Writes the JSON representation of the Identifier object as a string.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		public override void WriteJson(JsonWriter writer, Identifier value, JsonSerializer serializer)
			=> writer.WriteValue(value.ToString());

		/// <summary>
		/// Reads the JSON representation of a string and converts it back to an Identifier object.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="objectType"></param>
		/// <param name="existingValue"></param>
		/// <param name="hasExistingValue"></param>
		/// <param name="serializer"></param>
		/// <returns></returns>
		/// <exception cref="JsonSerializationException"></exception>
		public override Identifier ReadJson(JsonReader reader, Type objectType, Identifier existingValue, bool hasExistingValue, JsonSerializer serializer)
			=> reader.TokenType switch {
				JsonToken.String => Identifier.Parse((string)reader.Value!),
				JsonToken.Null   => Identifier.Invalid,
				JsonToken.None   => Identifier.Invalid,
				_                => throw new JsonSerializationException("Invalid token type for Identifier")
			};
	}
}