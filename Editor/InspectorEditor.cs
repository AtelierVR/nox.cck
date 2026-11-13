using System;
using Nox.Search;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.CCK.Editor {
	public abstract class InspectorEditor<T> : UnityEditor.Editor where T : UnityEngine.Object {
		private bool _visible;

		protected T Target
			=> (T)target;

		protected virtual string Title
			=> typeof(T).Name;

		/// <summary>
		/// Icon of the component, provided by the editor: it is drawn in the header of the template and given to
		/// the object so the header of the inspector window and the hierarchy show it. A mod resource
		/// (<c>"ui:icons/custom_menu.png"</c>) or an already loaded texture — see
		/// <see cref="ResourceTextureSource"/>; the resource is resolved by <see cref="InspectorIcon"/>.
		/// </summary>
		protected virtual ResourceTextureSource Icon
			=> null;

		protected VisualElement Root;
		protected VisualElement Content;

		/// <summary>
		/// Whether the inspector is displayed right now: attached to a panel and inside the area its window (and
		/// its scroll views) actually shows. Computed on demand, so it stays accurate even when a window is
		/// scrolled without any layout change. A hidden inspector does not need to be updated â€” see
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

			// Applied here again: the editor does not depend on OnEnable having run (an editor built outside the
			// inspector window, a domain reload), and both calls are idempotent.
			ApplyObjectIcon();
			ApplyIcon();

			// Panel events are routed to overridable helpers, so a derived editor never has to register on the
			// root itself (and cannot forget to unregister).
			Root.RegisterCallback<AttachToPanelEvent>(OnAttachToPanelInternal);
			Root.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelInternal);
			Root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

			return Root;
		}

		private void OnAttachToPanelInternal(AttachToPanelEvent evt) {
			// Last chance to set the icon: an editor that never got OnEnable still passes here when it is drawn.
			ApplyObjectIcon();

			OnAttachToPanel(evt);
			UpdateVisibility();
		}

		/// <summary>
		/// Sets the icon of the component: the header of the inspector window and the hierarchy show it instead of
		/// the script icon. Done here, so an editor only declares <see cref="Icon"/>. Unity calls it when the editor
		/// is created, i.e. as soon as the object is selected — before any GUI is built.
		/// </summary>
		protected virtual void OnEnable()
			=> ApplyObjectIcon();

		/// <summary>
		/// The icon of the object itself, so the window header and the hierarchy draw it. Nothing happens for an
		/// editor built without a target (<see cref="InspectorIconRegistry"/> does that to read the icon of a
		/// type): <c>target</c> itself throws in that case, which is what the <c>try</c> is for.
		/// </summary>
		private void ApplyObjectIcon() {
			try {
				InspectorIcon.Apply(target, Icon);
			} catch (Exception) {
				// An editor with no target has no object to icon.
			}
		}

		/// <summary>
		/// Puts <see cref="Icon"/> in the header of the template. The slot is hidden when the source resolves to
		/// nothing, so an inspector without an icon keeps the label alone.
		/// </summary>
		private void ApplyIcon() {
			if (Root?.Q<VisualElement>("header-icon") is not { } element)
				return;

			if (InspectorIcon.Resolve(Icon) is { } icon) {
				element.style.backgroundImage = new StyleBackground(icon);
				element.style.display         = DisplayStyle.Flex;
			} else {
				element.style.display = DisplayStyle.None;
			}
		}

		private void OnDetachFromPanelInternal(DetachFromPanelEvent evt) {
			OnDetachFromPanel(evt);
			UpdateVisibility();
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
			=> UpdateVisibility();

		/// <summary>
		/// Notifies <see cref="OnVisible"/> / <see cref="OnInvisible"/> of the state the layout and the panel give.
		/// Called when the inspector is attached, detached, resized or hidden â€” a window scrolled without a layout
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