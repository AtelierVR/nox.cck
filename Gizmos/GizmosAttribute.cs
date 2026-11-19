using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nox.CCK.Development {
	/// <summary>
	/// Declares how a component draws its scene view gizmos: the tags a tool filters it with, and when the
	/// gizmos are drawn.
	/// </summary>
	public class GizmosAttribute : System.Attribute {
#if UNITY_EDITOR
        /// <summary>Drawn whenever the scene view draws, and pickable: a click selects the component.</summary>
        public const GizmoType DefaultDrawGizmo = GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable;
#endif

		/// <summary>Free-form tags, used to select the components a tool draws (e.g. « cck.worlds.spawns »).</summary>
		public string[] Tags { get; private set; }

		/// <summary>
		/// When the gizmos of the component are drawn — the same flags a <c>[DrawGizmo]</c> handler takes.
		/// </summary>
#if UNITY_EDITOR
		public GizmoType DrawGizmo { get; private set; }
#endif

		/// <summary>Declares tags only: the gizmos are drawn with the default flags.</summary>
		public GizmosAttribute(params string[] tags) {
			Tags = Unique(tags);

#if UNITY_EDITOR
			DrawGizmo = DefaultDrawGizmo;
#endif
		}

		/// <summary>Declares when to draw the gizmos, then the tags.</summary>
#if UNITY_EDITOR
		public GizmosAttribute(GizmoType drawGizmo, params string[] tags) {
			DrawGizmo = drawGizmo;
			Tags      = Unique(tags);
		}
#endif

		/// <summary>Drops the empty and duplicated tags, in declaration order.</summary>
		private static string[] Unique(string[] tags) {
			List<string> unique = new();

			foreach (var tag in tags)
				if (!string.IsNullOrEmpty(tag) && !unique.Contains(tag))
					unique.Add(tag);

			return unique.ToArray();
		}
	}
}
