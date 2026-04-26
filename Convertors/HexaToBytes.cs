using System;
using Newtonsoft.Json;

namespace Nox.CCK.Convertors {
	/// <summary>
	/// A JSON converter that converts byte arrays to and from hexadecimal strings.
	/// Is used for hash in the SDK, which are stored as byte arrays but need to be serialized as strings in JSON.
	/// </summary>
	public class HexaToBytes : JsonConverter<byte[]> {

		/// <summary>
		/// Writes a byte array as a hexadecimal string to the JSON writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		public override void WriteJson(JsonWriter writer, byte[] value, JsonSerializer serializer)
			=> writer.WriteValue(BitConverter.ToString(value).Replace("-", ""));

		/// <summary>
		/// Reads a hexadecimal string from the JSON reader and converts it back to a byte array.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="objectType"></param>
		/// <param name="existingValue"></param>
		/// <param name="hasExistingValue"></param>
		/// <param name="serializer"></param>
		/// <returns></returns>
		public override byte[] ReadJson(JsonReader reader, Type objectType, byte[] existingValue, bool hasExistingValue, JsonSerializer serializer) {
			string hex = (string)reader.Value!;
			int length = hex.Length;
			byte[] bytes = new byte[length / 2];
			for (int i = 0; i < length; i += 2) 
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			return bytes;
		}
	}
}