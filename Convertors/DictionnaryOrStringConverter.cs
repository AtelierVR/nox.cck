using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Nox.CCK.Convertors
{
    public abstract class DictionnaryOrStringConverter<T> : JsonConverter<T> where T : Dictionary<string, string>
    {
        public const string DefaultKey = "default";

        private readonly string _defaultKey;
        protected readonly StringComparer _comparer;

        protected DictionnaryOrStringConverter(string defaultKey = null, StringComparer comparer = null)
        {
            _defaultKey = defaultKey;
            _comparer = comparer ?? StringComparer.Ordinal;
        }

        protected DictionnaryOrStringConverter(string defaultKey, bool ignoreCase)
        {
            _defaultKey = defaultKey;
            _comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        }

        protected virtual string GetDefaultKey()
            => _defaultKey ?? DefaultKey;

        protected abstract T CreateEmpty();
        protected abstract T CreateEmpty(StringComparer comparer);

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            var count = value.Count;
            switch (count)
            {
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

        public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                    var result = CreateEmpty(_comparer);
                    result.Add(GetDefaultKey(), (string)reader.Value);
                    return result;
                case JsonToken.StartObject:
                    return serializer.Deserialize<T>(reader);
                case JsonToken.Null:
                case JsonToken.None:
                    return CreateEmpty(_comparer);
                default:
                    throw new JsonSerializationException("Invalid token type for " + typeof(T).Name);
            }
        }
    }

    public class DictionnaryOrStringConverter : DictionnaryOrStringConverter<DictionnaryOrString>
    {
        public DictionnaryOrStringConverter(string defaultKey = null, StringComparer comparer = null)
            : base(defaultKey, comparer) { }

        public DictionnaryOrStringConverter(bool ignoreCase)
            : base(null, ignoreCase) { }

        public DictionnaryOrStringConverter(string defaultKey, bool ignoreCase)
            : base(defaultKey, ignoreCase) { }

        protected override DictionnaryOrString CreateEmpty()
            => new DictionnaryOrString();

        protected override DictionnaryOrString CreateEmpty(StringComparer comparer)
            => new DictionnaryOrString(comparer);
    }

    [Serializable]
    public class DictionnaryOrString : Dictionary<string, string>
    {
        public DictionnaryOrString() : base() { }
        public DictionnaryOrString(StringComparer comparer) : base(comparer) { }
    }
}