using System.Collections.Generic;
using UnityEngine;

namespace Nox.CCK.Interaction {
	/// <summary>Side a grabber works with, when it has one.</summary>
	public enum Handedness {
		/// <summary>Grabber without a side (a pointer, a script, a ray).</summary>
		None,

		Left,

		Right
	}

	/// <summary>
	/// Something that can hold a <see cref="IGrabbable"/>: an XR hand, a desktop pointer, an NPC, a script.
	/// <para>
	/// The contract is deliberately minimal: the grabbable only needs to know where it is attached, so a
	/// platform never has to expose its own hand type to the objects it carries.
	/// </para>
	/// </summary>
	public interface IGrabber {
		/// <summary>Pose the held object is attached to while grabbed.</summary>
		Transform GrabPoint { get; }

		/// <summary>
		/// Side of the grabber, so a grabbable can answer a grip authored per hand (see
		/// <see cref="IGrabAnchors"/>). <see cref="Handedness.None"/> when it has no side.
		/// </summary>
		Handedness Handedness { get; }

		/// <summary>
		/// Surface the grabber closed on when it took the object, or an invalid contact when it grabbed without
		/// touching anything (a script, a pointer aimed at the object).
		/// <para>
		/// A grab happens where the hand touched: it is what lets a hand pose itself on the surface and what
		/// pins the object's grab point on the point the player actually chose, instead of its centre.
		/// </para>
		/// </summary>
		GrabContact Contact { get; }

			/// <summary>
			/// Point the carried object is held against: the palm of a hand, the grab point of a pointer.
			/// <para>
			/// It is where <see cref="Contact"/> is put when the object is grabbed, so the object comes onto the
			/// grabber (its touched point in the palm) and the grabber itself never moves.
			/// </para>
			/// </summary>
			Vector3 HoldingPoint { get; }
	}

	/// <summary>
	/// Grabber that knows its own body: a held object ignores those colliders, so it can be carried into the
	/// player (a hand brought to the chest, a prop pulled in front of the view) without colliding with the
	/// capsule, the hands or the head - or pushing them.
	/// <para>
	/// Optional: a grabber that does not implement it keeps the plain behaviour, the host of the grab (a player
	/// body) then ignores the collisions itself.
	/// </para>
	/// </summary>
	public interface IGrabberBody {
		/// <summary>Colliders of the grabber's own body, or an empty list.</summary>
		IReadOnlyList<Collider> BodyColliders { get; }
	}

	/// <summary>
	/// Grabber that grabs with a side of its own - the palm of a hand - so a grabbed object can be laid onto it.
	/// <para>
	/// The grabber stays the master of its pose: it never moves and never turns. It is the grabbed object that
	/// comes onto the palm (see <see cref="GrabPose.PutSurfaceInPalm"/>), the point it was touched at landing on
	/// <see cref="IGrabber.HoldingPoint"/> and the surface that was touched turned to lie flat against the palm.
	/// </para>
	/// <para>
	/// Optional: a grabber without a side (a ray, a cursor, a script) keeps the pose the object already had.
	/// </para>
	/// </summary>
	public interface IGrabberPalm {
		/// <summary>
		/// Normal of the palm, in world space, pointing out of the hand - the side that touches what it grabs.
		/// </summary>
		/// <param name="normal">Normalized palm normal, when it can be read.</param>
		/// <returns><c>false</c> when the palm is not available (no avatar hand, no fingers).</returns>
		bool TryGetPalmNormal(out Vector3 normal);
	}

	/// <summary>
	/// Grabbable whose pose can be refreshed on demand. Used by the grabbers that only know where their grab point
	/// really is during the render pass (a view camera placed on the eyes of an avatar): they move the grab point
	/// and then ask the object to follow it, instead of letting it ride a pose that is already outdated.
	/// </summary>
	public interface IGrabFollower {
		/// <summary>Applies the pose of the grab point right now, if the object is held.</summary>
		void FollowGrabPose();
	}

	/// <summary>
	/// Grabber whose grab point is only final when the frame is rendered. A grabbable held by it waits for
	/// <see cref="IGrabFollower.FollowGrabPose"/> instead of following the grab point in <c>LateUpdate</c>: the
	/// object is then posed once per frame, so nothing ever sees it half-way between two poses.
	/// </summary>
	public interface IGrabberRenderPose {
		/// <summary>Whether the grab point is final only once the frame is rendered.</summary>
		bool PosesAtRender { get; }
	}
	/// Only things that can be held implement this: an object pinned in the world or laid out by a
	/// viewport simply does not, and there is no mode to read. Menus, portals, mirrors... answer this
	/// contract once in the platform that knows about them (a nox.xr menu is held by a <c>Hand</c> part,
	/// a nox.desktop panel by its pointer), and the features that are platform agnostic
	/// (<c>nox.worlds</c>, <c>nox.sessions</c>, a network entity) only have to depend on it.
	/// </para>
	/// <para>
	/// <b>The grabbable moves itself</b>: while <see cref="HeldCount"/> is not 0 it follows
	/// <see cref="IGrabber.GrabPoint"/> from its own <c>LateUpdate</c> (execution order 1000, after
	/// everything that moves the grabber, so the object never lags a frame behind the hand). A grabber only
	/// announces the grab through <see cref="OnGrab"/> - it never re-parents the object.
	/// </para>
	/// </summary>
	public interface IGrabbable {
		/// <summary>Transform the grabber attaches to: the object itself, not its wrapper.</summary>
		Transform Target { get; }

		/// <summary>Physical body carried by the object, or <c>null</c> when it follows its grabber only.</summary>
		Rigidbody Body { get; }

		/// <summary>Number of grabbers currently holding this object (0 when free).</summary>
		int HeldCount { get; }

		/// <summary>Colliders of the object, so a grabber can ignore them while holding it.</summary>
		IReadOnlyList<Collider> GrabColliders { get; }

		/// <summary>Whether this grabber is allowed to take the object right now.</summary>
		bool CanGrab(IGrabber grabber);

		/// <summary>Called right after a grabber took the object.</summary>
		void OnGrab(IGrabber grabber);

		/// <summary>Called right before a grabber lets the object go.</summary>
		void OnRelease(IGrabber grabber);

		/// <summary>Drops the object from every grabber that holds it (a menu closing, a world rule).</summary>
		void ReleaseAll();
	}
}
