using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Nox.CCK.Convertors {
	/// <summary>Serializes a <see cref="Vector3"/> as <c>[x, y, z]</c> (object form is still accepted).</summary>
	public sealed class Vector3JsonConverter : JsonConverter<Vector3> {
		public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer) {
			writer.WriteStartArray();
			writer.WriteValue(value.x);
			writer.WriteValue(value.y);
			writer.WriteValue(value.z);
			writer.WriteEndArray();
		}

		public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer) {
			var token = JToken.Load(reader);

			if (token is JArray array && array.Count >= 3)
				return new Vector3(array[0].Value<float>(), array[1].Value<float>(), array[2].Value<float>());

			if (token.Type == JTokenType.Object)
				return new Vector3(token.Value<float>("x"), token.Value<float>("y"), token.Value<float>("z"));

			return existingValue;
		}
	}
}
