using System;
using System.Collections.Generic;

namespace Main
{
	// maybe todo: make-so defaults are in a packaged VDF file/text-block, rather than being set here in the class
	[VDFType(propIncludeRegexL1: "", popOutL1: true)] public class Session
	{
		[VDFPreDeserialize] public Session() {}
		//public Session(string type, long timeStarted, int timeLeft, )

		public string type;
		public DateTime? timeStarted; // utc
		public List<SessionPause> timePauses = new List<SessionPause>();
		public DateTime? timeStopped; // utc
		
		/// <summary>Time till timer 'runs out' to zero, in seconds. (negative if past timeout, causing alarm volume to progressively increase)</summary>
		public int timeLeft;
		/// <summary>'time over' counter that will say lower than the actual, if locking was used; used for time-over progress-bar</summary>
		public int timeOver_withLocking;
		/// <summary>UTC time up to which time's passing has been processed.</summary>
		public DateTime processedTimeExtent;
		public bool paused;
		public bool locked;
	}
	public class SessionPause
	{
		[VDFPreDeserialize] public SessionPause() {}
		
		public DateTime? timeStarted; // utc
		public DateTime? timeStopped; // utc
	}
}