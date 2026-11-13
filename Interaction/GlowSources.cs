namespace Nox.CCK.Interaction {
	/// <summary>
	/// Number of glow mask sources alive in the scene.
	/// <para>
	/// The <c>OutlineGlowFeature</c> used to record its mask, blur and composite passes for every camera
	/// of every frame, even when nothing carried the glow layer: with an empty mask the result was
	/// identical to the untouched colour buffer, for four render graph textures, a full screen copy and
	/// three fullscreen passes per camera. Sources report themselves here when they appear and disappear
	/// (see <see cref="GrabbableHighlight"/>), so the feature is only enqueued while something can glow.
	/// </para>
	/// <para>
	/// Lives in the <c>Nox.CCK</c> assembly on purpose: the render feature is in <c>Nox.CCK.Rendering</c>,
	/// which references this one, and not the other way around.
	/// </para>
	/// </summary>
	public static class GlowSources {
		private static int _count;

		/// <summary>Whether at least one source can glow.</summary>
		public static bool Any
			=> _count > 0;

		/// <summary>Registers a source that will carry the glow layer.</summary>
		public static void Add()
			=> _count++;

		/// <summary>Unregisters a source registered with <see cref="Add"/>.</summary>
		public static void Remove() {
			if (_count > 0)
				_count--;
		}
	}
}
