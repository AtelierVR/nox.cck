using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Nox.CCK.Mods.Metadata {
	public interface IModMetadata {
		public string    DataType    { get; }
		public string    Id          { get; }
		public string[]  Provides    { get; }
		public Version   Version     { get; }
		public string[]  Permissions { get; }
		public IEntries  EntryPoints { get; }

		public string Name        { get; }
		public string Description { get; }
		public string License     { get; }
		public string GetIcon(uint size = 0);

		public IContact  Contact      { get; }
		public IPerson[] Authors      { get; }
		public IPerson[] Contributors { get; }

		public IRelation[]  Relations  { get; }
		public IRelation[]  Depends    { get; }
		public IRelation[]  Breaks     { get; }
		public IRelation[]  Conflicts  { get; }
		public IRelation[]  Recommends { get; }
		public IRelation[]  Suggests   { get; }
		public IReference[] References { get; }

		public T GetCustom<T>(string key, T defaultValue = default);

		public bool                       HasCustom<T>(string key);
		public Dictionary<string, object> Customs { get; }

		public JObject ToObject(ModMetadataFormat format = ModMetadataFormat.None);
		public string  ToJson(ModMetadataFormat format = ModMetadataFormat.None);

		public bool Match(IModMetadata req);
		public bool Match(string      id);
		public bool Match(IRelation    relation);


		#if UNITY_EDITOR
		public void SetCustom<T>(string key, T value);
		public bool Save(string path, ModMetadataFormat format = ModMetadataFormat.None);
		#endif
	}
}
