namespace Nox.CCK.Interaction {
	/// <summary>
	/// How a grabbable follows the grabber holding it.
	/// <para>
	/// The mode is per object, never per hand: the same prop behaves the same way whichever platform
	/// (an XR hand, a desktop pointer, an NPC or a script) takes it.
	/// </para>
	/// </summary>
	public enum GrabMode {
		/// <summary>
		/// Keeps the pose the object had when it was grabbed: both its position and its rotation follow
		/// the grab point. The natural mode for a prop picked up where it lies (a mirror, a cup, a ball).
		/// </summary>
		Free,

		/// <summary>
		/// Snaps the object to its <c>grabAnchor</c> - the local pose marking where the hand should hold
		/// it - then both its position and its rotation follow the grab point. The mode for tools whose
		/// grip must stay fixed in the hand (a gun, a sword, a torch).
		/// </summary>
		Snap,

		/// <summary>
		/// Position only: the object follows the translation of the grab point but keeps the world
		/// rotation it had when it was grabbed. A tray or a drink that must stay level.
		/// </summary>
		FixedRotation,

		/// <summary>
		/// Rotation only: the object follows the rotation of the grab point but keeps the world position
		/// it had when it was grabbed. A dial or a screen the player turns in place.
		/// </summary>
		FixedPosition
	}

	/// <summary>How an input starts and stops a grab.</summary>
	public enum GrabInputMode {
		/// <summary>The object is held while the input is pressed, and released when it is let go.</summary>
		Hold,

		/// <summary>A press takes the object, the next press releases it.</summary>
		Toggle
	}
}
