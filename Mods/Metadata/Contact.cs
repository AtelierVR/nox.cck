using System.Collections.Generic;

namespace Nox.CCK.Mods.Metadata
{
    public interface IContact
    {
        T Get<T>(string key) where T : class;
        bool Has<T>(string key) where T : class;
        Dictionary<string, object> GetAll();
    }
}