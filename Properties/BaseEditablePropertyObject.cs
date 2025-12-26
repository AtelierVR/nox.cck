namespace Nox.CCK.Properties {
	public abstract class BaseEditablePropertyObject : BasePropertyObject, IEditablePropertyObject {
		public void SetProperty(int key, object value)
			=> Properties[key] = value;

		public void RemoveProperty(int key)
			=> Properties.Remove(key);
	}
}