using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.CCK.Editor {
	/// <summary>
	/// <see cref="ListView"/> whose add button offers choices instead of adding a blank item: clicking « + »
	/// opens a popup listing them, and picking one runs the action it carries.
	/// <para>
	/// Built for collections whose items need something before they can be edited — a <c>[SerializeReference]</c>
	/// array, where an entry without a type is unusable. <see cref="RemoveSelected"/> and
	/// <see cref="MoveRequested"/> cover the other two edits: the list is not bound to a plain <c>IList</c>, so the
	/// owner deletes and moves the entries itself.
	/// </para>
	/// </summary>
	public class ChoiceListView : ListView {
		private readonly List<(string label, Action picked)> _choices = new();

		public ChoiceListView() {
			// Everything the ListView offers is on: the frame, the header with its title and its count, a multiple
			// selection, the dynamic height of the entries, and the animated reorder of the drag handles.
			showBorder                    = true;
			showFoldoutHeader             = true;
			showBoundCollectionSize       = true;
			selectionType                 = SelectionType.Multiple;
			virtualizationMethod          = CollectionVirtualizationMethod.DynamicHeight;
			reorderable                   = true;
			reorderMode                   = ListViewReorderMode.Animated;
			showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
			showAddRemoveFooter           = true;

			overridingAddButtonBehavior = OpenChoices;
			onRemove                    = _ => RemoveSelected?.Invoke(SelectedIndices());
			itemIndexChanged            += (from, to) => MoveRequested?.Invoke(from, to);
		}

		/// <summary>
		/// Called with the indices to delete when « − » is clicked, highest first so an owner deleting from a
		/// serialized array never shifts the indices it has left to delete.
		/// </summary>
		public Action<IEnumerable<int>> RemoveSelected { get; set; }

		/// <summary>Called with the old and new index when an entry is dragged to another place.</summary>
		public Action<int, int> MoveRequested { get; set; }

		/// <summary>Entries the « + » button offers, in the order they were added.</summary>
		public IReadOnlyList<(string label, Action picked)> Choices
			=> _choices;

		/// <summary>Adds an entry to the popup: <paramref name="picked"/> runs when it is chosen.</summary>
		public void AddChoice(string label, Action picked)
			=> _choices.Add((label, picked));

		/// <summary>Empties the popup, for a list of choices that changes with the selection.</summary>
		public void ClearChoices()
			=> _choices.Clear();

		/// <summary>The selected indices, highest first, so a deletion loop stays valid.</summary>
		private IEnumerable<int> SelectedIndices() {
			var indices = new List<int>(selectedIndices);
			indices.Sort();
			indices.Reverse();
			return indices;
		}

		private void OpenChoices(BaseListView list, Button button) {
			if (_choices.Count == 0)
				return;

			// UnityEditor.PopupWindow, not the PopupWindow element of UI Toolkit.
			UnityEditor.PopupWindow.Show(button.worldBound, new ChoicePopup(_choices));
		}

		/// <summary>Popup of the choices: they stack from the top, and picking one closes it then runs it.</summary>
		private sealed class ChoicePopup : PopupWindowContent {
			private readonly IReadOnlyList<(string label, Action picked)> _choices;

			public ChoicePopup(IReadOnlyList<(string label, Action picked)> choices)
				=> _choices = choices;

			public override Vector2 GetWindowSize()
				// The layout itself is the work of the classes (rows of `h-24`, `gap-4` between them, `p-4` around
				// them): the numbers only size the window, which has no scroll view to fall back on.
				=> new(220f, Mathf.Min(_choices.Count * 24f + Mathf.Max(0, _choices.Count - 1) * 4f + 8f, 320f));

			public override void OnOpen() {
				var root = editorWindow.rootVisualElement;

				// The popup is its own window: it does not inherit the sheet of the inspector, and the classes below
				// only exist in the CCK one.
				if (Resources.Load<StyleSheet>("nox.style") is { } style)
					root.styleSheets.Add(style);

				root.AddToClassList("p-4");

				var rows = new VisualElement();
				rows.AddToClassList("flex-col");
				rows.AddToClassList("gap-4");
				root.Add(rows);

				foreach (var (label, picked) in _choices) {
					var button = new Button(() => {
						editorWindow.Close();
						picked?.Invoke();
					}) {
						text = label
					};

					button.AddToClassList("h-24");
					button.AddToClassList("text-left");
					button.AddToClassList("m-0");
					rows.Add(button);
				}
			}

			public override void OnGUI(Rect rect) { }
		}
	}
}
