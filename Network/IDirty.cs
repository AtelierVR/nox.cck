namespace Nox.CCK.Network {
	/// <summary>
	/// Interface for objects that can be marked as "dirty" when modified.
	/// </summary>
	public interface IDirty {
		/// <summary>
		/// Indicates whether the object has been modified since the last sync.
		/// </summary>
		public bool IsDirty { get; set; }
	}
}