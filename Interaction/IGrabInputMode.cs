namespace Nox.CCK.Interaction {
	/// <summary>
	/// A <see cref="IGrabbable"/> that decides how a grab starts and stops, instead of leaving it to each
	/// grabber.
	/// <para>
	/// A personal mirror wants to be taken by a single press and stay taken (<see cref="GrabInputMode.Toggle"/>)
	/// even when the pointer that grabbed it is configured to hold its objects. A grabber reading this
	/// contract uses the mode of the object it is over (to start) or holding (to stop).
	/// </para>
	/// </summary>
	public interface IGrabInputMode {
		/// <summary>Hold: the object is carried while the input is pressed. Toggle: a press takes it, the next press releases it.</summary>
		GrabInputMode InputMode { get; }
	}
}
