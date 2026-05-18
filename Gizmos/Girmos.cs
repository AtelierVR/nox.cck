using UnityEditor;
using UnityEngine;
using UGizmos = UnityEngine.Gizmos;

namespace Nox.CCK.Development {
	public class Gizmos {
		// ── Capsule ──────────────────────────────────────────────────────
		public static void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius) {
			#if UNITY_EDITOR
			Vector3    upOffset    = point2 - point1;
			Vector3    up          = upOffset.Equals(default) ? Vector3.up : upOffset.normalized;
			Quaternion orientation = Quaternion.FromToRotation(Vector3.up, up);
			Vector3    forward     = orientation * Vector3.forward;
			Vector3    right       = orientation * Vector3.right;
			Handles.DrawWireArc(point2, forward, right, 180, radius);
			Handles.DrawWireArc(point1, forward, right, -180, radius);
			Handles.DrawLine(point1 + right * radius, point2 + right * radius);
			Handles.DrawLine(point1 - right * radius, point2 - right * radius);
			Handles.DrawWireArc(point2, right, forward, -180, radius);
			Handles.DrawWireArc(point1, right, forward, 180, radius);
			Handles.DrawLine(point1 + forward * radius, point2 + forward * radius);
			Handles.DrawLine(point1 - forward * radius, point2 - forward * radius);
			Handles.DrawWireDisc(point2, up, radius);
			Handles.DrawWireDisc(point1, up, radius);
			#endif
		}

		// ── Color ────────────────────────────────────────────────────────
		public static Color color {
			#if UNITY_EDITOR
			get => UGizmos.color;
			set {
				UGizmos.color = value;
				Handles.color = value;
			}
			#else
            get => Color.white;
            set {}
			#endif
		}

		// ── Lines ────────────────────────────────────────────────────────
		public static void DrawLine(Vector3 from, Vector3 to)
			#if UNITY_EDITOR
			=> UGizmos.DrawLine(from, to);
		#else
            {}
		#endif

		public static void DrawDottedLine(Vector3 from, Vector3 to, float screenSpaceSize = 4f) {
			#if UNITY_EDITOR
			Handles.DrawDottedLine(from, to, screenSpaceSize);
			#endif
		}

		public static void DrawPolyLine(Vector3[] points) {
			#if UNITY_EDITOR
			Handles.DrawPolyLine(points);
			#endif
		}

		public static void DrawBezier(Vector3 startPos, Vector3 endPos, Vector3 startTangent, Vector3 endTangent, float width = 2f) {
			#if UNITY_EDITOR
			Handles.DrawBezier(startPos, endPos, startTangent, endTangent, Handles.color, null, width);
			#endif
		}

		// ── Rays ─────────────────────────────────────────────────────────
		public static void DrawRay(Vector3 from, Vector3 direction)
			#if UNITY_EDITOR
			=> UGizmos.DrawRay(from, direction);
		#else
            {}
		#endif

		// ── Spheres ──────────────────────────────────────────────────────
		public static void DrawSphere(Vector3 center, float radius)
			#if UNITY_EDITOR
			=> UGizmos.DrawSphere(center, radius);
		#else
            {}
		#endif

		public static void DrawWireSphere(Vector3 center, float radius)
			#if UNITY_EDITOR
			=> UGizmos.DrawWireSphere(center, radius);
		#else
            {}
		#endif

		// ── Cubes ────────────────────────────────────────────────────────
		public static void DrawCube(Vector3 center, Vector3 size)
			#if UNITY_EDITOR
			=> UGizmos.DrawCube(center, size);
		#else
            {}
		#endif

		public static void DrawWireCube(Vector3 center, Vector3 size)
			#if UNITY_EDITOR
			=> UGizmos.DrawWireCube(center, size);
		#else
            {}
		#endif

		// ── Discs & Arcs ─────────────────────────────────────────────────
		public static void DrawWireDisc(Vector3 center, Vector3 normal, float radius) {
			#if UNITY_EDITOR
			Handles.DrawWireDisc(center, normal, radius);
			#endif
		}

		public static void DrawSolidDisc(Vector3 center, Vector3 normal, float radius) {
			#if UNITY_EDITOR
			Handles.DrawSolidDisc(center, normal, radius);
			#endif
		}

		public static void DrawWireArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius) {
			#if UNITY_EDITOR
			Handles.DrawWireArc(center, normal, from, angle, radius);
			#endif
		}

		public static void DrawSolidArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius) {
			#if UNITY_EDITOR
			Handles.DrawSolidArc(center, normal, from, angle, radius);
			#endif
		}

		// ── Rectangles ───────────────────────────────────────────────────
		public static void DrawSolidRectangleWithOutline(Vector3 center, Vector3 normal, Vector3 up, float width, float height, Color faceColor, Color outlineColor) {
			#if UNITY_EDITOR
			Vector3 right = Vector3.Cross(normal, up).normalized * (width * 0.5f);
			Vector3 upV   = up.normalized * (height * 0.5f);
			Vector3[] verts = {
				center - right - upV,
				center + right - upV,
				center + right + upV,
				center - right + upV
			};
			Handles.DrawSolidRectangleWithOutline(verts, faceColor, outlineColor);
			#endif
		}

		// ── Text / Labels ────────────────────────────────────────────────
		public static void DrawLabel(Vector3 position, string text) {
			#if UNITY_EDITOR
			Handles.Label(position, text);
			#endif
		}

		public static void DrawLabel(Vector3 position, string text, int fontSize, Color textColor) {
			#if UNITY_EDITOR
			var style = new GUIStyle(GUI.skin.label) {
				fontSize = fontSize,
				normal   = { textColor = textColor }
			};
			Handles.Label(position, text, style);
			#endif
		}

		// ── Arrows ───────────────────────────────────────────────────────
		public static void DrawArrow(Vector3 position, Vector3 direction, float size = 1f) {
			#if UNITY_EDITOR
			if (direction == Vector3.zero)
				return;
			Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(direction), size, EventType.Repaint);
			#endif
		}
	}
}