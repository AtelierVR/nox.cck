#if UNITY_EDITOR
using UnityEditor;
#endif
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
			DrawWireArc(point1, forward, right, -180, radius1);
			DrawWireArc(point1, right, forward, 180, radius1);
			// Top hemisphere (radius2)
			DrawWireArc(point2, forward, right, 180, radius2);
			DrawWireArc(point2, right, forward, -180, radius2);
			// Connecting lines
			DrawLine(point1 + right * radius1, point2 + right * radius2);
			DrawLine(point1 - right * radius1, point2 - right * radius2);
			DrawLine(point1 + forward * radius1, point2 + forward * radius2);
			DrawLine(point1 - forward * radius1, point2 - forward * radius2);
			#endif
		}

		// ── Color ────────────────────────────────────────────────────────
		public static Color Color {
			#if UNITY_EDITOR
			get => Handles.color;
			set {
				// Kept in sync for the code that still reads UnityEngine.Gizmos.color in a gizmo pass.
				UGizmos.color = value;
				Handles.color = value;
			}
			#else
            get => Color.white;
            set {}
			#endif
		}

		// ── Lines ────────────────────────────────────────────────────────
		// Drawn through Handles rather than UnityEngine.Gizmos: Gizmos only draws from a gizmo pass, while
		// Handles draws from a gizmo pass *and* from a SceneView callback (see GizmosRenderer).
		public static void DrawLine(Vector3 from, Vector3 to) {
			#if UNITY_EDITOR
			Handles.DrawLine(from, to);
			#endif
		}

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

		// ── Rays ───────────────────────────────────────────────────────
		public static void DrawRay(Vector3 from, Vector3 direction) {
			#if UNITY_EDITOR
			Handles.DrawLine(from, from + direction);
			#endif
		}

		// ── Spheres ──────────────────────────────────────────────────────
		public static void DrawSphere(Vector3 center, float radius) {
			#if UNITY_EDITOR
			// Handles has no solid sphere: the cap is what the scene view uses for its own handles.
			Handles.SphereHandleCap(0, center, Quaternion.identity, radius * 2f, EventType.Repaint);
			#endif
		}

		public static void DrawWireSphere(Vector3 center, float radius) {
			#if UNITY_EDITOR
			// Handles has no wire sphere: three great circles.
			Handles.DrawWireDisc(center, Vector3.up, radius);
			Handles.DrawWireDisc(center, Vector3.right, radius);
			Handles.DrawWireDisc(center, Vector3.forward, radius);
			#endif
		}

		// ── Cubes ──────────────────────────────────────────────────────
		public static void DrawCube(Vector3 center, Vector3 size) {
			#if UNITY_EDITOR
			Handles.CubeHandleCap(0, center, Quaternion.identity, Mathf.Max(size.x, size.y, size.z), EventType.Repaint);
			#endif
		}

		public static void DrawWireCube(Vector3 center, Vector3 size) {
			#if UNITY_EDITOR
			Handles.DrawWireCube(center, size);
			#endif
		}

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

		/// <summary>
		/// Arrow drawn as geometry — a shaft and a cone head — whose tip is at the end of the arrow, in the
		/// current gizmo colour. Unlike <see cref="DrawArrow(Vector3, Vector3, float)"/>, which draws a handle
		/// cap, it takes its lengths in metres and points reliably towards
		/// <paramref name="direction"/>.
		/// </summary>
		/// <param name="from">Tail of the arrow.</param>
		/// <param name="direction">Direction the arrow points to.</param>
		/// <param name="length">Total length of the arrow, head included.</param>
		/// <param name="headLength">Length of the cone head, clamped to <paramref name="length"/>.</param>
		/// <param name="headAngle">Full opening angle of the cone head, in degrees.</param>
		public static void DrawArrow(Vector3 from, Vector3 direction, float length, float headLength, float headAngle = 60f) {
			#if UNITY_EDITOR
			if (direction.sqrMagnitude < 1e-8f || length <= 0f)
				return;

			var dir  = direction.normalized;
			var tip  = from + dir * length;
			var head = Mathf.Clamp(headLength, 0f, length);

			// The shaft stops where the head starts, so the two do not overlap.
			if (length > head)
				DrawRay(from, dir * (length - head));

			// DrawSolidCone takes the apex, i.e. the tip: the head is built from the tip backwards.
			if (head > 0f)
				DrawSolidCone(tip, -dir, headAngle, head);
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

		// ── Ranges ───────────────────────────────────────────────────────
		/// <summary>Radial range: a wire sphere of the given radius.</summary>
		public static void DrawRange(Vector3 center, float radius) {
			#if UNITY_EDITOR
			if (radius > 0f)
				DrawWireSphere(center, radius);
			#endif
		}

		/// <summary>Angular range around <paramref name="direction"/>: bounding lines, cap disc and apex.</summary>
		public static void DrawAngleRange(Vector3 position, Vector3 direction, float angle, float radius) {
			#if UNITY_EDITOR
			if (direction.sqrMagnitude < 1e-8f || angle <= 0f || radius <= 0f)
				return;

			var dir   = direction.normalized;
			var right = Vector3.Cross(dir, Vector3.up);
			if (right.sqrMagnitude < 1e-8f)
				right = Vector3.Cross(dir, Vector3.forward);
			right.Normalize();

			float half      = angle * 0.5f;
			var   capCentre = position + dir * (Mathf.Cos(half * Mathf.Deg2Rad) * radius);
			var   capRadius = Mathf.Sin(half * Mathf.Deg2Rad) * radius;

			DrawWireDisc(capCentre, dir, capRadius);
			DrawLine(position, position + Quaternion.AngleAxis( half, right) * dir * radius);
			DrawLine(position, position + Quaternion.AngleAxis(-half, right) * dir * radius);
			#endif
		}

		/// <summary>
		/// Side indicator: a circle on the plane plus a solid cone towards
		/// <paramref name="normal"/>.
		/// </summary>
		public static void DrawSideRange(Vector3 position, Vector3 normal, float radius) {
			#if UNITY_EDITOR
			if (radius <= 0f)
				return;
			var n = normal.sqrMagnitude < 1e-8f ? Vector3.up : normal.normalized;
			DrawWireDisc(position, n, radius);
			DrawSolidCone(position, n, 90f, radius);
			#endif
		}
	}
}
