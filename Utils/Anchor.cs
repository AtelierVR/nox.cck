using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nox.CCK.Utils {
	/// <summary>
	/// Component marking a sandbox anchor boundary in the transform hierarchy.
	/// Used to prevent propagation (such as parent component searches, script sandbox boundaries,
	/// or editor helper duplicate detection) across the anchor boundary.
	/// <para>
	/// The tags name the scopes the anchor belongs to (<c>"world"</c>, <c>"session"</c>) and carry
	/// the scope identifiers (the session id, for instance), so a consumer can stop on the anchor
	/// that owns its own scope.
	/// </para>
	/// </summary>
	public class Anchor : MonoBehaviour {
		[SerializeField]
		private List<string> tags = new();

		/// <summary>Tags associated with this anchor boundary.</summary>
		public List<string> Tags {
			get => tags ??= new List<string>();
			set => tags = value ?? new List<string>();
		}

		/// <summary>Adds a tag when it is not already present.</summary>
		public Anchor Add(string tag) {
			if (string.IsNullOrEmpty(tag))
				return this;

			var list = Tags;
			if (!list.Exists(existing => string.Equals(existing, tag, StringComparison.OrdinalIgnoreCase)))
				list.Add(tag);
			return this;
		}

		/// <summary>Adds several tags at once (see <see cref="Add(string)"/>).</summary>
		public Anchor Add(params string[] values) {
			if (values == null)
				return this;
			foreach (var value in values)
				Add(value);
			return this;
		}

		/// <summary>Checks whether this anchor contains the specified tag.</summary>
		public bool HasTag(string tag) {
			if (string.IsNullOrEmpty(tag) || tags == null)
				return false;
			for (var i = 0; i < tags.Count; i++)
				if (string.Equals(tags[i], tag, StringComparison.OrdinalIgnoreCase))
					return true;
			return false;
		}

		/// <summary>
		/// Tries to find the nearest anchor (starting at <paramref name="from"/> itself) carrying
		/// <paramref name="tag"/>.
		/// </summary>
		/// <returns><c>true</c> when a boundary was found; the hierarchy has none otherwise.</returns>
		public static bool TryFind(Transform from, string tag, out Anchor anchor) {
			anchor = null;

			if (!from || string.IsNullOrEmpty(tag))
				return false;

			for (var current = from; current; current = current.parent) {
				if (!current.TryGetComponent<Anchor>(out var candidate) || !candidate.HasTag(tag))
					continue;

				anchor = candidate;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Gets the topmost parent transform within the sandbox, stopping at any Anchor boundary
		/// that has the specified tag (or any anchor if none specified).
		/// </summary>
		public static Transform Clamp(Transform current, string tag = null) {
			Transform lastValid = current;
			while (current != null) {
				if (current.TryGetComponent<Anchor>(out var anchor))
					if (string.IsNullOrEmpty(tag) || anchor.HasTag(tag))
						return current;
				lastValid = current;
				current = current.parent;
			}
			return lastValid;
		}
	}
}
