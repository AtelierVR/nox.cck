using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.CCK.Editor {
	public abstract class InspectorEditor<T> : UnityEditor.Editor where T : Object {
		private bool _visible;

		protected T Target
			=> (T)target;

		protected virtual string Title
			=> typeof(T).Name;

		protected VisualElement Root;
		protected VisualElement Content;

		/// <summary>
		/// Whether the inspector is displayed right now: attached to a panel and inside the area its window (and
		/// its scroll views) actually shows. Computed on demand, so it stays accurate even when a window is
		/// scrolled without any layout change. A hidden inspector does not need to be updated — see
		/// <see cref="OnVisible"/> and <see cref="OnInvisible"/>.
		/// </summary>
		protected bool IsVisible
			=> IsDisplayed();


		public override VisualElement CreateInspectorGUI() {
			if (Root != null)
				return Root;

			Root = Resources
				.Load<VisualTreeAsset>("InspectorEditor")
				.CloneTree();

			Root.Q<Label>("header-label").text = Title;
			Content = Root.Q<VisualElement>("content");

			// Panel events are routed to overridable helpers, so a derived editor never has to register on the
			// root itself (and cannot forget to unregister).
			Root.RegisterCallback<AttachToPanelEvent>(OnAttachToPanelInternal);
			Root.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelInternal);
			Root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

			return Root;
		}

		private void OnAttachToPanelInternal(AttachToPanelEvent evt) {
			OnAttachToPanel(evt);
			UpdateVisibility();
		}

		private void OnDetachFromPanelInternal(DetachFromPanelEvent evt) {
			OnDetachFromPanel(evt);
			UpdateVisibility();
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
			=> UpdateVisibility();

		/// <summary>
		/// Notifies <see cref="OnVisible"/> / <see cref="OnInvisible"/> of the state the layout and the panel give.
		/// Called when the inspector is attached, detached, resized or hidden — a window scrolled without a layout
		/// change raises nothing, the check is on demand through <see cref="IsVisible"/>.
		/// </summary>
		private void UpdateVisibility() {
			var visible = IsDisplayed();
			if (visible == _visible)
				return;

			_visible = visible;
			if (_visible)
				OnVisible();
			else
				OnInvisible();
		}

		/// <summary>
		/// Whether the inspector has a non empty area inside the panel: an element which is hidden, collapsed or
		/// scrolled out of its window is not displayed.
		/// </summary>
		private bool IsDisplayed() {
			if (Root == null || Root.panel == null)
				return false;

			if (!IsShown(Root))
				return false;

			var area = Root.worldBound;
			for (var parent = Root.parent; parent != null; parent = parent.parent) {
				if (!IsShown(parent))
					return false;

				// Scrolling a window moves the inspector without changing its own layout.
				if (parent is ScrollView scrollView)
					area = Intersect(area, scrollView.contentViewport.worldBound);
			}

			return area.width > 0f && area.height > 0f;
		}

		private static bool IsShown(VisualElement element)
			=> element.resolvedStyle.display != DisplayStyle.None
			&& element.resolvedStyle.visibility != Visibility.Hidden;

		private static Rect Intersect(Rect a, Rect b) {
			var min = new Vector2(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin));
			var max = new Vector2(Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));
			return new Rect(min, new Vector2(Mathf.Max(0f, max.x - min.x), Mathf.Max(0f, max.y - min.y)));
		}

		/// <summary>
		/// Called when the inspector starts being displayed (attached to a panel with a non empty area, see
		/// <see cref="IsVisible"/>): the place to refresh what was left alone while it was hidden.
		/// </summary>
		protected virtual void OnVisible() { }

		/// <summary>
		/// Called when the inspector stops being displayed (collapsed, shrunk to nothing, detached from its
		/// panel): whatever <see cref="OnVisible"/> started can be released here. A window scrolled away without
		/// a layout change raises nothing, <see cref="IsVisible"/> still reports it.
		/// </summary>
		protected virtual void OnInvisible() { }

		/// <summary>
		/// Called when the inspector is added to a panel: the root is displayed, so this is where a derived
		/// editor starts listening to what it renders.
		/// </summary>
		protected virtual void OnAttachToPanel(AttachToPanelEvent evt) { }

		/// <summary>
		/// Called when the inspector is removed from its panel: whatever <see cref="OnAttachToPanel"/> registered
		/// must be released here.
		/// </summary>
		protected virtual void OnDetachFromPanel(DetachFromPanelEvent evt) { }
	}
}