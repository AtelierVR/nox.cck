using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.CCK.Utils {
	public class UpdateLayout {
		/// <summary>Components to update, reused between the calls: the helper only runs on the main thread.</summary>
		private static readonly List<IUpdateLayout> _layouts = new();

		/// <summary>See <see cref="UpdateImmediate(RectTransform)"/>.</summary>
		public static void UpdateImmediate(GameObject go)
			=> UpdateImmediate(go ? go.GetComponent<RectTransform>() : null);

		/// <summary>
		/// Lays out <paramref name="rect"/> now: every node of its active hierarchy is rebuilt from its own
		/// children, bottom-up, after the <see cref="IUpdateLayout"/> components of that node ran.
		/// <para>
		/// The rebuild of each node is not redundant: a nested layout group only gets its size once its
		/// children have theirs, and the components above read that size (a widget grid positions its items
		/// from the cell size of its parent). Rebuilding only the root left every nested group at its stale
		/// size, so pages came out overlapping. The cost is paid once per page, not per frame: what ran on
		/// every tick of a slid value was the caller (see <c>SettingsComponent</c>), which now only calls
		/// this when a row of the page actually changed.
		/// </para>
		/// </summary>
		public static void UpdateImmediate(RectTransform rect) {
			if (!rect || !rect.gameObject.activeInHierarchy)
				return;

			foreach (Transform child in rect)
				if (child is RectTransform childRect)
					UpdateImmediate(childRect);

			_layouts.Clear();
			rect.GetComponents(_layouts);
			for (var i = 0; i < _layouts.Count; i++)
				_layouts[i].UpdateLayout();

			LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
		}
	}

	/// <summary>
	/// Interface for components that need to update their layout.
	/// </summary>
	public interface IUpdateLayout {
		/// <summary>
		/// Updates the component's layout.
		/// </summary>
		void UpdateLayout();
	}
}
