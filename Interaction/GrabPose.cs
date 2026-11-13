using UnityEngine;

namespace Nox.CCK.Interaction {
	/// <summary>
	/// Poses a grabbable takes on the grab point of its grabber, shared by the objects that do not simply
	/// ride it (a mirror, a panel).
	/// <para>
	/// Everything here is written against a <see cref="IGrabber.GrabPoint"/> alone: a grab point looks away
	/// from whatever holds the object - the ray of a near/far, the view of the desktop pointer, the hand - so
	/// the opposite direction always points back at the player.
	/// </para>
	/// </summary>
	public static class GrabPose {
		/// <summary>
		/// Rotation that makes an object look back at the origin of its grab point, standing up.
		/// <para>
		/// The <b>up axis</b> of the result is the direction to the grab origin; its forward is the world up
		/// with the part lying along that axis removed, so a panel is never rolled and a mirror held above the
		/// player tips down to keep seeing them.
		/// </para>
		/// <para>
		/// Looking at the panel from straight above or below leaves no world up to project. The axes of the
		/// grab point are used then, because they come from the hand or the view and stay meaningful in every
		/// direction - which is what tells the object which way is up when the world cannot.
		/// </para>
		/// </summary>
		/// <param name="point">Grab point of the grabber holding the object.</param>
		/// <param name="fallback">Rotation used when the grab point gives no vertical at all.</param>
		/// <returns>The rotation, its up axis pointing at the grab origin.</returns>
		public static Quaternion Facing(Transform point, Quaternion fallback) {
			if (!point)
				return fallback;

			var axis = -point.forward;

			if (!TryVertical(axis, Vector3.up, out var vertical)
			    && !TryVertical(axis, point.up, out vertical)
			    && !TryVertical(axis, point.right, out vertical))
				return fallback;

			return Quaternion.LookRotation(vertical, axis);
		}

		/// <summary>Vertical of a panel whose up axis is <paramref name="axis"/>, taken from a reference direction.</summary>
		/// <param name="axis">Direction the up axis of the panel points at (towards the grab origin).</param>
		/// <param name="reference">Direction to project on the plane of the panel.</param>
		/// <param name="vertical">Normalized vertical, when the reference is not parallel to the axis.</param>
		/// <returns><c>false</c> when the reference lies along the axis: there is no vertical to take from it.</returns>
		private static bool TryVertical(Vector3 axis, Vector3 reference, out Vector3 vertical) {
			vertical = Vector3.ProjectOnPlane(reference, axis);

			if (vertical.sqrMagnitude < 0.0001f) {
				vertical = Vector3.zero;
				return false;
			}

			vertical.Normalize();
			return true;
		}

		/// <summary>
		/// Pose that lays the surface a grabber closed on into its palm.
		/// <para>
		/// The grabber is the master of its own pose: it is never moved and never turned, the object is what
		/// comes onto it. The point that was touched slides onto <see cref="IGrabber.HoldingPoint"/> and the
		/// surface is turned so its normal meets the palm (<see cref="IGrabberPalm"/>), which is what lets the
		/// fingers lie on the object instead of closing in their own plane. The turn is the shortest one, so
		/// the object keeps as much of its own orientation as the grab allows.
		/// </para>
		/// </summary>
		/// <param name="target">Object being grabbed: the turn happens around its own origin.</param>
		/// <param name="grabber">Grabber that took it.</param>
		/// <param name="position">World position the object must take.</param>
		/// <param name="rotation">World rotation the object must take.</param>
		public static void PutSurfaceInPalm(Transform target, IGrabber grabber, out Vector3 position, out Quaternion rotation) {
			position = target ? target.position : Vector3.zero;
			rotation = target ? target.rotation : Quaternion.identity;

			if (!target || grabber == null)
				return;

			var contact = grabber.Contact;
			if (!contact.IsValid)
				return;

			var turn = Quaternion.identity;

			if (grabber is IGrabberPalm palm
			    && contact.Normal.sqrMagnitude > 0.000001f
			    && palm.TryGetPalmNormal(out var palmNormal)
			    && palmNormal.sqrMagnitude > 0.000001f) {
				// The face that was touched has to end up flat on the palm, so its outward normal has to face
				// the palm: it becomes the opposite of the palm normal.
				var wanted = -palmNormal;
				var facing = Vector3.Dot(contact.Normal, wanted);

				// Nearly opposed means a half turn whose axis the cross product cannot choose: the panel would
				// land flipped every which way. The axis is taken from the palm itself, which is stable.
				turn = facing < -0.9995f
					? Quaternion.AngleAxis(180f, palmNormal)
					: Quaternion.FromToRotation(contact.Normal, wanted);
			}

			// The contact point is what lands in the palm: it is moved with the turn, so the object has to be
			// pulled back by where the turn carried it.
			var offset = contact.Point - target.position;

			rotation = turn * target.rotation;
			position = grabber.HoldingPoint - turn * offset;
		}
	}

	/// <summary>Surface of a collider a grab closed on: the point that lands in the palm and its face normal.</summary>
	public static class GrabSurface {
		/// <summary>
		/// Contact of a grab on <paramref name="collider"/>: the point of its surface the palm holds and the
		/// outward normal of the face that point lies on.
		/// <para>
		/// <paramref name="facing"/> is what the grab looks at the object with (the opposite of the palm normal,
		/// the ray of a pointer): it picks <b>which</b> face was grabbed. Without it, a hand closing on the
		/// border of a thin panel would answer with the rim - the geometrically nearest face - and the panel
		/// would be turned a quarter turn through the hand instead of lying flat on it.
		/// </para>
		/// </summary>
		/// <param name="collider">Touched collider.</param>
		/// <param name="point">Point the grab reached, in world space.</param>
		/// <param name="facing">Direction the grab looks at the object with (normalized, may be zero).</param>
		/// <param name="contact">Point of the surface that has to sit in the palm.</param>
		/// <param name="normal">Normalized outward normal of that surface.</param>
		/// <returns><c>false</c> for a shape the contact cannot be read from (a mesh).</returns>
		public static bool TryContact(Collider collider, Vector3 point, Vector3 facing, out Vector3 contact, out Vector3 normal) {
			contact = point;
			normal = Vector3.zero;

			switch (collider) {
				case null:
					return false;

				case BoxCollider box:
					return TryBoxContact(box, point, facing, out contact, out normal);

				case SphereCollider sphere: {
					var center = sphere.transform.TransformPoint(sphere.center);
					var scale = sphere.transform.lossyScale;
					var radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
					var direction = point - center;

					if (direction.sqrMagnitude < 0.000000001f)
						direction = facing.sqrMagnitude > 0.000001f ? -facing : Vector3.up;

					normal = direction.normalized;
					contact = center + normal * radius;
					return true;
				}

				case CapsuleCollider capsule: {
					if (!TryCapsule(capsule, point, out var segment, out var axis, out var radius))
						return false;

					var direction = point - segment;
					if (direction.sqrMagnitude < 0.000000001f)
						direction = facing.sqrMagnitude > 0.000001f ? -facing : -axis;

					normal = direction.normalized;
					contact = segment + normal * radius;
					return true;
				}

				default:
					return false;
			}
		}

		/// <summary>Normal of the surface of a collider at a point, without telling the grab which face to look at.</summary>
		/// <param name="collider">Touched collider.</param>
		/// <param name="point">Touched point, in world space.</param>
		/// <param name="normal">Normalized outward normal.</param>
		/// <returns><c>false</c> for a shape the normal cannot be read from.</returns>
		public static bool TryNormal(Collider collider, Vector3 point, out Vector3 normal)
			=> TryContact(collider, point, Vector3.zero, out _, out normal);

		/// <summary>
		/// Face of a box the grab closed on, with the point projected onto it: the point is clamped to the face,
		/// so a palm that reached past the border still holds the border of that face instead of a rim.
		/// </summary>
		private static bool TryBoxContact(BoxCollider box, Vector3 point, Vector3 facing, out Vector3 contact, out Vector3 normal) {
			contact = point;
			normal = Vector3.zero;

			var local = box.transform.InverseTransformPoint(point) - box.center;
			var half = box.size * 0.5f;

			var axis = -1;
			var sign = 1f;
			var best = 0f;

			if (facing.sqrMagnitude > 0.000001f) {
				for (var i = 0; i < 3; i++) {
					if (half[i] <= 0.00001f)
						continue;

					var unit = Vector3.zero;
					unit[i] = 1f;

					var towards = Vector3.Dot(box.transform.TransformDirection(unit), facing.normalized);
					if (Mathf.Abs(towards) <= Mathf.Abs(best))
						continue;

					best = towards;
					axis = i;
					sign = towards >= 0f ? 1f : -1f;
				}
			}

			// A grab that comes in along the surface says nothing about which face it is on: the nearest one is
			// used then, which is what a collision would answer.
			if (axis < 0 || Mathf.Abs(best) < 0.05f) {
				best = 0f;
				for (var i = 0; i < 3; i++) {
					if (half[i] <= 0.00001f)
						continue;

					var reach = Mathf.Abs(local[i]) / half[i];
					if (reach <= best)
						continue;

					best = reach;
					axis = i;
					sign = local[i] < 0f ? -1f : 1f;
				}
			}

			if (axis < 0)
				return false;

			var projected = local;
			for (var i = 0; i < 3; i++)
				projected[i] = i == axis ? sign * half[i] : Mathf.Clamp(local[i], -half[i], half[i]);

			var outward = Vector3.zero;
			outward[axis] = sign;

			contact = box.transform.TransformPoint(projected + box.center);
			normal = box.transform.TransformDirection(outward).normalized;
			return true;
		}

		/// <summary>Segment and radius of the core of a capsule, in world space.</summary>
		private static bool TryCapsule(CapsuleCollider capsule, Vector3 point, out Vector3 segment, out Vector3 axis, out float radius) {
			segment = point;
			radius = 0f;

			var scale = capsule.transform.lossyScale;
			var radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));

			axis = capsule.direction switch {
				1    => capsule.transform.up,
				2    => capsule.transform.forward,
				_    => capsule.transform.right
			};

			var lengthScale = Mathf.Abs(capsule.direction switch {
				1    => scale.y,
				2    => scale.z,
				_    => scale.x
			});

			radius = Mathf.Max(0f, capsule.radius * radiusScale);

			var center = capsule.transform.TransformPoint(capsule.center);
			var half = Mathf.Max(0f, capsule.height * lengthScale * 0.5f - radius);
			var along = Mathf.Clamp(Vector3.Dot(point - center, axis), -half, half);

			segment = center + axis * along;
			return radius > 0.00001f;
		}
	}
}
