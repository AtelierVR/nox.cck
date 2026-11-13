namespace Nox.CCK.Interaction {
	/// <summary>
	/// A <see cref="IGrabbable"/> that stays where it is in the world instead of being brought to the hand.
	/// <para>
	/// A grabber that reads this contract keeps the object at the distance it was grabbed at: a menu opened
	/// across the room stays across the room and follows the pointer, a mirror keeps its place. Without it a
	/// far grab pulls the object in front of the player, which is what a prop wants but not a panel.
	/// </para>
	/// </summary>
	public interface IGrabAtDistance {
		/// <summary>Whether the object must stay at the distance it was grabbed at.</summary>
		bool KeepsGrabDistance { get; }
	}
}
