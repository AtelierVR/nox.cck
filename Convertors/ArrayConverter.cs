using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace Nox.CCK.Convertors {
	/// <summary>
	/// A generic JSON converter that handles arrays by delegating each element's
	/// conversion to a specified <see cref="JsonConverter{T}"/>.
	/// </summary>
	/// <typeparam name="TConverter">
	/// The <see cref="JsonConverter{T}"/> used to convert each element of the array.
	/// </typeparam>
	/// <example>
	/// <code>
	/// [JsonConverter(typeof(ArrayConverter&lt;StringToIdentifierConverter&gt;))]
	/// public Identifier[] Identifiers { get; set; }
	/// </code>
	/// </example>
	public class ArrayConverter<TConverter> : JsonConverter
		where TConverter : JsonConverter, new() {

		private static readonly TConverter _converter = new TConverter();
		private static readonly Type _elementType;
		private static readonly Type _arrayType;

		static ArrayConverter() {
			var baseType = typeof(TConverter).BaseType;
			while (baseType != null && (!baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(JsonConverter<>)))
				baseType = baseType.BaseType;

			if (baseType == null)
				throw new InvalidOperationException(
					$"Type '{typeof(TConverter).Name}' must extend JsonConverter<T> to be used with ArrayConverter<T>.");

			_elementType = baseType.GetGenericArguments()[0];
			_arrayType   = _elementType.MakeArrayType();
		}

		/// <inheritdoc/>
		public override bool CanConvert(Type objectType)
			=> objectType == _arrayType;

		/// <summary>
		/// Writes each element of the array using <typeparamref name="TConverter"/>.
		/// </summary>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			if (value == null) {
				writer.WriteNull();
				return;
			}
			var array = (Array)value;
			writer.WriteStartArray();
			foreach (var item in array)
				_converter.WriteJson(writer, item, serializer);
			writer.WriteEndArray();
		}

		/// <summary>
		/// Reads a JSON array and converts each element using <typeparamref name="TConverter"/>.
		/// </summary>
		/// <exception cref="JsonSerializationException">Thrown when the token is not a JSON array.</exception>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			if (reader.TokenType == JsonToken.Null)
				return null;

			if (reader.TokenType != JsonToken.StartArray)
				throw new JsonSerializationException(
					$"Expected StartArray token but got '{reader.TokenType}' while deserializing {_arrayType}.");

			var items = new List<object>();
			while (reader.Read() && reader.TokenType != JsonToken.EndArray)
				items.Add(_converter.ReadJson(reader, _elementType, null, serializer));

			var result = Array.CreateInstance(_elementType, items.Count);
			for (var i = 0; i < items.Count; i++)
				result.SetValue(items[i], i);
			return result;
		}
	}
}