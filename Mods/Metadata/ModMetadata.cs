using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Nox.CCK.Mods.Metadata {
	public interface IModMetadata {
		public string    GetDataType();
		public string    GetId();
		public string[]  GetProvides();
		public Version   GetVersion();
		public string[]  GetPermissions();
		public IEntries   GetEntryPoints();

		public string GetName();
		public string GetDescription();
		public string GetLicense();
		public string GetIcon(uint size = 0);

		public IContact  GetContact();
		public IPerson[] GetAuthors();
		public IPerson[] GetContributors();

		public IRelation[]  GetRelations();
		public IRelation[]  GetDepends();
		public IRelation[]  GetBreaks();
		public IRelation[]  GetConflicts();
		public IRelation[]  GetRecommends();
		public IRelation[]  GetSuggests();
		public Reference[] GetReferences();

		public T GetCustom<T>(string key, T defaultValue = default);

		public bool                       HasCustom<T>(string key);
		public Dictionary<string, object> GetCustoms();

		public JObject ToObject();
		public string  ToJson();

		public bool Match(IModMetadata req);
		public bool Match(string      id);
		public bool Match(IRelation    relation);


		#if UNITY_EDITOR
		public void SetCustom<T>(string key, T value);
		public bool Save(string         path);
		#endif
	}
}