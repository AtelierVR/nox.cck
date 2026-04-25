using System;
using Newtonsoft.Json;

namespace Nox.CCK.Convertors {
	public class TranslatedStringConverter : DictionnaryOrStringConverter<TranslatedString> {
		public TranslatedStringConverter() : base(null, StringComparer.OrdinalIgnoreCase) { }

		protected override string GetDefaultKey() => Language.LanguageManager.FallbackLanguage;

		protected override TranslatedString CreateEmpty() => new TranslatedString();
		protected override TranslatedString CreateEmpty(StringComparer comparer) => new TranslatedString();
	}

	/// <summary>
	/// A class representing a translated string, 
	/// which is a dictionary that maps language codes 
	/// to their corresponding translations.
	/// </summary>
	[Serializable]
	public class TranslatedString : DictionnaryOrString {
		public TranslatedString() : base(StringComparer.OrdinalIgnoreCase) { }
	}
}