using UnityEngine;

namespace Nox.CCK.Language {
	public class TextLanguage : MonoBehaviour {
		public string   key;
		public string[] arguments;

		private string text
			=> LanguageManager.Get(key, arguments);

		private TMPro.TMP_Text      _tmp;
		private UnityEngine.UI.Text _text;

		private void Awake()
			=> CacheTarget();

		private void Start() {
			LanguageManager.OnLanguageChanged.AddListener(OnUpdateText);
			UpdateText();
		}

		private void OnDestroy()
			=> LanguageManager.OnLanguageChanged.RemoveListener(OnUpdateText);


		private void OnValidate() {
			CacheTarget();
			UpdateText();
		}

		private void OnUpdateText(string lang)
			=> UpdateText();

		/// <summary>
		/// Resolves the target text component once. <c>GetComponent</c> on an absent type builds an
		/// error message in the Editor (a string allocation on every call), and this runs on the hot
		/// path of every animated text.
		/// </summary>
		private void CacheTarget() {
			_tmp   = GetComponent<TMPro.TextMeshProUGUI>();
			_text  = _tmp == null ? GetComponent<UnityEngine.UI.Text>() : null;
		}

		public void UpdateText() {
			if (_tmp == null && _text == null)
				CacheTarget();

			if (_tmp != null)
				_tmp.text = text;
			else if (_text != null)
				_text.text = text;
		}

		public void UpdateText(string[] args) {
			arguments = args;
			UpdateText();
		}

		public void UpdateText(string k, string[] args = null) {
			key = k;
			if (args != null)
				arguments = args;
			UpdateText();
		}
	}
}
