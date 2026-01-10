using System;
using UnityEngine.Events;
namespace Nox.CCK.Events {
	public class NoxEvent : UnityEvent {
		public NoxEvent() : base() { }
	}
	
	public class NoxEvent<T0> : UnityEvent<T0> {
		public NoxEvent() : base() { }
	}

	public class NoxEvent<T0, T1> : UnityEvent<T0, T1> {
		public NoxEvent() : base() { }
	}

	public class NoxEvent<T0, T1, T2> : UnityEvent<T0, T1, T2> {
		public NoxEvent() : base() { }
	}

	public class NoxEvent<T0, T1, T2, T3> : UnityEvent<T0, T1, T2, T3> {
		public NoxEvent() : base() { }
	}
}