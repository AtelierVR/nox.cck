namespace Nox.CCK.Properties {
	public interface IEditablePropertyObject : IPropertyObject {
		public void SetProperty(int key, object value);
		
		public void RemoveProperty(int key);
	}
}