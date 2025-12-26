using System.Collections.Generic;
using System.Linq;

namespace Nox.CCK.Properties {
	public abstract class BasePropertyObject : IPropertyObject {
		protected readonly Dictionary<int, object> Properties = new();

		public KeyValuePair<int, object>[] GetProperties()
			=> Properties.ToArray();

		public T GetProperty<T>(int key)
			=> (T)Properties[key];

		public bool TryGetProperty<T>(int key, out T property) {
			if (Properties.TryGetValue(key, out var obj) && obj is T t) {
				property = t;
				return true;
			}

			property = default;
			return false;
		}

		public bool HasProperty(int key)
			=> Properties.ContainsKey(key);
	}
}