using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Java.IO;
using Java.Lang;
using File = System.IO.File;

namespace Main
{
	[VDFType(propIncludeRegexL1: "", popOutL1: true)] public class MainData
	{
		[VDFPreDeserialize] public MainData() {} // makes-so in-code defaults are used, for props that aren't set in the VDF

		public Settings settings = new Settings();

		public bool currentTimerExists;
		public long currentTimer_lastResumeTime = -1;
		public int currentTimer_timeLeft;
		public bool currentTimer_paused;
		public TimerType currentTimer_type;
	}
	// maybe todo: make-so defaults are in a packaged VDF file/text-block, rather than being set here in the class
	[VDFType(propIncludeRegexL1: "", popOutL1: true)] public class Settings
	{
		[VDFPreDeserialize] public Settings() {}

		public bool keepScreenOnWhileOpen = true;
		public int numberOfTimerSteps = 11;
		public int timeIncrementForTimerSteps = 10;

		public string alarmSoundFilePath;
		public int minVolume;
		public int maxVolume = 50;
		public int timeToMaxVolume = 10;
		
		[VDFProp(popOutL2: true)] public List<Hotkey> hotkeys = new List<Hotkey>();
	}
	public enum VKey
	{
		None,
		VolumeUp,
		VolumeDown
	}
	public enum HotkeyAction
	{
		None,
		StartTimer_Rest,
		StartTimer_Work
	}
	[VDFType(propIncludeRegexL1: "")] public class Hotkey
	{
		public VKey key;
		public HotkeyAction action;
		public int action_startTimer_length = 10;
	}

	public enum TimerType
	{
		Rest,
		Work
	}

	[Activity(Label = "Productivity Tracker", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		public static MainActivity main;

		public MainData mainData = new MainData();
		public void LoadMainData()
		{
			var file = new FileInfo("/storage/sdcard0/Productivity Tracker/MainData.vdf");
			if (file.Exists)
			{
				var vdf = File.ReadAllText(file.FullName);
				mainData = VDF.Deserialize<MainData>(vdf);
			}
		}
		public void SaveMainData()
		{
			var file = new FileInfo("/storage/sdcard0/Productivity Tracker/MainData.vdf").CreateFolders();
			var vdf = VDF.Serialize<MainData>(mainData);
			File.WriteAllText(file.FullName, vdf);
		}

		int count;
		protected override void OnCreate(Bundle bundle)
		{
			main = this;
			base.OnCreate(bundle);
			SetContentView(Resource.Layout.Main);

			LoadMainData();
			RefreshKeepScreenOn();
			RefreshTimerStepButtons();
			RefreshDynamicUI();
			// if current-timer should be running, make sure its running by pausing-and-resuming (scheduled alarm awakening might have been lost on device reboot)
			// maybe make-so: current-timer's scheduled awakening is rescheduled on device startup as well
			if (mainData.currentTimerExists && !mainData.currentTimer_paused)
			{
				PauseTimer();
				ResumeTimer();
			}

			var timeLeftPanel = FindViewById<FrameLayout>(Resource.Id.TimeLeftPanel);
			{
				var timeLeftBar = FindViewById<ImageView>(Resource.Id.TimeLeftBar);
				var timeLeftBar_clip = (ClipDrawable)timeLeftBar.Drawable;
				timeLeftBar_clip.SetLevel((int)(10000 * .5));

				var timeLeftLabel = timeLeftPanel.AddChild(new TextView(this) {Gravity = GravityFlags.Center, TextSize = 30}, new FrameLayout.LayoutParams(200, 100) {Gravity = GravityFlags.Center});
				timeLeftLabel.SetSingleLine(true);
				timeLeftLabel.Text = "10:10";
			}

			var timeOverPanel = FindViewById<FrameLayout>(Resource.Id.TimeOverPanel);
			{
				var timeOverBar = FindViewById<ImageView>(Resource.Id.TimeOverBar);
				var timeOverBar_clip = (ClipDrawable)timeOverBar.Drawable;
				timeOverBar_clip.SetLevel((int)(10000 * 1));

				var soundIconButton = timeOverPanel.AddChild(new ImageButton(this), new FrameLayout.LayoutParams(30, 30) {Gravity = GravityFlags.CenterVertical});
				soundIconButton.SetBackgroundResource(Resource.Drawable.Volume);

				var timeOverLabel = timeOverPanel.AddChild(new TextView(this) {Gravity = GravityFlags.Center, TextSize = 30}, new FrameLayout.LayoutParams(200, 100) {Gravity = GravityFlags.Center});
				timeOverLabel.SetSingleLine(true);
				timeOverLabel.Text = "10:10";
			}

			FindViewById<Button>(Resource.Id.Pause).Click += delegate
			{
				if (mainData.currentTimer_paused)
					ResumeTimer();
				else
					PauseTimer();
			};
			FindViewById<Button>(Resource.Id.Stop).Click += delegate { StopTimer(); };
		}
		public void RefreshKeepScreenOn()
		{
			if (mainData.settings.keepScreenOnWhileOpen)
				Window.AddFlags(WindowManagerFlags.KeepScreenOn);
			else
				Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
		}
		public void RefreshTimerStepButtons()
		{
			var restButtonsPanel = FindViewById<LinearLayout>(Resource.Id.RestButtons);
			while (restButtonsPanel.ChildCount > 1)
				restButtonsPanel.RemoveViewAt(1);
			for (var i = 0; i < mainData.settings.numberOfTimerSteps; i++)
			{
				var timerStepButton = restButtonsPanel.AddChild(new Button(this), new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .75f) { Gravity = GravityFlags.CenterVertical });
				timerStepButton.Text = ((mainData.settings.numberOfTimerSteps - (i + 1)) * mainData.settings.timeIncrementForTimerSteps).ToString();
				var timerStepLength = mainData.settings.timeIncrementForTimerSteps * i;
                timerStepButton.Click += (sender, e)=>{ StartTimer(TimerType.Rest, timerStepLength); };
			}

			var workButtonsPanel = FindViewById<LinearLayout>(Resource.Id.WorkButtons);
			while (workButtonsPanel.ChildCount > 1)
				workButtonsPanel.RemoveViewAt(1);
			for (var i = 0; i < mainData.settings.numberOfTimerSteps; i++)
			{
				var timerStepButton = workButtonsPanel.AddChild(new Button(this), new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .75f) { Gravity = GravityFlags.CenterVertical });
				timerStepButton.Text = ((mainData.settings.numberOfTimerSteps - (i + 1)) * mainData.settings.timeIncrementForTimerSteps).ToString();
				var timerStepLength = mainData.settings.timeIncrementForTimerSteps * i;
				timerStepButton.Click += (sender, e)=>{ StartTimer(TimerType.Rest, timerStepLength); };
			}
		}
		protected override void OnPause()
		{
			base.OnPause();
			if (refreshDynamicUITimer != null && refreshDynamicUITimer.Enabled)
				refreshDynamicUITimer.Enabled = false;
			SaveMainData();
		}
		protected override void OnResume()
		{
			base.OnResume();
			if (mainData.currentTimer_lastResumeTime != -1)
				StartRefreshDynamicUITimer();
		}
		/*protected override void OnStop()
		{
			base.OnStop();
			SaveMainData();
		}*/

		PendingIntent GetLaunchUpdateServicePendingIntent()
		{
			var launchUpdateService = new Intent(this, typeof(UpdateService));
			launchUpdateService.AddFlags(ActivityFlags.SingleTop);
			return PendingIntent.GetService(this, 0, launchUpdateService, PendingIntentFlags.UpdateCurrent);
		}

		Timer refreshDynamicUITimer;
		void StartRefreshDynamicUITimer()
		{
			refreshDynamicUITimer = new Timer(1000);
			//refreshDynamicUITimer.Elapsed += delegate { RefreshDynamicUI(); };
			//refreshDynamicUITimer.Elapsed += delegate { new Handler().Post(RefreshDynamicUI); };
			refreshDynamicUITimer.Elapsed += delegate { RunOnUiThread(RefreshDynamicUI); ; };
			refreshDynamicUITimer.Enabled = true;
		}
		void RefreshDynamicUI()
		{
			FindViewById<Button>(Resource.Id.Stop).Enabled = mainData.currentTimerExists;
			FindViewById<Button>(Resource.Id.Pause).Enabled = mainData.currentTimerExists;
			FindViewById<Button>(Resource.Id.Pause).Text = mainData.currentTimerExists && mainData.currentTimer_paused ? "Resume" : "Pause";
			
			// make-so: time-left and time-over uis get updated
			// make-so: time-left and time-over uis get darkened/undarkened if timer is paused/resumed
		}

		void StartTimer(TimerType type, int minutes)
		{
			if (mainData.currentTimerExists)
				StopTimer();
			mainData.currentTimerExists = true;
            mainData.currentTimer_lastResumeTime = JavaSystem.CurrentTimeMillis();
			mainData.currentTimer_timeLeft = minutes * 60 * 1000;
			mainData.currentTimer_paused = false;
            mainData.currentTimer_type = type;
			((AlarmManager)GetSystemService(AlarmService)).Set(AlarmType.RtcWakeup, mainData.currentTimer_lastResumeTime + mainData.currentTimer_timeLeft, GetLaunchUpdateServicePendingIntent());
			
			RefreshDynamicUI();
			StartRefreshDynamicUITimer();
		}
		void PauseTimer()
		{
			var timePassedFromLastTimerResume = (int)(JavaSystem.CurrentTimeMillis() - mainData.currentTimer_lastResumeTime);
            mainData.currentTimer_timeLeft -= timePassedFromLastTimerResume;
            mainData.currentTimer_paused = true;
			((AlarmManager)GetSystemService(AlarmService)).Cancel(GetLaunchUpdateServicePendingIntent());
			RefreshDynamicUI();
		}
		void ResumeTimer()
		{
			mainData.currentTimer_paused = false;
			mainData.currentTimer_lastResumeTime = JavaSystem.CurrentTimeMillis();
			((AlarmManager)GetSystemService(AlarmService)).Set(AlarmType.RtcWakeup, mainData.currentTimer_lastResumeTime + mainData.currentTimer_timeLeft, GetLaunchUpdateServicePendingIntent());
			RefreshDynamicUI();
		}
		void StopTimer()
		{
			mainData.currentTimerExists = false;
			refreshDynamicUITimer.Enabled = false;
			((AlarmManager)GetSystemService(AlarmService)).Cancel(GetLaunchUpdateServicePendingIntent());
			RefreshDynamicUI();

			// make-so: time-left and time-over uis get cleared
		}

		public override bool OnCreateOptionsMenu(IMenu menu)
		{
			MenuInflater.Inflate(Resource.Menu.Main_Menu, menu);
			return base.OnCreateOptionsMenu(menu);
		}
		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			if (item.ItemId == Resource.Id.Settings)
				StartActivity(new Intent(this, typeof(SettingsActivity)));
			else if (item.ItemId == Resource.Id.About)
			{
				AlertDialog.Builder alert = new AlertDialog.Builder(this);
				alert.SetTitle("About Productivity Tracker");
				alert.SetMessage("\"Improve productivity using a timer-assisted work-and-rest cycle, and track it on your lifetime productivity graph.\"");

				LinearLayout linear = new LinearLayout(this) { Orientation = Orientation.Vertical };
				var text = linear.AddChild(new TextView(this));
				text.Text = @"
Author: Stephen Wicklund (Venryx)

This is an open source project, under the GPLv2 license.
The source code is available for anyone to view and modify.
Link: http://github.com/Venryx/Productivity-Tracker".Trim();
                text.SetPadding(30, 30, 30, 30);
				alert.SetView(linear);

				alert.SetPositiveButton("Ok", (sender, e)=>{});
				alert.Show();
			}
			return base.OnOptionsItemSelected(item);
		}

		public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
		{
			var usedKey = false;
			foreach (Hotkey hotkey in mainData.settings.hotkeys)
				if ((keyCode == Keycode.VolumeDown && hotkey.key == VKey.VolumeDown) || (keyCode == Keycode.VolumeUp && hotkey.key == VKey.VolumeUp))
				{
					usedKey = true; // consider key used, even if the action is "None" (so, e.g. if user set hotkey for volume-up, they can also absorb volume-down key presses with "None" action hotkey)
					if (hotkey.action == HotkeyAction.StartTimer_Rest || hotkey.action == HotkeyAction.StartTimer_Work)
						StartTimer(hotkey.action == HotkeyAction.StartTimer_Rest ? TimerType.Rest : TimerType.Work, hotkey.action_startTimer_length);
				}

			if (usedKey)
				return true;
			return base.OnKeyDown(keyCode, e);
		}
		/*public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
		{
			return base.OnKeyUp(keyCode, e);
		}*/
	}
}