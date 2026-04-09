using System;
using Newtonsoft.Json;

namespace Nox.CCK.Convertors {
	/// <summary>
	/// A JSON converter that converts byte arrays to and from Base64 strings.
	/// Is used for public keys in the SDK, which are stored as byte arrays but need to be serialized as strings in JSON.
	/// </summary>
	public class Base64ToBytes : JsonConverter<byte[]> {

		/// <summary>
		/// Writes a byte array as a Base64 string to the JSON writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		public override void WriteJson(JsonWriter writer, byte[] value, JsonSerializer serializer)
			=> writer.WriteValue(Convert.ToBase64String(value));

		/// <summary>
		/// Reads a Base64 string from the JSON reader and converts it back to a byte array.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="objectType"></param>
		/// <param name="existingValue"></param>
		/// <param name="hasExistingValue"></param>
		/// <param name="serializer"></param>
		/// <returns></returns>
		public override byte[] ReadJson(JsonReader reader, Type objectType, byte[] existingValue, bool hasExistingValue, JsonSerializer serializer)
			=> Convert.FromBase64String((string)reader.Value!);
	}
}