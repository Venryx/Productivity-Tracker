﻿using System;
using System.Collections.Generic;
using System.Linq;
using VDFN;

namespace Main
{
	// maybe todo: make-so defaults are in a packaged VDF file/text-block, rather than being set here in the class
	[VDFType(propIncludeRegexL1: "", popOutL1: true)] public class Session
	{
		[VDFPreDeserialize] protected Session() {}
		public Session(string type, DateTime timeStarted, int timeLeft)
		{
			this.type = type;
			LoadSessionTypeFromSessionTypeName();
			this.timeStarted = timeStarted;
			this.timeLeft = timeLeft;
		}

		//[VDFPreDeserialize] protected void PreDeserialize(VDFNode node, VDFNodePath path, VDFLoadOptions options) { sessionType = MainActivity.main.mainData.settings.sessionTypes.FirstOrDefault(a=>a.name == type); }
		[VDFPostDeserialize] protected void PostDeserialize(VDFNode node, VDFNodePath path, VDFLoadOptions options) { LoadSessionTypeFromSessionTypeName(); }
		public void LoadSessionTypeFromSessionTypeName() { sessionType = MainActivity.main.mainData.settings.sessionTypes.FirstOrDefault(a=>a.name == type); }
		[VDFPreSerialize] protected void PreSerialize()
		{
			if (sessionType != null)
				type = sessionType.name;
		}

		public string type; // make-so: named sessionTypeName
		[VDFProp(false)] public SessionType sessionType;
		public DateTime timeStarted; // utc
		public DateTime? timeStopped; // utc
		[VDFProp(popOutL2: true)] public List<Subsession> subsessions = new List<Subsession>();

		/// <summary>Time till timer 'runs out' to zero, in seconds. (negative if past timeout, causing alarm volume to progressively increase)</summary>
		public int timeLeft;
		/// <summary>'time over' counter that will say lower than the actual, if locking was used; used for time-over progress-bar</summary>
		public int timeOver_withLocking;
		/// <summary>UTC time up to which time's passing has been processed.</summary>
		public DateTime processedTimeExtent;
		public bool paused;
		public bool locked;

		public Session Clone() { return VDF.Deserialize<Session>(VDF.Serialize<Session>(this)); }
	}
	[VDFType(propIncludeRegexL1: "")] public class Subsession // period of time in which a session was 'running' (i.e. not paused)
	{
		[VDFPreDeserialize] protected Subsession() {}
		public Subsession(DateTime timeStarted) { this.timeStarted = timeStarted; }
		
		public DateTime timeStarted; // utc
		public DateTime? timeStopped; // utc
	}
}