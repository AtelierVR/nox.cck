using System;

namespace Nox.CCK.Convertors {
	public class TranslatedStringConverter : DictionnaryOrStringConverter<TranslatedString> {
		public TranslatedStringConverter() : base(null, StringComparer.OrdinalIgnoreCase) { }

		protected override string DefaultKey
			=> Language.LanguageManager.FallbackLanguage;

		protected override TranslatedString CreateEmpty() 
			=> new();

		protected override TranslatedString CreateEmpty(StringComparer comparer) 
			=> new();
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