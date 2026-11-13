using System;
using System.Diagnostics;

namespace Nox.CCK.Utils {
	/// <summary>
	/// High resolution wall clock.
	/// <para>
	/// <see cref="DateTime.UtcNow"/> is rounded to the system timer tick (several milliseconds on
	/// Windows): two stamps taken in the same tick read as identical, which makes anything timed with
	/// them — a round trip over a local relay, for instance — measure exactly 0 ms. Anchoring a
	/// monotonic counter to one wall clock instant gives the same instants with a resolution of a
	/// fraction of a microsecond.
	/// </para>
	/// </summary>
	public static class Clock {
		private static readonly DateTime _epoch = DateTime.UtcNow;
		private static readonly long _epochTicks = Stopwatch.GetTimestamp();

		/// <summary>Current UTC instant, read from the monotonic counter.</summary>
		public static DateTime UtcNow {
			get {
				var elapsed = Stopwatch.GetTimestamp() - _epochTicks;
				return _epoch.AddTicks((long)(elapsed * (TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency)));
			}
		}
	}
}
