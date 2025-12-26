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

			// Charger le UXML
			var visualTree = Resources.Load<VisualTreeAsset>("InspectorEditor");
			Root = visualTree.CloneTree();

			Root.Q<Label>("header-label").text = Title;

			Content = Root.Q<VisualElement>("content");

			// Retourner l'élément racine
			return Root;
		}
	}
}