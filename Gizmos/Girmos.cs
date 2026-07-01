using UnityEditor;
using UnityEngine;
using UGizmos = UnityEngine.Gizmos;

namespace Nox.CCK.Development {
	public static class Gizmos {
		// ── Capsule ──────────────────────────────────────────────────────
		public static void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius) {
			DrawWireCapsule(point1, point2, radius, radius);
		}

		public static void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius1, float radius2) {
			#if UNITY_EDITOR
			Vector3    upOffset    = point2 - point1;
			Vector3    up          = upOffset.Equals(default) ? Vector3.up : upOffset.normalized;
			Quaternion orientation = Quaternion.FromToRotation(Vector3.up, up);
			Vector3    forward     = orientation * Vector3.forward;
			Vector3    right       = orientation * Vector3.right;
			// Bottom hemisphere (radius1)
			Handles.DrawWireArc(point1, forward, right, -180, radius1);
			Handles.DrawWireArc(point1, right, forward, 180, radius1);
			// Top hemisphere (radius2)
			Handles.DrawWireArc(point2, forward, right, 180, radius2);
			Handles.DrawWireArc(point2, right, forward, -180, radius2);
			// Connecting lines
			Handles.DrawLine(point1 + right * radius1, point2 + right * radius2);
			Handles.DrawLine(point1 - right * radius1, point2 - right * radius2);
			Handles.DrawLine(point1 + forward * radius1, point2 + forward * radius2);
			Handles.DrawLine(point1 - forward * radius1, point2 - forward * radius2);
			#endif
		}

		// ── Color ────────────────────────────────────────────────────────
		public static Color Color {
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

		// ── Cone ─────────────────────────────────────────────────────────
		public static void DrawSolidCone(Vector3 apex, Vector3 direction, float angle, float length, int segments = 16) {
			#if UNITY_EDITOR
			if (angle <= 0f || length <= 0f) return;
			var dir = direction.normalized;
			var baseCenter = apex + dir * length;
			var radius = Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad) * length;
			// Lateral triangles
			var right = Vector3.Cross(dir, Vector3.up).normalized;
			if (right.magnitude < 0.01f) right = Vector3.Cross(dir, Vector3.forward).normalized;
			var forward = Vector3.Cross(right, dir).normalized;
			for (int i = 0; i < segments; i++) {
				float a0 = (float)i / segments * Mathf.PI * 2f;
				float a1 = (float)(i + 1) / segments * Mathf.PI * 2f;
				var p0 = baseCenter + (right * Mathf.Cos(a0) + forward * Mathf.Sin(a0)) * radius;
				var p1 = baseCenter + (right * Mathf.Cos(a1) + forward * Mathf.Sin(a1)) * radius;
				Handles.DrawAAConvexPolygon(apex, p0, p1);
			}
			#endif
		}
	}
}