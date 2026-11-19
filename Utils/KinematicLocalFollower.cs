using UnityEngine;

namespace Nox.CCK.Utils {
	/// <summary>
	/// Keeps the body on the local pose of its transform: when the parent moves (a tracking space, an avatar, an
	/// animator), the body is put back where the transform is, so it stays at the same local position and rotation
	/// under that parent instead of lagging behind the move. What the physics does to the body is written on the
	/// transform, so what it touches can still stop it.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	[DefaultExecutionOrder(900)]
	public class KinematicLocalFollower : MonoBehaviour {
		private Rigidbody _body;
		private Transform _parent;
		private Vector3 _parentPosition;
		private Quaternion _parentRotation = Quaternion.identity;

		private void Awake() {
			_body = GetComponent<Rigidbody>();
			_parent = transform.parent;
			RememberParent();
		}

		private void Update()
			=> Hold();

		private void LateUpdate()
			=> Hold();

		private void FixedUpdate()
			=> Hold();

		private void Hold() {
			if (!_body)
				return;

			if (ParentMoved()) {
				_body.position = transform.position;
				_body.rotation = transform.rotation;
				RememberParent();
				return;
			}

			var position = _body.position;
			var rotation = _body.rotation;

			if (transform.position != position || transform.rotation != rotation)
				transform.SetPositionAndRotation(position, rotation);
		}

		private bool ParentMoved()
			=> _parent && (_parent.position != _parentPosition || _parent.rotation != _parentRotation);

		private void RememberParent() {
			if (!_parent)
				return;

			_parentPosition = _parent.position;
			_parentRotation = _parent.rotation;
		}
	}
}
