using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace Nox.CCK.Utils {
	/// <summary>
	/// A struct that represents a unique identifier for an object,
	/// which can include a type,
	/// an ID (either string or uint),
	/// an optional server address,
	/// and an optional query dictionary.
	/// The string representation of the identifier is in the format:
	/// [type:][id][?key=value[&amp;key=value...]][@server]
	/// </summary>
	public readonly struct Identifier : IEquatable<Identifier> {
		public static readonly Identifier Invalid = new(null, 0u, null, null);

		public const string LOCAL_SERVER = "::";

		public readonly string Type;

		public readonly object Id;

		public readonly string Server;

		public readonly Dictionary<string, string[]> Query;

		public Identifier(string type, object id, Dictionary<string, string[]> query = null, string server = LOCAL_SERVER) {
			if (!string.IsNullOrEmpty(type) && (type.Length != 1 || type.Contains(':')))
				throw new ArgumentException("Type must be either null, or a single character that is not ':'", nameof(type));
			Type = type;
			if (id is not string and not uint)
				throw new ArgumentException("Id must be either string or uint", nameof(id));
			if (id is string str && string.IsNullOrEmpty(str))
				throw new ArgumentException("String id cannot be null or empty", nameof(id));
			Id    = id;
			Query = query ?? new Dictionary<string, string[]>();
			Server = string.IsNullOrEmpty(server)
				? LOCAL_SERVER
				: server;
		}

		public static Identifier Parse(string raw) {
			string server = null;
			var    atIdx  = raw.LastIndexOf('@');
			if (atIdx != -1 && atIdx < raw.Length - 1) {
				server = raw[(atIdx + 1)..];
				raw    = raw[..atIdx];
			}

			var query = new Dictionary<string, string[]>();
			var qIdx  = raw.IndexOf('?');
			if (qIdx != -1) {
				var qs = HttpUtility.ParseQueryString(raw[(qIdx + 1)..]);
				foreach (var k in qs.AllKeys) {
					if (k == null)
						continue;
					query[k] = qs.GetValues(k);
				}
				raw = raw[..qIdx];
			}

			string type     = null;
			var    colonIdx = raw.IndexOf(':');
			if (colonIdx == 1) {
				type = raw[..colonIdx];
				raw  = raw[(colonIdx + 1)..];
			}

			return new Identifier(type, raw, query, server);
		}

		public uint NumericId
			=> Id switch {
				uint u => u,
				string s when uint.TryParse(s, out var p)
					=> p,
				_ => 0u
			};

		public string StringId
			=> Id switch {
				string s => s,
				uint u   => u.ToString(),
				_        => null
			};

		public bool IsLocal(string localAddress = null)
			=> Server == null
				|| string.IsNullOrEmpty(Server)
				|| Server == LOCAL_SERVER
				|| Server == localAddress;

		/// <summary>
		/// Converts the identifier back to its string representation,
		/// using the provided fallback address if the server is local or not specified.
		/// </summary>
		/// <param name="fallbackAddress"></param>
		/// <returns></returns>
		public string ToString(string fallbackAddress = LOCAL_SERVER) {
			if (string.IsNullOrEmpty(fallbackAddress))
				fallbackAddress = null;

			var sb = new StringBuilder();

			if (Type != null) {
				sb.Append(Type);
				sb.Append(':');
			}

			sb.Append(StringId ?? NumericId.ToString());

			if (Query is { Count: > 0 }) {
				sb.Append('?');
				var first = true;
				foreach (var kvp in Query) {
					var key = HttpUtility.UrlEncode(kvp.Key);
					foreach (var value in kvp.Value) {
						var k = HttpUtility.UrlEncode(kvp.Key);
						var v = HttpUtility.UrlEncode(value);
						if (!first)
							sb.Append('&');
						else
							first = false;
						sb.Append(k);
						sb.Append('=');
						sb.Append(v);
					}
				}
			}

			if (!string.IsNullOrEmpty(Server) && Server != LOCAL_SERVER) {
				sb.Append("@");
				sb.Append(Server);
			} else if (!string.IsNullOrEmpty(fallbackAddress)) {
				sb.Append("@");
				sb.Append(fallbackAddress);
			}

			return sb.ToString();
		}

		public string ToShortString(bool withServer = true) {
			var sb = new StringBuilder();
			sb.Append(StringId ?? NumericId.ToString());
			if (!withServer || Server == LOCAL_SERVER)
				return sb.ToString();
			sb.Append("@");
			sb.Append(Server);
			return sb.ToString();
		}

		public override string ToString()
			=> ToString(null);

		/// <summary>
		/// Check equality with another object
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
			=> obj is Identifier identifier && Equals(identifier);

		/// <summary>
		/// Get the hash code of the identifier
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
			=> HashCode.Combine(Id, Server);



		/// <summary>
		/// Check equality with another identifier,
		/// ignoring the type and treating local servers as equal
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(Identifier other)
			=> IsValid() == other.IsValid()
				&& (string.IsNullOrEmpty(Type) || string.IsNullOrEmpty(other.Type) || Type.Equals(other.Type))
				&& StringId.Equals(other.StringId)
				&& (Server.Equals(LOCAL_SERVER) || other.Server.Equals(LOCAL_SERVER) || Server.Equals(other.Server));

		public bool IsValid()
			=> Id switch {
				string s => !string.IsNullOrEmpty(s),
				uint u   => u != 0,
				_        => false
			};
	}
}