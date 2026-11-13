using Cysharp.Threading.Tasks;

namespace Nox.CCK {
	/// <summary>
	/// A <see cref="IDataContainer"/> with a storage behind it, so the values can come from it and go back
	/// to it. Both calls complete once the storage is really read or written, which lets a caller wait for
	/// them instead of guessing (see the avatar shutdown, which must not outlive the table it writes to).
	/// <para>
	/// Whether the storage is a file, an account table or nothing at all is up to the implementation: an
	/// in-memory container answers both calls immediately.
	/// </para>
	/// </summary>
	public interface ISavableContainer : IDataContainer {
		/// <summary>Fills the container from its storage. Completes once the read is done.</summary>
		public UniTask Load();

		/// <summary>
		/// Writes the container to its storage. Completes once the write is done; a write already in flight
		/// is awaited instead of starting a second one.
		/// </summary>
		public UniTask Save();
	}
}
