using UnityEngine;

namespace Nox.CCK.Build {
	/// <summary>
	/// Free-form note attached to an object. The content is purely informational (reminders, credits,
	/// instructions…) and the whole component is stripped from the asset during the build, so it never
	/// reaches the published bundle.
	/// </summary>
	[AddComponentMenu("Nox/Build/Build Note")]
	public class BuildNote : MonoBehaviour, IRemoveOnBuild {
		[SerializeField]
		[TextArea(3, 20)]
		[Tooltip("Informational text, kept in the project but removed from the build.")]
		private string _information = string.Empty;

		/// <summary>
		/// Informational text displayed in the inspector. This is never shipped with the build.
		/// </summary>
		public string Information {
			get => _information;
			set => _information = value;
		}

		public override string ToString()
			=> _information;
	}
}
