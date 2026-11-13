using System;
using UnityEngine;

namespace Nox.CCK.Interaction {
	/// <summary>
	/// An object a grabber can climb on, shared by every platform: what a hand has to find on the prop to
	/// pull the player up with it.
	/// </summary>
	public interface IClimbable {
		/// <summary>Whether climbing it is allowed right now.</summary>
		bool Enabled { get; }

		/// <summary>Per-axis climbing strength multiplier.</summary>
		Vector3 Axis { get; }

		/// <summary>Reset the grab offset once no grabber climbs on it anymore.</summary>
		void ResetGrabOffset();
	}

	/// <summary>An object a grabber can push, shared by every platform.</summary>
	public interface IPushable {
		/// <summary>Whether pushing it is allowed right now.</summary>
		bool Enabled { get; }

		/// <summary>Per-axis multiplier applied to the push force.</summary>
		Vector3 StrengthScale { get; }

		/// <summary>Transform carrying the push (the body of the object).</summary>
		Transform Target { get; }
	}

	/// <summary>
	/// A grabbable whose grip is authored: the grabber is placed on the anchor of its side instead of
	/// keeping the pose it took the object with.
	/// <para>
	/// It is what tells a prop how to sit in a hand (a tool held by its handle, a panel taken from its
	/// border): <see cref="GrabMode.Snap"/> reads it through <see cref="GrabbableObject"/>, and a platform
	/// can read it too to pose a hand on the object instead of posing the object in the hand.
	/// </para>
	/// </summary>
	public interface IGrabAnchors {
		/// <summary>Anchor of this grabber, or <c>null</c> when the object has none for its side.</summary>
		Transform GetGrabAnchor(IGrabber grabber);
	}

	/// <summary>Curl of the five fingers of a hand, 0 = open, 1 = closed (thumb, index, middle, ring, pinky).</summary>
	[Serializable]
	public struct GrabCurls {
		public float Thumb;
		public float Index;
		public float Middle;
		public float Ring;
		public float Pinky;

		/// <summary>Curl of a finger, 0 = thumb ... 4 = pinky.</summary>
		/// <param name="finger">Index of the finger.</param>
		public readonly float this[int finger]
			=> finger switch {
				0 => Thumb,
				1 => Index,
				2 => Middle,
				3 => Ring,
				4 => Pinky,
				_ => 0f
			};
	}

	/// <summary>
	/// Where a grabber closed its hand on an object: the point of its surface and the normal there.
	/// <para>
	/// It is what AutoHand calls the grab hit: the grab happens <b>where the hand touched</b>, not at the
	/// centre of the object, so a hand can pose itself on the surface (see <c>IGrabber.Contact</c>).
	/// </para>
	/// </summary>
	[Serializable]
	public struct GrabContact {
		/// <summary>Collider of the object the grabber touched, or <c>null</c> when it did not touch a surface.</summary>
		public Collider Collider;

		/// <summary>Point of the surface the grabber touched, in world space.</summary>
		public Vector3 Point;

		/// <summary>Normal of the surface at <see cref="Point"/>, pointing away from the object.</summary>
		public Vector3 Normal;

		/// <summary>Whether the grabber actually touched a surface.</summary>
		public bool IsValid
			=> Collider != null;
	}

	/// <summary>
	/// A grabbable that poses the <b>hand</b> instead of riding the hand: the fingers take the shape of the
	/// grip while the object is held, the way AutoHand poses a hand on its <c>GrabbablePose</c>.
	/// <para>
	/// A platform reads it on the held object every frame; an object that does not implement it leaves the
	/// fingers to the tracking.
	/// </para>
	/// </summary>
	public interface IGrabPose {
		/// <summary>Grip the fingers take while this object is held.</summary>
		/// <param name="grabber">Grabber holding the object.</param>
		/// <param name="curls">Curl of each finger.</param>
		/// <returns><c>false</c> when the object has no grip for that grabber.</returns>
		bool TryGetGrabCurls(IGrabber grabber, out GrabCurls curls);
	}
}
