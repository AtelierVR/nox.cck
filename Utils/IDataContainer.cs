using System;
using Nox.CCK.Events;

namespace Nox.CCK {
	/// <summary>
	/// Key-value container holding complex values (team, identifier, walk speed, jump power, ...),
	/// as opposed to <c>IProperty</c> (see <c>nox.entities</c>) which carries the avatar parameters.
	/// <para>
	/// Values are kept as-is (any CLR object). Whether they are persisted depends on the
	/// implementation: the entity container is in-memory and stays out of the avatar pipeline
	/// (anti-cheat), the <c>nox.players</c> store is persisted to disk and <c>nox.table</c>, and a
	/// runtime avatar keeps its own (see <c>IRuntimeAvatar.Data</c>).
	/// </para>
	/// <para>
	/// It lives in the CCK, not in <c>nox.entities</c>, so a CCK assembly (an avatar, a world) can hold
	/// one without depending on the entity pipeline.
	/// </para>
	/// </summary>
	public interface IDataContainer : IDisposable {
		/// <summary>Read a typed value, or <paramref name="default"/> if not set.</summary>
		T Get<T>(string[] key, T @default = default);

		/// <summary>Read a typed value from a single-segment key.</summary>
		T Get<T>(string key, T @default = default);

		/// <summary>Check whether a key exists.</summary>
		bool Has(string[] key);

		/// <summary>Check whether a single-segment key exists.</summary>
		bool Has(string key);

		/// <summary>Write a typed value.</summary>
		void Set<T>(string[] key, T value);

		/// <summary>Write a typed value for a single-segment key.</summary>
		void Set<T>(string key, T value);

		/// <summary>Remove a key.</summary>
		void Delete(string[] key);

		/// <summary>Remove a single-segment key.</summary>
		void Delete(string key);

		/// <summary>Raised on every <c>Set</c>/<c>Delete</c> with the key, the new and the old value.</summary>
		NoxEvent<string[], object, object> OnChanged { get; }
	}
}
