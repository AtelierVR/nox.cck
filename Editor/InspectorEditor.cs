using UnityEngine;
using UnityEngine.UIElements;

namespace Nox.CCK.Editor {
	public abstract class InspectorEditor<T> : UnityEditor.Editor where T : Object {
		protected T Target
			=> (T)target;

		protected virtual string Title
			=> typeof(T).Name;

		protected VisualElement Root;
		protected VisualElement Content;


		public override VisualElement CreateInspectorGUI() {
			if (Root != null)
				return Root;

			Root = Resources
				.Load<VisualTreeAsset>("InspectorEditor")
				.CloneTree();

			Root.Q<Label>("header-label").text = Title;
			Content = Root.Q<VisualElement>("content");

			return Root;
		}
	}
}