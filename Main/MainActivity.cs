using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Java.IO;

using File = System.IO.File;

namespace Main
{
	// maybe todo: make-so defaults are in a packaged VDF file/text-block, rather than being set here in the class
	[VDFType(propIncludeRegexL1: "", popOutL1: true)] public class Settings
	{
		public string alarmSoundFilePath;
		public int minVolume;
		public int maxVolume = 50;
		public int timeToMaxVolume = 10;
		public int numberOfTimerSteps = 11;
		public int timeIncrementForTimerSteps = 10;

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

		public Settings settings = new Settings();

		public void LoadSettings()
		{
			//var settingsFile = new File("/storage/sdcard0/Productivity Tracker/Settings.vdf");
			var settingsFile = new FileInfo("/storage/sdcard0/Productivity Tracker/Settings.vdf");
			if (settingsFile.Exists)
			{
				var settingsVDF = File.ReadAllText(settingsFile.FullName);
				settings = VDF.Deserialize<Settings>(settingsVDF);
			}
		}
		public void SaveSettings()
		{
			var settingsFile = new FileInfo("/storage/sdcard0/Productivity Tracker/Settings.vdf").CreateFolders();
			var settingsVDF = VDF.Serialize<Settings>(settings);
			File.WriteAllText(settingsFile.FullName, settingsVDF);
		}

		int count;
		protected override void OnCreate(Bundle bundle)
		{
			main = this;
			base.OnCreate(bundle);
			SetContentView(Resource.Layout.Main);

			LoadSettings();

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

			RefreshTimerStepButtons();
		}
		public void RefreshTimerStepButtons()
		{
			var restButtonsPanel = FindViewById<LinearLayout>(Resource.Id.RestButtons);
			while (restButtonsPanel.ChildCount > 1)
				restButtonsPanel.RemoveViewAt(1);
			for (var i = 0; i < settings.numberOfTimerSteps; i++)
			{
				var timerStepButton = restButtonsPanel.AddChild(new Button(this), new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .75f) { Gravity = GravityFlags.CenterVertical });
				timerStepButton.Text = ((settings.numberOfTimerSteps - (i + 1)) * settings.timeIncrementForTimerSteps).ToString();
				var timerStepLength = settings.timeIncrementForTimerSteps * i;
                timerStepButton.Click += (sender, e)=>{ StartTimer(TimerType.Rest, timerStepLength); };
			}

			var workButtonsPanel = FindViewById<LinearLayout>(Resource.Id.WorkButtons);
			while (workButtonsPanel.ChildCount > 1)
				workButtonsPanel.RemoveViewAt(1);
			for (var i = 0; i < settings.numberOfTimerSteps; i++)
			{
				var timerStepButton = workButtonsPanel.AddChild(new Button(this), new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .75f) { Gravity = GravityFlags.CenterVertical });
				timerStepButton.Text = ((settings.numberOfTimerSteps - (i + 1)) * settings.timeIncrementForTimerSteps).ToString();
				var timerStepLength = settings.timeIncrementForTimerSteps * i;
				timerStepButton.Click += (sender, e)=>{ StartTimer(TimerType.Rest, timerStepLength); };
			}
		}
		protected override void OnResume()
		{
			base.OnResume();
			
			RefreshTimerStepButtons();
		}

		void StartTimer(TimerType type, int length)
		{
			// todo
		}
		void PauseTimer()
		{
			// todo
		}
		void StopTimer()
		{
			// todo
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
			foreach (Hotkey hotkey in settings.hotkeys)
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