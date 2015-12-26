using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Media;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Text;
using Java.IO;
using Java.Lang;
using Java.Util;
using VDFN;
using File = System.IO.File;
using Math = System.Math;
using Timer = System.Timers.Timer;
using Orientation = Android.Widget.Orientation;
using Stream = Android.Media.Stream;

namespace Main
{
	[VDFType(propIncludeRegexL1: "", popOutL1: true)] public class MainData
	{
		[VDFPreDeserialize] public MainData() {} // makes-so in-code defaults are used, for props that aren't set in the VDF

		public Settings settings = new Settings();
	}
	// maybe todo: make-so defaults are in a packaged VDF file/text-block, rather than being set here in the class
	[VDFType(propIncludeRegexL1: "", popOutL1: true)] public class Settings
	{
		[VDFPreDeserialize] public Settings() {}

		// general
		// ==========

		public bool keepScreenOnWhileOpen = true;
		public bool fastMode;

		// productivity graph
		// ==========

		public int daysVisibleAtOnce = 7;
		public int startGraphAtLocalHourX;
		public bool showLocalTime = true;
		public bool show12HourTime;

		// timer
		// ==========

		public int numberOfTimerSteps = 11;
		public int timeIncrementForTimerSteps = 10;
		//public bool addCustomButton;
		//public bool addAlarmButton;

		// session types
		// ==========

		public List<SessionType> sessionTypes = new List<SessionType> {new SessionType("Rest"), new SessionType("Work")};
		public string selectedSessionTypeName;

		// others
		// ==========

		public double keyHoldLength = 3;
		public bool blockUnusedKeys;
		//public bool blockMouseEvents;
		[VDFProp(popOutL2: true)] public List<Hotkey> hotkeys = new List<Hotkey>();
	}
	[VDFType(propIncludeRegexL1: "")] public class SessionType
	{
		[VDFPreDeserialize] protected SessionType() {}
		public SessionType(string name) { this.name = name; }

		public string name;
		public Color color = Color.Black;
		public int setMasterAlarmVolume = -1;
		public string alarmSoundFilePath;
		public int minVolume;
		public int maxVolume = 50;
		public int timeToMaxVolume = 10;
		//public VolumeScaleType volumeFadeType = VolumeScaleType.Loudness;
	}
	public enum HotkeyAction
	{
		None,
		StartSession,
		PauseSession,
		ToggleSessionPaused,
		StopSession,
		TurnScreenOff,
		TurnScreenOn,
		ToggleScreenOn
	}
	[VDFType(propIncludeRegexL1: "")] public class Hotkey
	{
		[VDFPreDeserialize] public Hotkey() {}
		public Keycode key;
		public HotkeyAction action;
		public string action_startSession_type;
		public int action_startSession_length = 10;
	}
}