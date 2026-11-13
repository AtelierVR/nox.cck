using System;
using Nox.CCK.Editor;
using Nox.CCK.Language;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements {
	/// <summary>Kind of a <see cref="NotificationElement"/>: it selects the accent of the box (border and icon).</summary>
	public enum NotificationKind {
		Info,
		Success,
		Warning,
		Error
	}

	/// <summary>
	/// Notification box built in code: the icon, the message and the action buttons are created here, so a
	/// template only has to declare the element (<c>&lt;uie:NotificationElement kind="Warning" message="..."/&gt;</c>)
	/// and the box always sizes itself on its content — the message wraps and the box grows with it.
	/// <para>
	/// The element only builds the structure and sets classes: the colours, the sizes and the wrapping are
	/// the <c>.notification*</c> rules of the CCK stylesheet (<c>Resources/styles/notification.uss</c>).
	/// </para>
	/// </summary>
	[UxmlElement]
	public partial class NotificationElement : VisualElement {
		private NotificationKind _kind = NotificationKind.Info;
		private string           _message = string.Empty;

		private readonly VisualElement _icon;
		private readonly VisualElement _body;
		private readonly Label         _messageLabel;
		private readonly VisualElement _actions;

		public NotificationElement() {
			AddToClassList("notification");

			// Structure only: the sizes, the wrapping and the accent all come from the CCK stylesheet
			// (.notification / .notification-icon / .notification-body / .notification-message).
			_icon = new VisualElement { name = "icon" };
			_icon.AddToClassList("notification-icon");
			Add(_icon);

			// Column: the actions go under the message instead of squeezing it on the right.
			_body = new VisualElement { name = "body" };
			_body.AddToClassList("notification-body");

			_messageLabel = new Label { name = "message" };
			_messageLabel.AddToClassList("notification-message");
			_messageLabel.EnableInClassList("hidden", true);
			_body.Add(_messageLabel);

			_actions = new VisualElement { name = "actions" };
			_actions.AddToClassList("notification-actions");
			_actions.EnableInClassList("hidden", true);
			_body.Add(_actions);

			Add(_body);

			ApplyKind();
		}

		/// <summary>Variant of the box (see the <c>.notification.&lt;kind&gt;</c> rules of the CCK).</summary>
		[UxmlAttribute("kind")]
		public NotificationKind Kind {
			get => _kind;
			set {
				_kind = value;
				ApplyKind();
			}
		}

		/// <summary>Message shown as is (a language entry, see <see cref="LocalizedNotificationElement"/>).</summary>
		[UxmlAttribute("message")]
		public string Message {
			get => _message;
			set {
				_message           = value ?? string.Empty;
				_messageLabel.text = _message;
				_messageLabel.EnableInClassList("hidden", string.IsNullOrEmpty(_message));
			}
		}

		/// <summary>
		/// Adds a button under the message. <paramref name="label"/> is a language entry (resolved with
		/// <paramref name="args"/>, or a raw text when no pack knows it) and <paramref name="callback"/>
		/// runs when the button is clicked.
		/// </summary>
		public Button AddAction(string label, Action callback, params object[] args) {
			var button = new Button(() => callback?.Invoke()) {
				text = LanguageManager.Get(label, args)
			};
			button.AddToClassList("me-4");

			_actions.Add(button);
			_actions.EnableInClassList("hidden", false);

			return button;
		}

		/// <summary>Removes every action button; the box keeps its message only.</summary>
		public void ClearActions() {
			_actions.Clear();
			_actions.EnableInClassList("hidden", true);
		}

		private void ApplyKind() {
			foreach (var kind in Enum.GetValues(typeof(NotificationKind)))
				RemoveFromClassList(kind.ToString().ToLowerInvariant());

			AddToClassList(_kind.ToString().ToLowerInvariant());
		}
	}

	/// <summary>
	/// <see cref="NotificationElement"/> whose message is a language entry: the <c>lang-key</c> attribute is
	/// the key and <see cref="LangArgs"/> the arguments, refreshed on every language change by
	/// <see cref="LocalizedInspectorEditor{T}"/>.
	/// </summary>
	[UxmlElement]
	public partial class LocalizedNotificationElement : NotificationElement, ILocalizedElement {
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
				Message = LanguageManager.Get(_langKey, LangArgs);
		}
	}
}
