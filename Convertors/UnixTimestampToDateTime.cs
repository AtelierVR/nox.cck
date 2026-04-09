using System;
using Newtonsoft.Json;
namespace Nox.CCK.Convertors {
	/// <summary>
	/// Converts a Unix timestamp (in milliseconds) to a DateTime object and vice versa.
	/// </summary>
	public class UnixTimestampToDateTime : JsonConverter<DateTime> {

		/// <summary>
		/// Converts a DateTime object to a Unix timestamp (in milliseconds) and writes it to the JSON writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
			=> writer.WriteValue(new DateTimeOffset(value).ToUnixTimeMilliseconds());

		/// <summary>
		/// Converts a Unix timestamp (in milliseconds) from the JSON reader to a DateTime object.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="objectType"></param>
		/// <param name="existingValue"></param>
		/// <param name="hasExistingValue"></param>
		/// <param name="serializer"></param>
		/// <returns></returns>
		/// <exception cref="JsonSerializationException"></exception>
		public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
			=> reader.TokenType switch {
				JsonToken.Integer => DateTimeOffset.FromUnixTimeMilliseconds((long)reader.Value!).UtcDateTime,
				JsonToken.Float   => DateTimeOffset.FromUnixTimeMilliseconds((long)(double)reader.Value!).UtcDateTime,
				_                 => throw new JsonSerializationException("Invalid token type for DateTime")
			};
	}
}