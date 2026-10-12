using Nox.CCK.Editor;
using Nox.CCK.Language;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements {
	/// <summary>
	/// <see cref="Label"/> whose text is the language entry named by its <c>lang-key</c> attribute, formatted
	/// with its <c>userData</c> as arguments (see <see cref="ILocalizedElement"/>).
	/// <para>
	/// Only <c>lang-key</c> is exposed to the UXML: the arguments are assigned from code, and the text is
	/// refreshed on every language change by <see cref="Nox.CCK.Editor.LocalizedInspectorEditor{T}"/>.
	/// </para>
	/// </summary>
	[UxmlElement]
	public partial class LocalizedLabel : Label, ILocalizedElement {
		private string _langKey;

		/// <summary>Key of the language entry, as the <c>lang-key</c> attribute of the UXML.</summary>
		[UxmlAttribute("lang-key")]
		public string LangKey {
			get => _langKey;
			set {
				_langKey = value;
				RefreshText();
			}
		}

		/// <summary>Arguments formatted into the entry, kept in the element's <c>userData</c>.</summary>
		public object[] LangArgs {
			get => userData as object[];
			set {
				userData = value;
				RefreshText();
			}
		}

		public void RefreshText() {
			if (!string.IsNullOrEmpty(_langKey))
				text = LanguageManager.Get(_langKey, LangArgs);
		}
	}

	/// <summary>
	/// <see cref="Button"/> whose text is the language entry named by its <c>lang-key</c> attribute (see
	/// <see cref="LocalizedLabel"/>).
	/// </summary>
	[UxmlElement]
	public partial class LocalizedButton : Button, ILocalizedElement {
		private string _langKey;

		/// <summary>Key of the language entry, as the <c>lang-key</c> attribute of the UXML.</summary>
		[UxmlAttribute("lang-key")]
		public string LangKey {
			get => _langKey;
			set {
				_langKey = value;
				RefreshText();
			}
		}

		/// <summary>Arguments formatted into the entry, kept in the element's <c>userData</c>.</summary>
		public object[] LangArgs {
			get => userData as object[];
			set {
				userData = value;
				RefreshText();
			}
		}

		public void RefreshText() {
			if (!string.IsNullOrEmpty(_langKey))
				text = LanguageManager.Get(_langKey, LangArgs);
		}
	}

	/// <summary>
	/// <see cref="Foldout"/> whose title is the language entry named by its <c>lang-key</c> attribute (see
	/// <see cref="LocalizedLabel"/>).
	/// </summary>
	[UxmlElement]
	public partial class LocalizedFoldout : Foldout, ILocalizedElement {
		private string _langKey;

		/// <summary>Key of the language entry, as the <c>lang-key</c> attribute of the UXML.</summary>
		[UxmlAttribute("lang-key")]
		public string LangKey {
			get => _langKey;
			set {
				_langKey = value;
				RefreshText();
			}
		}

		/// <summary>Arguments formatted into the entry, kept in the element's <c>userData</c>.</summary>
		public object[] LangArgs {
			get => userData as object[];
			set {
				userData = value;
				RefreshText();
			}
		}

		public void RefreshText() {
			if (!string.IsNullOrEmpty(_langKey))
				text = LanguageManager.Get(_langKey, LangArgs);
		}
	}
}
