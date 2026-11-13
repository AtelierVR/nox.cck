using System.Collections.Generic;
using System.Reflection;
using Nox.CCK.Development;
using UnityEditor;
using UnityEngine;
using Gizmos = Nox.CCK.Development.Gizmos;
using Object = UnityEngine.Object;

namespace Nox.CCK.Editor {
	/// <summary>
	/// Makes the glyphs of the <see cref="IGizmos"/> components that declare a <see cref="GizmosAttribute"/>
	/// pickable — a click selects the object — and draws the components whose <c>OnDrawGizmos</c> is not the
	/// Unity message (an explicit interface implementation), honouring <see cref="GizmoType.Selected"/> and
	/// <see cref="GizmoType.NonSelected"/>.
	/// <para>
	/// A public <c>OnDrawGizmos()</c> stays the Unity message: the scene view draws it by itself, so it is not
	/// drawn here again.
	/// </para>
	/// </summary>
	[InitializeOnLoad]
	public static class GizmosRenderer {
		/// <summary>Radius of the picking disc drawn around each <see cref="IGizmos.GetPickPoints"/> point, in metres.</summary>
		private const float PickRadius = 0.5f;

		/// <summary>Components of the loaded scenes, and whether Unity draws them by itself.</summary>
		private static readonly List<(IGizmos gizmos, bool drawnByUnity)> Components = new();

		/// <summary>Controls registered while picking: their id, and the component they select.</summary>
		private static readonly Dictionary<int, Component> Pickable = new();

		static GizmosRenderer() {
			SceneView.duringSceneGui += OnSceneGui;
			EditorApplication.hierarchyChanged += Refresh;
			Undo.undoRedoPerformed += Refresh;
			Refresh();
		}

		/// <summary>
		/// Caches the components of the loaded scenes: a scene view event runs on every repaint, so scanning
		/// every object each time would be wasteful. The list is rebuilt on every hierarchy change.
		/// </summary>
		private static void Refresh() {
			Components.Clear();

			foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude)) {
				if (behaviour is not IGizmos gizmos)
					continue;

				// A public OnDrawGizmos — declared here or inherited — is the Unity message: the scene view
				// already draws that component. An explicit interface implementation is private, thus not
				// found here, and is drawn below.
				var message = behaviour.GetType().GetMethod(nameof(IGizmos.OnDrawGizmos), BindingFlags.Instance | BindingFlags.Public, null, System.Type.EmptyTypes, null);
				Components.Add((gizmos, message != null));
			}
		}

		private static void OnSceneGui(SceneView view) {
			var evt = Event.current;
			if (evt == null)
				return;

			var repaint = evt.type == EventType.Repaint;
			var picking = evt.type == EventType.Layout || evt.type == EventType.MouseMove || evt.type == EventType.MouseDown;

			if (!repaint && !picking)
				return;

			if (picking)
				Pickable.Clear();

			foreach (var (gizmos, drawnByUnity) in Components) {
				if (gizmos is not Component component || !component || !component.gameObject.activeInHierarchy)
					continue;

				var attribute = gizmos.GetGizmosAttribute();
				if (attribute == null)
					continue;

				// A public OnDrawGizmos is the Unity message: the scene view draws that component by itself.
				// The others draw here, when their options match the selection.
				if (repaint && !drawnByUnity) {
					var selected = Selection.Contains(component.gameObject);
					if ((attribute.DrawGizmo & (selected ? GizmoType.Selected : GizmoType.NonSelected)) != 0) {
						var color = Gizmos.Color;
						gizmos.OnDrawGizmos();
						Gizmos.Color = color;
					}
				}

				if (!picking || (attribute.DrawGizmo & GizmoType.Pickable) == 0)
					continue;

				var id = component.GetEntityId().GetHashCode() & 0x7FFFFFFF;
				Pickable[id] = component;
				HandleUtility.AddControl(id, PickDistance(gizmos, component));
			}

			// Another handle is being dragged, or the user is orbiting the view: not a click on a gizmo.
			if (evt.type != EventType.MouseDown || evt.button != 0 || evt.alt || GUIUtility.hotControl != 0)
				return;

			if (!Pickable.TryGetValue(HandleUtility.nearestControl, out var picked) || !picked)
				return;

			Selection.activeObject = picked.gameObject;
			evt.Use();
		}

		/// <summary>
		/// Distance, in pixels, from the mouse to the closest picking disc of the component: the one
		/// <see cref="HandleUtility.nearestControl"/> ranks.
		/// </summary>
		private static float PickDistance(IGizmos gizmos, Component component) {
			var points = gizmos.GetPickPoints();
			if (points == null || points.Length == 0)
				return HandleUtility.DistanceToCircle(component.transform.position, PickRadius);

			var distance = float.MaxValue;
			foreach (var point in points)
				distance = Mathf.Min(distance, HandleUtility.DistanceToCircle(point, PickRadius));

			return distance;
		}
	}
}
