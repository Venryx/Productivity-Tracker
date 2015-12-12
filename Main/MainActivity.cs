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
using Math = System.Math;

namespace Main
{
	[VDFType(propIncludeRegexL1: "", popOutL1: true)] public class MainData
	{
		[VDFPreDeserialize] public MainData() {} // makes-so in-code defaults are used, for props that aren't set in the VDF

		public Settings settings = new Settings();

		public bool currentTimerExists;
		public long currentTimer_lastResumeTime = -1;
		public int currentTimer_timeLeftAtLastPause;
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
		//public bool addCustomButton;
		//public bool addAlarmButton;

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

		public MainData data = new MainData();
		public void LoadData()
		{
			var file = new FileInfo("/storage/sdcard0/Productivity Tracker/MainData.vdf");
			if (file.Exists)
			{
				var vdf = File.ReadAllText(file.FullName);
				data = VDF.Deserialize<MainData>(vdf);
			}
		}
		public void SaveData()
		{
			var file = new FileInfo("/storage/sdcard0/Productivity Tracker/MainData.vdf").CreateFolders();
			var vdf = VDF.Serialize<MainData>(data);
			File.WriteAllText(file.FullName, vdf);
		}

		//const int secondsPerMinute = 60;
		const int secondsPerMinute = 1; // for testing

		ImageView timeLeftBar;
		ImageView timeOverBar;
		//TextView countdownLabel;
		Button countdownLabel;
		protected override void OnCreate(Bundle bundle)
		{
			main = this;
			base.OnCreate(bundle);
			SetContentView(Resource.Layout.Main);

			LoadData();

			timeLeftBar = FindViewById<ImageView>(Resource.Id.TimeLeftBar);
			timeOverBar = FindViewById<ImageView>(Resource.Id.TimeOverBar);

			// has to start with something
			timeLeftBar.Background = Drawables.clip_yPlus_blue_dark;
			timeOverBar.Background = Drawables.clip_xPlus_blue_dark;

			//var rootGroup = (ViewGroup)Window.DecorView.RootView;
			//var root = (LinearLayout)rootGroup.GetChildAt(0);
			var rootHolder = FindViewById<FrameLayout>(Android.Resource.Id.Content);
			var root = (LinearLayout)rootHolder.GetChildAt(0);

			var overlayHolder = rootHolder.AddChild(new FrameLayout(this), new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
			//countdownLabel = overlayHolder.AddChild(new TextView(this) {TextSize = 30}, new FrameLayout.LayoutParams(200, 100) {Gravity = GravityFlags.Left | GravityFlags.Top});
			//countdownLabel.SetSingleLine(true);
			//countdownLabel = overlayHolder.AddChild(new Button(this) {TextSize = 30}, new FrameLayout.LayoutParams(200, 100) {Gravity = GravityFlags.NoGravity});
			countdownLabel = overlayHolder.AddChild(new Button(this) {TextSize = 30, Visibility = ViewStates.Gone}, new FrameLayout.LayoutParams(230, 110));
			countdownLabel.SetPadding(0, 0, 0, 0);
			countdownLabel.Text = "10:10";

			/*var timeLeftPanel = FindViewById<FrameLayout>(Resource.Id.TimeLeftPanel);
			{
				var timeLeftBar_clip = (ClipDrawable)timeLeftBar.Background;
				timeLeftBar_clip.SetLevel((int)(10000 * .5));
			}
			var timeOverPanel = FindViewById<FrameLayout>(Resource.Id.TimeOverPanel);
			{
				/*var soundIconButton = timeOverPanel.AddChild(new ImageButton(this), new FrameLayout.LayoutParams(30, 30) {Gravity = GravityFlags.CenterVertical});
				soundIconButton.SetBackgroundResource(Resource.Drawable.Volume);*#/
			}*/

			FindViewById<Button>(Resource.Id.Pause).Click += delegate
			{
				if (data.currentTimer_paused)
					ResumeTimer();
				else
					PauseTimer();
			};
			FindViewById<Button>(Resource.Id.Stop).Click += delegate { StopTimer(); };

			FindViewById<LinearLayout>(Resource.Id.RestButtons).AddChild(new TextView(this) {Gravity = GravityFlags.CenterHorizontal, Text = "Rest"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			FindViewById<LinearLayout>(Resource.Id.WorkButtons).AddChild(new TextView(this) {Gravity = GravityFlags.CenterHorizontal, Text = "Work"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

			RefreshKeepScreenOn();
			RefreshTimerStepButtons();
			RefreshDynamicUI();
			// if current-timer should be running, make sure its running by pausing-and-resuming (scheduled alarm awakening might have been lost on device reboot)
			// maybe make-so: current-timer's scheduled awakening is rescheduled on device startup as well
			if (data.currentTimerExists && !data.currentTimer_paused)
			{
				PauseTimer();
				ResumeTimer();
			}
		}
		public void RefreshKeepScreenOn()
		{
			if (data.settings.keepScreenOnWhileOpen)
				Window.AddFlags(WindowManagerFlags.KeepScreenOn);
			else
				Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
		}
		public void RefreshTimerStepButtons()
		{
			var restButtonsPanel = FindViewById<LinearLayout>(Resource.Id.RestButtons);
			while (restButtonsPanel.ChildCount > 1)
				restButtonsPanel.RemoveViewAt(1);
			for (var i = 0; i < data.settings.numberOfTimerSteps; i++)
			{
				var timerStepButton = restButtonsPanel.AddChild(new Button(this), new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .75f) { Gravity = GravityFlags.CenterVertical }, 1);
				var timerStepLength = data.settings.timeIncrementForTimerSteps * i;
				timerStepButton.Text = timerStepLength.ToString();
				timerStepButton.Click += (sender, e)=>{ StartTimer(TimerType.Rest, timerStepLength); };
			}

			var workButtonsPanel = FindViewById<LinearLayout>(Resource.Id.WorkButtons);
			while (workButtonsPanel.ChildCount > 1)
				workButtonsPanel.RemoveViewAt(1);
			for (var i = 0; i < data.settings.numberOfTimerSteps; i++)
			{
				var timerStepButton = workButtonsPanel.AddChild(new Button(this), new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .75f) {Gravity = GravityFlags.CenterVertical}, 1);
				var timerStepLength = data.settings.timeIncrementForTimerSteps * i;
				timerStepButton.Text = timerStepLength.ToString();
				timerStepButton.Click += (sender, e)=> { StartTimer(TimerType.Work, timerStepLength); };
			}
		}
		protected override void OnPause()
		{
			base.OnPause();
			if (refreshDynamicUITimer != null && refreshDynamicUITimer.Enabled)
				refreshDynamicUITimer.Enabled = false;
			SaveData();
		}
		protected override void OnResume()
		{
			base.OnResume();
			if (data.currentTimer_lastResumeTime != -1)
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
			if (refreshDynamicUITimer == null)
			{
				refreshDynamicUITimer = new Timer(1000);
				//refreshDynamicUITimer.Elapsed += delegate { RefreshDynamicUI(); };
				//refreshDynamicUITimer.Elapsed += delegate { new Handler().Post(RefreshDynamicUI); };
				refreshDynamicUITimer.Elapsed += delegate { RunOnUiThread(RefreshDynamicUI); };
			}
			refreshDynamicUITimer.Enabled = true;
		}
		void RefreshDynamicUI()
		{
			FindViewById<Button>(Resource.Id.Stop).Enabled = data.currentTimerExists;
			FindViewById<Button>(Resource.Id.Pause).Enabled = data.currentTimerExists;
			FindViewById<Button>(Resource.Id.Pause).Text = data.currentTimerExists && data.currentTimer_paused ? "Resume" : "Pause";
			countdownLabel.Enabled = !data.currentTimer_paused;
			
			var timeLeftBar_clip = (ClipDrawable)timeLeftBar.Background;
			//var timeLeftBar_clipPercent = (double)timeLeftBar_clip.Level / 10000;
            var timeOverBar_clip = (ClipDrawable)timeOverBar.Background;
			//var timeOverBar_clipPercent = (double)timeOverBar_clip.Level / 10000;

			if (data.currentTimerExists)
			{
				//if (!data.currentTimer_paused)
				var timeLeft = data.currentTimer_paused ? data.currentTimer_timeLeftAtLastPause : data.currentTimer_timeLeftAtLastPause - (int)(JavaSystem.CurrentTimeMillis() - data.currentTimer_lastResumeTime);
				var timeOver = -timeLeft;

				var timeLeftForClipFull = (data.settings.numberOfTimerSteps * data.settings.timeIncrementForTimerSteps) * secondsPerMinute * 1000;
				var timeLeft_clipPercent = V.Clamp(0, 1, (double)timeLeft / timeLeftForClipFull);
				timeLeftBar_clip.SetLevel((int)(10000 * timeLeft_clipPercent));

				var timeOverForClipEmpty = data.settings.timeToMaxVolume * secondsPerMinute * 1000;
				var timeOver_clipPercent = V.Clamp(0, 1, 1 - ((double)timeOver / timeOverForClipEmpty));
				timeOverBar_clip.SetLevel((int)(10000 * timeOver_clipPercent));

				if (timeLeft >= 0) // if still in time-left panel
				{
					var timeLeftBar_center_x = timeLeftBar.GetPositionFrom().x + (timeLeftBar.Width / 2);
					var timeLeftBar_clipTop_y = timeLeftBar.GetPositionFrom().y + (int)(timeLeftBar.Height * (1 - timeLeft_clipPercent));
					var layoutParams = (FrameLayout.LayoutParams)countdownLabel.LayoutParameters;
                    layoutParams.LeftMargin = timeLeftBar_center_x - (countdownLabel.Width / 2);
					layoutParams.TopMargin = timeLeftBar_clipTop_y - countdownLabel.Height;
					countdownLabel.LayoutParameters = layoutParams;
				}
				else
				{
					var timeOverBar_clipRight_x = timeOverBar.GetPositionFrom().x + (int)(timeOverBar.Width * timeOver_clipPercent);
					var timeOverBar_center_y = timeOverBar.GetPositionFrom().y + (timeOverBar.Height / 2);
					var layoutParams = (FrameLayout.LayoutParams)countdownLabel.LayoutParameters;
					layoutParams.LeftMargin = timeOverBar_clipRight_x;
					layoutParams.TopMargin = timeOverBar_center_y - (countdownLabel.Height / 2);
					countdownLabel.LayoutParameters = layoutParams;
				}
				var timeLeft_inSeconds = (int)Math.Round(timeLeft / 1000d);
                var minutesLeft = timeLeft_inSeconds / secondsPerMinute;
				var secondsLeft = timeLeft_inSeconds % secondsPerMinute;
				countdownLabel.Text = minutesLeft + ":" + secondsLeft.ToString("D2");
				countdownLabel.Visibility = ViewStates.Visible;
			}
			else // if stopped
			{
				timeLeftBar_clip.SetLevel((int)(10000 * 0));
				timeOverBar_clip.SetLevel((int)(10000 * 0));
				countdownLabel.Visibility = ViewStates.Gone;
			}
		}

		void StartTimer(TimerType type, int minutes)
		{
			if (data.currentTimerExists)
				StopTimer();
			data.currentTimerExists = true;
            data.currentTimer_lastResumeTime = JavaSystem.CurrentTimeMillis();
			data.currentTimer_timeLeftAtLastPause = minutes * secondsPerMinute * 1000;
            data.currentTimer_type = type;

			//data.currentTimer_paused = false;
			//RefreshDynamicUI();
			//StartRefreshDynamicUITimer();
			//((AlarmManager)GetSystemService(AlarmService)).Set(AlarmType.RtcWakeup, data.currentTimer_lastResumeTime + data.currentTimer_timeLeft, GetLaunchUpdateServicePendingIntent());

			ResumeTimer();
		}
		void PauseTimer()
		{
			var timePassedFromLastTimerResume = (int)(JavaSystem.CurrentTimeMillis() - data.currentTimer_lastResumeTime);
            data.currentTimer_timeLeftAtLastPause -= timePassedFromLastTimerResume;
            data.currentTimer_paused = true;

			timeLeftBar.Background = data.currentTimer_type == TimerType.Rest ? Drawables.clip_yPlus_blue_dark : Drawables.clip_yPlus_green_dark;
			timeOverBar.Background = data.currentTimer_type == TimerType.Rest ? Drawables.clip_xPlus_blue_dark : Drawables.clip_xPlus_green_dark;
			RefreshDynamicUI();

			((AlarmManager)GetSystemService(AlarmService)).Cancel(GetLaunchUpdateServicePendingIntent());
		}
		void ResumeTimer()
		{
			data.currentTimer_paused = false;
			data.currentTimer_lastResumeTime = JavaSystem.CurrentTimeMillis();

			timeLeftBar.Background = data.currentTimer_type == TimerType.Rest ? Drawables.clip_yPlus_blue : Drawables.clip_yPlus_green;
			timeOverBar.Background = data.currentTimer_type == TimerType.Rest ? Drawables.clip_xPlus_blue : Drawables.clip_xPlus_green;
			RefreshDynamicUI();
			StartRefreshDynamicUITimer();

			((AlarmManager)GetSystemService(AlarmService)).Set(AlarmType.RtcWakeup, data.currentTimer_lastResumeTime + data.currentTimer_timeLeftAtLastPause, GetLaunchUpdateServicePendingIntent());
		}
		void StopTimer()
		{
			data.currentTimerExists = false;

			refreshDynamicUITimer.Enabled = false;
			RefreshDynamicUI();

			((AlarmManager)GetSystemService(AlarmService)).Cancel(GetLaunchUpdateServicePendingIntent());
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
The source code is available to view and modify.
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
			foreach (Hotkey hotkey in data.settings.hotkeys)
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