using System.Collections.Generic;

namespace Nox.CCK.Properties {
	public interface IPropertyObject {
		/// <summary>
		/// Get all properties as key-value pairs.
		/// </summary>
		/// <returns></returns>
		public KeyValuePair<int, object>[] GetProperties();

		/// <summary>
		/// Get a property by its key.
		/// </summary>
		/// <param name="key"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetProperty<T>(int key);

		/// <summary>
		/// Try to get a property by its key.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="property"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public bool TryGetProperty<T>(int key, out T property);

		/// <summary>
		/// Check if a property exists by its key.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool HasProperty(int key);
	}
}