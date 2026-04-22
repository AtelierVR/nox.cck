using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Nox.CCK.Convertors {
	/// <summary>
	/// A custom JSON converter that converts between a string and an Identifier object.
	/// </summary>
	public class TranslatedStringConverter : JsonConverter<TranslatedString> {

		/// <summary>
		/// Writes the JSON representation of the Identifier object as a string.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		public override void WriteJson(JsonWriter writer, TranslatedString value, JsonSerializer serializer) {
			var count = value.Count;
			switch (count) {
				case 0:
					writer.WriteNull();
					break;
				case 1:
					// print as string
					writer.WriteValue(value.FirstOrDefault().Value);
					break;
				default:
					// print as object
					writer.WriteValue(value);
					break;
			}
		}

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
		public override TranslatedString ReadJson(JsonReader reader, Type objectType, TranslatedString existingValue, bool hasExistingValue, JsonSerializer serializer)
			=> reader.TokenType switch {
				JsonToken.String      => new TranslatedString { { Language.LanguageManager.FallbackLanguage, (string)reader.Value } },
				JsonToken.StartObject => serializer.Deserialize<TranslatedString>(reader),
				JsonToken.Null        => new TranslatedString(),
				JsonToken.None        => new TranslatedString(),
				_                     => throw new JsonSerializationException("Invalid token type for Identifier")
			};
	}

	/// <summary>
	/// A class representing a translated string, 
	/// which is a dictionary that maps language codes 
	/// to their corresponding translations.
	/// </summary>
	[Serializable]
	public class TranslatedString : Dictionary<string, string> {
		public TranslatedString() : base(StringComparer.OrdinalIgnoreCase) { }
	}
}