using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nox.CCK.Interaction {
	/// <summary>
	/// Default <see cref="IGrabbable"/>: a prop a platform can take, carry and drop, without knowing which
	/// platform did it.
	/// <para>
	/// The object moves itself: while <see cref="HeldCount"/> is not 0 it follows the grab point of its
	/// first grabber from <c>LateUpdate</c> (execution order <see cref="FollowOrder"/>), so it never lags
	/// the hand by a frame and never has to be re-parented. A grabber only calls
	/// <see cref="OnGrab"/> / <see cref="OnRelease"/>.
	/// </para>
	/// <para>
	/// <see cref="GrabMode"/> decides how it follows the grab point: see <see cref="GrabMode"/> for the
	/// behaviours (free, fixed grip for tools, translation only, rotation only). A second hand can take
	/// the object while the first one holds it: the first grabber keeps driving the pose, and the object
	/// is only released once the last one lets go.
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/Interaction/Grabbable Object")]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(FollowOrder)]
	public class GrabbableObject : MonoBehaviour, IGrabbable, IGrabAtDistance, IGrabAnchors, IGrabFollower {
		/// <summary>
		/// Execution order of the follow step: <b>after</b> the tracked parts (XRFollow runs at 1000) and
		/// after the interactors that move a grab point, so the object is drawn with the pose of the hand or
		/// view of the current frame instead of the previous one.
		/// </summary>
		public const int FollowOrder = 1100;

		[Header("Grab")]
		[SerializeField, Tooltip("How the object follows the grab point while it is held.")]
		private GrabMode mode = GrabMode.Free;

		[SerializeField, Tooltip("Local pose marking where a hand holds the object. Required by Snap: the grip lands on the grab point with this rotation.")]
		private Transform grabAnchor;

		[SerializeField, Tooltip("Grips authored per hand: the anchor used instead of the shared one when that side takes the object.")]
		private SideAnchor[] sideAnchors = Array.Empty<SideAnchor>();

		[SerializeField, Min(0f), Tooltip("Longest distance a grabber can take the object from, in metres. 0 lets the grabber decide.")]
		private float maxRange;

		/// <summary>Grip of one side of the grabbers.</summary>
		[Serializable]
		public struct SideAnchor {
			[Tooltip("Side this anchor is for. None = any grabber without a side.")]
			public Handedness Side;

			[Tooltip("Transform the grip lands on for that side.")]
			public Transform Anchor;
		}

		[Header("Body")]
		[SerializeField, Tooltip("Make the rigidbody kinematic while the object is held, so the grab point drives it without fighting the physics.")]
		private bool kinematicWhileHeld = true;

		[SerializeField, Tooltip("Restore the gravity the body had when the object is released (only when kinematicWhileHeld is on).")]
		private bool restoreGravity = true;

		[SerializeField, Tooltip("Ignore the collisions with the grabber's own body (capsule, hands, head) while held, so the object can be brought into the player without colliding with it or pushing it.")]
		private bool ignoreGrabberBody = true;

		[Header("Release")]
		[SerializeField, Tooltip("Throw the object with the velocity of the grab point when it is released. The grab point carries the motion of the hand or of the view, so it also carries the locomotion of the player.")]
		private bool throwOnRelease = true;

		[SerializeField, Min(0f), Tooltip("Seconds of motion the throw is measured over. A flick released a moment after the hand stopped still counts, instead of being lost by a single-frame measurement.")]
		private float throwWindow = 0.05f;

		[SerializeField, Min(0f), Tooltip("Multiplier applied to the grab point velocity when thrown.")]
		private float throwScale = 1f;

		/// <summary>Grabbers currently holding the object, in grab order. The first one drives the pose.</summary>
		private readonly List<IGrabber> _holders = new List<IGrabber>();

		/// <summary>Colliders of the object, resolved once and on demand.</summary>
		private Collider[] _colliders;

		/// <summary>Pairs ignored for the current grab, restored when the last grabber lets go.</summary>
		private readonly List<(Collider Object, Collider Body)> _ignoredBody = new();

		private Rigidbody _body;
		private bool _bodyResolved;

		/// <summary>Pose of the object in the grab point, captured when the first grabber took it.</summary>
		private Vector3 _localPosition;
		private Quaternion _localRotation = Quaternion.identity;

		/// <summary>World pose captured at grab time, used by the fixed modes.</summary>
		private Vector3 _lockedPosition;
		private Quaternion _lockedRotation = Quaternion.identity;

		/// <summary>Local pose of <see cref="grabAnchor"/> in the object, captured when Snap starts.</summary>
		private Vector3 _anchorLocalPosition;
		private Quaternion _anchorLocalRotation = Quaternion.identity;

		/// <summary>Body state to restore once the last grabber lets go.</summary>
		private bool _wasKinematic = true;
		private bool _wasGravity;

		/// <summary>Last grab point position, to measure the throw velocity.</summary>
		private Vector3 _lastPointPosition;

		/// <summary>Recent positions of the grab point, kept to measure a throw over a short window.</summary>
		private readonly List<(float Time, Vector3 Position)> _motion = new();
		private bool _hasPointPosition;

		/// <summary>Velocity kept from the last release (measured on the grab point).</summary>
		private Vector3 _releaseVelocity;

		/// <summary>A grabber just took the object.</summary>
		public event Action<IGrabber> Grabbed;

		/// <summary>A grabber just let the object go.</summary>
		public event Action<IGrabber> Released;

		/// <summary>How the object follows its grabber.</summary>
		public GrabMode Mode {
			get => mode;
			set => mode = value;
		}

		/// <summary>
		/// A grabbable keeps its distance only when it says so (a menu panel, a mirror): a plain prop is
		/// brought in front of the player by a pointer instead.
		/// </summary>
		public virtual bool KeepsGrabDistance
			=> false;

		/// <summary>Local pose marking the grip. Used by <see cref="GrabMode.Snap"/>.</summary>
		public Transform GrabAnchor {
			get => grabAnchor;
			set => grabAnchor = value;
		}

		/// <summary>Anchor of a grabber: the one authored for its side, or the shared one.</summary>
		public Transform GetGrabAnchor(IGrabber grabber) {
			var side = grabber != null
				? grabber.Handedness
				: Handedness.None;

			for (var i = 0; i < sideAnchors.Length; i++) {
				var entry = sideAnchors[i];
				if (entry.Side == side && entry.Anchor)
					return entry.Anchor;
			}

			return grabAnchor;
		}

		/// <summary>Transform the grabber attaches to: the object itself.</summary>
		public Transform Target
			=> transform;

		/// <summary>Physical body carried by the object, or <c>null</c> when it follows its grabber only.</summary>
		public Rigidbody Body {
			get {
				if (!_bodyResolved) {
					_body = GetComponent<Rigidbody>();
					_bodyResolved = true;
				}

				return _body;
			}
		}

		/// <summary>Number of grabbers currently holding the object (0 when free).</summary>
		public int HeldCount
			=> _holders.Count;

		/// <summary>Whether any grabber holds the object.</summary>
		public bool IsHeld
			=> _holders.Count > 0;

		/// <summary>Grabber driving the pose, or <c>null</c> when the object is free.</summary>
		public IGrabber Holder
			=> _holders.Count > 0 ? _holders[0] : null;

		/// <summary>Colliders of the object, so a grabber can ignore them while holding it.</summary>
		public IReadOnlyList<Collider> GrabColliders {
			get {
				if (_colliders == null)
					_colliders = GetComponentsInChildren<Collider>(true);

				return _colliders ?? Array.Empty<Collider>();
			}
		}

		/// <summary>Whether this grabber is allowed to take the object right now.</summary>
		public bool CanGrab(IGrabber grabber)
			=> isActiveAndEnabled
			   && grabber != null
			   && grabber.GrabPoint
			   && !_holders.Contains(grabber)
			   && InRange(grabber);

		/// <summary>Whether the grabber is close enough for <see cref="maxRange"/> (0 accepts any distance).</summary>
		private bool InRange(IGrabber grabber) {
			if (maxRange <= 0f)
				return true;

			var source = grabber as Component;
			var origin = source ? source.transform.position : grabber.GrabPoint.position;

			return (origin - transform.position).sqrMagnitude <= maxRange * maxRange;
		}

		/// <summary>Called right after a grabber took the object.</summary>
		public void OnGrab(IGrabber grabber) {
			if (!CanGrab(grabber))
				return;

			if (_holders.Count == 0) {
				CapturePose(grabber);
				StoreBody();
				IgnoreGrabberBody(grabber);
			}

			_holders.Add(grabber);
			_hasPointPosition = false;
			Grabbed?.Invoke(grabber);
		}

		/// <summary>Called right before a grabber lets the object go.</summary>
		public void OnRelease(IGrabber grabber) {
			if (grabber == null || !_holders.Remove(grabber))
				return;

			// Still held by another hand: the first grabber keeps driving the pose, and its offsets stay.
			if (_holders.Count > 0)
				return;

			// The grabber may release from a late update that runs before this follow step: the pose of
			// this frame is taken as the last sample so the throw measures the motion up to the release.
			var point = grabber.GrabPoint;
			if (point)
				RecordMotion(point.position);

			MeasureThrowVelocity(grabber);
			RestoreBody();
			RestoreGrabberBody();
			_hasPointPosition = false;

			if (throwOnRelease && Body && !Body.isKinematic)
				Body.linearVelocity = _releaseVelocity;

			Released?.Invoke(grabber);
		}

		/// <summary>Release the object from every grabber.</summary>
		public void ReleaseAll() {
			for (var i = _holders.Count - 1; i >= 0; i--)
				OnRelease(_holders[i]);
		}

		/// <summary>
		/// Captures the pose of the object in the grab point (free / fixed modes) or of the anchor
		/// (snap). Called once, when the first grabber takes the object.
		/// </summary>
		private void CapturePose(IGrabber grabber) {
			var point = grabber.GrabPoint;

			_lockedPosition = transform.position;
			_lockedRotation = transform.rotation;

			// The grabber is the master of its own pose, so a free grip is the object that comes onto it: the
			// touched point in its palm, the touched face turned onto it. The other modes keep the pose the
			// object already had (a snap grip is authored, a fixed axis is deliberate).
			var position = _lockedPosition;
			var rotation = _lockedRotation;

			if (mode == GrabMode.Free)
				GrabPose.PutSurfaceInPalm(transform, grabber, out position, out rotation);

			// Capture the whole pose even for the fixed modes: the values they do not use are cheap and
			// switching the mode at runtime must not need another grab.
			_localPosition = point.InverseTransformPoint(position);
			_localRotation = Quaternion.Inverse(point.rotation) * rotation;

			var anchor = GetGrabAnchor(grabber);
			if (!anchor)
				anchor = transform;

			_anchorLocalPosition = transform.InverseTransformPoint(anchor.position);
			_anchorLocalRotation = Quaternion.Inverse(transform.rotation) * anchor.rotation;
		}

		/// <summary>
		/// Applies the pose demanded by the grab point of the first grabber.
		/// <para>
		/// Overridden by the grabbables that do not simply ride the grab point (a mirror that stays where
		/// it is and only turns towards the grabber): they replace the pose, the rest of the bookkeeping
		/// (holders, body, throw) stays here.
		/// </para>
		/// </summary>
		protected virtual void FollowGrabPoint() {
			var point = Holder != null ? Holder.GrabPoint : null;
			if (!point) {
				// The grab point went away (a hand was destroyed): the object must not stay attached to
				// a dead transform.
				ReleaseAll();
				return;
			}

			Vector3 position;
			Quaternion rotation;

			switch (mode) {
				case GrabMode.Snap:
					rotation = point.rotation * Quaternion.Inverse(_anchorLocalRotation);
					position = point.position - rotation * Vector3.Scale(_anchorLocalPosition, transform.lossyScale);
					break;

				case GrabMode.FixedRotation:
					position = point.TransformPoint(_localPosition);
					rotation = _lockedRotation;
					break;

				case GrabMode.FixedPosition:
					position = _lockedPosition;
					rotation = point.rotation * _localRotation;
					break;

				default:
					position = point.TransformPoint(_localPosition);
					rotation = point.rotation * _localRotation;
					break;
			}

			transform.SetPositionAndRotation(position, rotation);

			_lastPointPosition = point.position;
			RecordMotion(point.position);
			_hasPointPosition = true;
		}

		/// <summary>Remembers where the grab point was, pruned to a few throw windows.</summary>
		private void RecordMotion(Vector3 position) {
			var now = Time.unscaledTime;

			_motion.Add((now, position));

			var oldest = now - Mathf.Max(throwWindow, 0.05f) * 4f;

			while (_motion.Count > 2 && _motion[0].Time < oldest)
				_motion.RemoveAt(0);
		}

		/// <summary>Remembers the body state and freezes it, so the grab point can drive the object.</summary>
		private void StoreBody() {
			var body = Body;
			if (!body)
				return;

			_wasKinematic = body.isKinematic;
			_wasGravity = body.useGravity;

			if (!kinematicWhileHeld)
				return;

			if (!body.isKinematic) {
				body.linearVelocity = Vector3.zero;
				body.angularVelocity = Vector3.zero;
			}

			body.isKinematic = true;
			body.useGravity = false;
		}

		/// <summary>Gives the body back to the physics engine with the state it had when grabbed.</summary>
		private void RestoreBody() {
			var body = Body;
			if (!body || !kinematicWhileHeld)
				return;

			body.isKinematic = _wasKinematic;

			if (restoreGravity)
				body.useGravity = _wasGravity;
		}

		/// <summary>
		/// Cuts the collisions between the object and the body of the grabber, so carrying it into the
		/// player does not hit - or push - its own capsule, hands or head.
		/// </summary>
		private void IgnoreGrabberBody(IGrabber grabber) {
			RestoreGrabberBody();

			if (!ignoreGrabberBody || grabber is not IGrabberBody body)
				return;

			var others = body.BodyColliders;
			var own = GrabColliders;

			if (others == null || others.Count == 0)
				return;

			for (var i = 0; i < own.Count; i++) {
				var collider = own[i];

				if (!collider)
					continue;

				for (var j = 0; j < others.Count; j++) {
					var other = others[j];

					if (!other || other == collider || _ignoredBody.Contains((collider, other)))
						continue;

					Physics.IgnoreCollision(collider, other, true);
					_ignoredBody.Add((collider, other));
				}
			}
		}

		/// <summary>Gives the collisions with the body of the grabber back.</summary>
		private void RestoreGrabberBody() {
			for (var i = 0; i < _ignoredBody.Count; i++) {
				var (own, body) = _ignoredBody[i];

				if (own && body)
					Physics.IgnoreCollision(own, body, false);
			}

			_ignoredBody.Clear();
		}

		/// <summary>Velocity of the grab point over the throw window, used when the object is thrown.</summary>
		private void MeasureThrowVelocity(IGrabber grabber) {
			var window = Mathf.Max(throwWindow, 0.05f);

			_releaseVelocity = Vector3.zero;

			if (throwOnRelease && _hasPointPosition && _motion.Count >= 2) {
				var last = _motion[_motion.Count - 1];
				var index = _motion.Count - 1;

				while (index > 0 && _motion[index - 1].Time >= last.Time - window)
					index--;

				_releaseVelocity = VelocityFrom(index, last);

				// The hand had already stopped when it let go: reach a little further back so the flick that
				// ended just before is not swallowed by the frames that followed it.
				while (index > 0 && _releaseVelocity.magnitude < 0.1f && _motion[index - 1].Time >= last.Time - window * 2f)
					_releaseVelocity = VelocityFrom(--index, last);
			}

			_motion.Clear();
		}

		/// <summary>Velocity between a sample of the history and the latest one.</summary>
		private Vector3 VelocityFrom(int index, (float Time, Vector3 Position) last) {
			var delta = last.Time - _motion[index].Time;

			return delta > 0.0001f
				? (last.Position - _motion[index].Position) / delta * throwScale
				: Vector3.zero;
		}

		protected virtual void LateUpdate() {
			if (_holders.Count > 0 && !HoldsRenderPose())
				FollowGrabPoint();
		}

		/// <summary>Whether the holder poses the object itself, once the frame is rendered.</summary>
		private bool HoldsRenderPose()
			=> Holder is IGrabberRenderPose deferred && deferred.PosesAtRender;

		/// <summary>Applies the pose of the grab point right now, for a grabber whose pose is final only at render time.</summary>
		public void FollowGrabPose() {
			if (_holders.Count > 0)
				FollowGrabPoint();
		}

		private void OnDisable()
			=> ReleaseAll();

		private void OnDrawGizmosSelected() {
			Gizmos.color = mode == GrabMode.Snap
				? new Color(0.2f, 0.9f, 0.6f, 1f)
				: new Color(0.5f, 0.7f, 1f, 1f);

			DrawAnchor(grabAnchor);

			for (var i = 0; i < sideAnchors.Length; i++)
				DrawAnchor(sideAnchors[i].Anchor);
		}

		private static void DrawAnchor(Transform anchor) {
			if (!anchor)
				return;

			Gizmos.DrawWireSphere(anchor.position, 0.03f);
			Gizmos.DrawLine(anchor.position, anchor.position + anchor.up * 0.08f);
		}
	}
}
