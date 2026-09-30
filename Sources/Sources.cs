using System.Collections.Generic;

namespace Nox.CCK.Utils {
	/// <summary>
	/// Holds a value coming from one of several sources: the values are stored in a list
	/// and retrieved by their type (see <see cref="Is{T}"/> / <see cref="TryGet{T}"/>).
	/// Useful to expose a value that can either be a "lazy" descriptor (url, id...)
	/// or the already resolved resource.
	/// </summary>
	public class Sources {
		private readonly List<object> _values = new();

		public Sources(params object[] values) {
			if (values == null)
				return;

			foreach (var value in values) {
				if (value == null)
					continue;
				_values.Add(value);
			}
		}

		/// <summary>The values held by this source (in insertion order).</summary>
		public IReadOnlyList<object> Values
			=> _values;

		/// <summary>Whether the source holds a value assignable to <typeparamref name="T"/>.</summary>
		public bool Is<T>()
			=> TryGet<T>(out _);

		/// <summary>Gets the value assignable to <typeparamref name="T"/>, if any.</summary>
		public bool TryGet<T>(out T value) {
			foreach (var v in _values)
				if (v != null && typeof(T).IsInstanceOfType(v)) {
					value = (T)v;
					return true;
				}

			value = default;
			return false;
		}

		/// <summary>Whether no source is set.</summary>
		public bool IsEmpty
			=> _values.Count == 0;

		/// <summary>Adds a value to this source.</summary>
		public Sources Add<T>(T value) {
			if (value != null)
				_values.Add(value);
			return this;
		}

		/// <summary>Creates a source holding the given value.</summary>
		public static Sources From<T>(T value)
			=> new(value);

		/// <summary>A source holding no value.</summary>
		public static Sources Empty
			=> new();
	}
}
