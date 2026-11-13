using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Nox.CCK.Convertors {
	/// <summary>Serializes a <see cref="Quaternion"/> as <c>[x, y, z, w]</c> (object form is still accepted).</summary>
	public sealed class QuaternionJsonConverter : JsonConverter<Quaternion> {
		public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer) {
			writer.WriteStartArray();
			writer.WriteValue(value.x);
			writer.WriteValue(value.y);
			writer.WriteValue(value.z);
			writer.WriteValue(value.w);
			writer.WriteEndArray();
		}

		public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer) {
			var token = JToken.Load(reader);

			if (token is JArray array && array.Count >= 4)
				return new Quaternion(
					array[0].Value<float>(),
					array[1].Value<float>(),
					array[2].Value<float>(),
					array[3].Value<float>()
				);

			if (token.Type == JTokenType.Object)
				return new Quaternion(
					token.Value<float>("x"),
					token.Value<float>("y"),
					token.Value<float>("z"),
					token.Value<float>("w")
				);

			return existingValue;
		}
	}
}
