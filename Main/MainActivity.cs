using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Android.App;
using Android.App.Admin;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Hardware.Display;
using Android.Media;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Text;
using Java.IO;
using Java.Lang;
using Java.Util;
using Math = System.Math;
using File = System.IO.File;
using Timer = System.Timers.Timer;
using Orientation = Android.Widget.Orientation;
using Stream = Android.Media.Stream;

namespace Main
{
	[Activity(Label = "Productivity Tracker", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		public static MainActivity main;
		static MainActivity() { VDFExtensions.Init(); }

		DirectoryInfo RootFolder=>new DirectoryInfo("/storage/sdcard0/Productivity Tracker/");
		public MainData mainData = new MainData();
		public void LoadMainData()
		{
			var file = RootFolder.GetFile("MainData.vdf");
			if (file.Exists)
			{
				var vdf = File.ReadAllText(file.FullName);
				mainData = VDF.Deserialize<MainData>(vdf);
			}
		}
		public void SaveMainData()
		{
			var file = RootFolder.GetFile("MainData.vdf").CreateFolders();
			var vdf = VDF.Serialize<MainData>(mainData);
			File.WriteAllText(file.FullName, vdf);
		}
		List<Day> days = new List<Day>();
		void LoadDays(int daysBack)
		{
			var today = DateTime.UtcNow.Date;
			for (var i = 0; i < daysBack; i++)
				LoadDay(today.AddDays(-i));
		}
		void LoadDay(DateTime date)
		{
			var file = RootFolder.GetFolder("Days").GetFile(date.ToString_U_Date() + ".vdf");
			if (!file.Exists)
				return;
			var vdf = File.ReadAllText(file.FullName);
			var day = VDF.Deserialize<Day>(vdf);
			for (var i = 0; i <= days.Count; i++)
			{
				// if there's still a day left to check
				if (i < days.Count)
				{
					// if we've reached a day that we're not later than, insert self before that day
					if (day.date <= days[i].date)
					{
						days.Insert(i, day);
						break;
					}
				}
				else // we must be later than all the other days
				{
					days.Add(day);
					break;
				}
			}
		}
		void SaveDaysNeg1And0() // save yesterday's and today's data
		{
			SaveDay(CurrentDay); // run first, so new day created if just passed midnight
			if (days.Count >= 2)
				SaveDay(days[days.Count - 2]);
		}
		void SaveDay(Day day)
		{
			var vdf = VDF.Serialize<Day>(day);
			var file = RootFolder.GetFolder("Days").GetFile(day.date.ToString_U_Date() + ".vdf").CreateFolders();
			File.WriteAllText(file.FullName, vdf);
		}
		Day CurrentDay
		{
			get
			{
				var today = DateTime.UtcNow.Date;
                if (days.Count == 0 || today > days.Last().date)
					days.Add(new Day(today));
				return days.Last();
			}
		}
		Session CurrentSession
		{
			get
			{
				var day0 = CurrentDay; // run first, so new day created if just passed midnight
				var dayNeg1 = days.Count >= 2 ? days[days.Count - 2] : null;
				if (day0.sessions.Count > 0)
					return !day0.sessions.Last().timeStopped.HasValue ? day0.sessions.Last() : null;
				return dayNeg1 != null && dayNeg1.sessions.Count >= 1 && !dayNeg1.sessions.Last().timeStopped.HasValue ? dayNeg1.sessions.Last() : null;
			}
		}

		int SecondsPerMinute=>mainData.settings.fastMode ? 1 : 60;

		public Typeface baseTypeface;
		ImageView timeLeftBar;
		ImageView timeOverBar;
		//TextView countdownLabel;
		Button countdownLabel;
		protected override void OnCreate(Bundle bundle)
		{
			main = this;
			base.OnCreate(bundle);
			SetContentView(Resource.Layout.Main);
			
			LoadMainData();
			LoadDays(mainData.settings.daysVisibleAtOnce + 1);

			VolumeControlStream = Stream.Alarm;
			baseTypeface = new Button(this).Typeface;
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
			countdownLabel.Click += delegate
			{
				CurrentSession.locked = !CurrentSession.locked;
				UpdateDynamicUI();
				UpdateNotification();
			};

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
				if (CurrentSession.paused)
					ResumeSession();
				else
					PauseSession();
			};
			FindViewById<Button>(Resource.Id.Stop).Click += delegate { StopSession(); };

			FindViewById<LinearLayout>(Resource.Id.RestButtons).AddChild(new TextView(this) {Gravity = GravityFlags.CenterHorizontal, Text = "Rest"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			FindViewById<LinearLayout>(Resource.Id.WorkButtons).AddChild(new TextView(this) {Gravity = GravityFlags.CenterHorizontal, Text = "Work"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			
			UpdateKeepScreenOn();
			UpdateTimerStepButtons();
			UpdateProductivityGraph();
			UpdateDynamicUI();
			UpdateNotification();
			// if current-timer should be running, make sure its running by pausing-and-resuming (scheduled alarm awakening might have been lost by a device reboot)
			// maybe make-so: current-timer's scheduled awakening is rescheduled on device startup as well
			if (CurrentSession != null && !CurrentSession.paused)
			{
				PauseSession();
				ResumeSession();
			}
		}
		protected override void OnPause()
		{
			base.OnPause();
			//if (currentTimer != null)
			//	currentTimer.Enabled = false;
			SaveMainData();
			SaveDaysNeg1And0();
		}
		/*protected override void OnResume()
		{
			base.OnResume();
			//ProcessTimeUpToNowAndUpdateOutflow(); // update instantly (instead of waiting for next timer-tick)
			//if (currentTimer != null)
			//	currentTimer.Enabled = true;
		}*/
		/*protected override void OnStop()
		{
			base.OnStop();
			SaveMainData();
		}*/

		public void UpdateKeepScreenOn()
		{
			if (mainData.settings.keepScreenOnWhileOpen)
				Window.AddFlags(WindowManagerFlags.KeepScreenOn);
			else
				Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
		}
		public void UpdateTimerStepButtons()
		{
			var restButtonsPanel = FindViewById<LinearLayout>(Resource.Id.RestButtons);
			while (restButtonsPanel.ChildCount > 1)
				restButtonsPanel.RemoveViewAt(1);
			for (var i = 0; i < mainData.settings.numberOfTimerSteps; i++)
			{
				var timerStepButton = restButtonsPanel.AddChild(new Button(this), new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .75f) { Gravity = GravityFlags.CenterVertical }, 1);
				var timerStepLength = mainData.settings.timeIncrementForTimerSteps * i; // in minutes
				timerStepButton.Text = timerStepLength.ToString();
				timerStepButton.Click += (sender, e)=>{ StartSession("Rest", timerStepLength); };
			}

			var workButtonsPanel = FindViewById<LinearLayout>(Resource.Id.WorkButtons);
			while (workButtonsPanel.ChildCount > 1)
				workButtonsPanel.RemoveViewAt(1);
			for (var i = 0; i < mainData.settings.numberOfTimerSteps; i++)
			{
				var timerStepButton = workButtonsPanel.AddChild(new Button(this), new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .75f) {Gravity = GravityFlags.CenterVertical}, 1);
				var timerStepLength = mainData.settings.timeIncrementForTimerSteps * i; // in minutes
				timerStepButton.Text = timerStepLength.ToString();
				timerStepButton.Click += (sender, e)=> { StartSession("Work", timerStepLength); };
			}
		}
		void UpdateProductivityGraph()
		{
			// make-so
		}
		
		//PendingIntent GetLaunchUpdateServicePendingIntent()
		PendingIntent GetPendingIntent_LaunchMain()
		{
			/*var launchUpdateService = new Intent(this, typeof(UpdateService));
			launchUpdateService.AddFlags(ActivityFlags.SingleTop);
			return PendingIntent.GetService(this, 0, launchUpdateService, PendingIntentFlags.UpdateCurrent);*/

			var launchMain = new Intent(this, typeof(MainActivity));
			launchMain.AddFlags(ActivityFlags.SingleTop);
			return PendingIntent.GetService(this, 0, launchMain, PendingIntentFlags.UpdateCurrent);
        }

		void StartSession(string type, int minutes)
		{
			// data
			if (CurrentSession != null)
				StopSession();
			var session = new Session();
			session.type = type;
			session.timeStarted = DateTime.UtcNow;
			session.timeLeft = minutes * SecondsPerMinute;
			//session.currentTimer_processedTimeExtent = JavaSystem.CurrentTimeMillis();
			CurrentDay.sessions.Add(session);

			// actors
			ResumeSession();
		}
		// make-so: Pause and Resume methods add entries to the Session.timePauses list; break point
		void PauseSession()
		{
			// data
            CurrentSession.paused = true;
			timeLeftBar.Background = CurrentSession.type == "Rest" ? Drawables.clip_yPlus_blue_dark : Drawables.clip_yPlus_green_dark;
			timeOverBar.Background = CurrentSession.type == "Rest" ? Drawables.clip_xPlus_blue_dark : Drawables.clip_xPlus_green_dark;
			ProcessTimeUpToNow();

			// actors
			UpdateOutflow();
			if (currentTimer != null) // (for if called from OnCreate method)
				currentTimer.Enabled = false;
			((AlarmManager)GetSystemService(AlarmService)).Cancel(GetPendingIntent_LaunchMain());
		}
		void ResumeSession()
		{
			// data
			CurrentSession.processedTimeExtent = DateTime.UtcNow;
			CurrentSession.paused = false;
			timeLeftBar.Background = CurrentSession.type == "Rest" ? Drawables.clip_yPlus_blue : Drawables.clip_yPlus_green;
			timeOverBar.Background = CurrentSession.type == "Rest" ? Drawables.clip_xPlus_blue : Drawables.clip_xPlus_green;
			ProcessTimeUpToNow();

			// actors
			UpdateOutflow();
			if (currentTimer == null)
			{
				currentTimer = new Timer(1000);
				currentTimer.Elapsed += delegate
				{
					ProcessTimeUpToNow();
					UpdateOutflow();
				};
			}
			currentTimer.Enabled = true;
			if (CurrentSession.timeLeft > 0)
				((AlarmManager)GetSystemService(AlarmService)).Set(AlarmType.RtcWakeup, CurrentSession.processedTimeExtent.Ticks_Milliseconds() + (CurrentSession.timeLeft * 1000), GetPendingIntent_LaunchMain());
		}
		void StopSession()
		{
			// data
			//ProcessTimeUpToNow();
			CurrentSession.timeStopped = DateTime.UtcNow;

			// actors
			UpdateOutflow();
			currentTimer.Enabled = false;
			((AlarmManager)GetSystemService(AlarmService)).Cancel(GetPendingIntent_LaunchMain());
		}

		public Timer currentTimer;
		MediaPlayer alarmPlayer;
		void ProcessTimeUpToNow() { CurrentTimer_ProcessTimeUpToNow(); }
		
		void CurrentTimer_ProcessTimeUpToNow() // actually, processes time up to [now, rounded to nearest second]
		{
			//var timeToProcess = (int)(DateTime.UtcNow - CurrentSession.processedTimeExtent).TotalSeconds; // in seconds
			var timeToProcess = (int)Math.Round((DateTime.UtcNow - CurrentSession.processedTimeExtent).TotalSeconds); // in seconds

			if (!CurrentSession.paused && (!CurrentSession.locked || CurrentSession.timeLeft <= 0))
			{
				var timeToProcess_partBeforeTimeout = Math.Min(Math.Max(0, CurrentSession.timeLeft), timeToProcess);
				var timeToProcess_partAfterTimeout = timeToProcess - timeToProcess_partBeforeTimeout;

				CurrentSession.timeLeft -= timeToProcess;
				if (!CurrentSession.locked)
					CurrentSession.timeOver_withLocking += timeToProcess_partAfterTimeout;
			}

			//data.currentTimer_timeAtLastTick = currentTime;
			CurrentSession.processedTimeExtent = CurrentSession.processedTimeExtent.AddSeconds(timeToProcess);
		}
		void UpdateOutflow()
		{
			UpdateAudio();
			RunOnUiThread(UpdateDynamicUI);
			RunOnUiThread(UpdateNotification);
		}
		void UpdateAudio()
		{
			if (CurrentSession == null || CurrentSession.timeLeft > 0 || CurrentSession.paused)
			{
				if (alarmPlayer != null && alarmPlayer.IsPlaying)
				{
					alarmPlayer.Stop();
					alarmPlayer = null;  // if alarm-player was stopped, it's as good as null ™ (you'd have to call reset(), which makes it lose all its data anyway), so nullify it
				}
			}
			else
			{
				var timeOver_withLocking = CurrentSession.timeOver_withLocking; // in seconds
				var timeOverForClipEmpty = mainData.settings.timeToMaxVolume * SecondsPerMinute; // in seconds
				var percentThroughTimeOverBar = V.Clamp(0, 1, (double)timeOver_withLocking / timeOverForClipEmpty);

				if (alarmPlayer == null)
				{
					//alarmPlayer = MediaPlayer.Create(this, new FileInfo(data.settings.alarmSoundFilePath).ToFile().ToURI_Android());
					alarmPlayer = new MediaPlayer();
					alarmPlayer.SetAudioStreamType(Stream.Alarm);
					alarmPlayer.SetDataSource(this, new FileInfo(mainData.settings.alarmSoundFilePath).ToFile().ToURI_Android());
					alarmPlayer.Looping = true;
					//audioPlayer.SeekTo(timeOver_withLocking * 1000);
					//audioPlayer.SetWakeMode(this, WakeLockFlags.AcquireCausesWakeup);
					alarmPlayer.Prepare();
					alarmPlayer.Start();
				}

				var volume = V.Lerp(mainData.settings.minVolume / 100d, mainData.settings.maxVolume / 100d, percentThroughTimeOverBar);
                alarmPlayer.SetVolume((float)volume, (float)volume);
				//alarmPlayer.VSetVolume(V.Lerp(data.settings.minVolume / 100d, data.settings.maxVolume / 100d, percentThroughTimeOverBar), data.settings.volumeFadeType);
			}
		}
		void UpdateDynamicUI()
		{
			FindViewById<Button>(Resource.Id.Stop).Enabled = CurrentSession != null;
			FindViewById<Button>(Resource.Id.Pause).Enabled = CurrentSession != null;
			FindViewById<Button>(Resource.Id.Pause).Text = CurrentSession != null && CurrentSession.paused ? "Resume" : "Pause";
			countdownLabel.Enabled = CurrentSession != null && !CurrentSession.paused;
			
			var timeLeftBar_clip = (ClipDrawable)timeLeftBar.Background;
			//var timeLeftBar_clipPercent = (double)timeLeftBar_clip.Level / 10000;
            var timeOverBar_clip = (ClipDrawable)timeOverBar.Background;
			//var timeOverBar_clipPercent = (double)timeOverBar_clip.Level / 10000;

			if (CurrentSession != null)
			{
				//if (!data.currentTimer_paused)
				var timeLeft = CurrentSession.timeLeft; // in seconds
				//var timeOver = -timeLeft; // in seconds
				var timeOver_withLocking = CurrentSession.timeOver_withLocking; // in seconds

				var timeLeftForClipFull = mainData.settings.numberOfTimerSteps * mainData.settings.timeIncrementForTimerSteps * SecondsPerMinute; // in seconds
				var timeLeft_clipPercent = V.Clamp(0, 1, (double)timeLeft / timeLeftForClipFull);
				timeLeftBar_clip.SetLevel((int)(10000 * timeLeft_clipPercent));

				var timeOverForClipEmpty = mainData.settings.timeToMaxVolume * SecondsPerMinute; // in seconds
				var timeOver_clipPercent = V.Clamp(0, 1, 1 - ((double)timeOver_withLocking / timeOverForClipEmpty));
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
                var minutesLeft = Math.Abs(timeLeft / SecondsPerMinute);
				var secondsLeft = Math.Abs(timeLeft % SecondsPerMinute);
				countdownLabel.Text = (timeLeft < 0 ? "-" : "") + minutesLeft + ":" + secondsLeft.ToString("D2");
				/*var countdownText = minutesLeft + ":" + secondsLeft.ToString("D2");
				if (data.currentTimer_locked)
				{
					//countdownLabel.TextFormatted = Html.FromHtml("<u>" + countdownText + "</u>");
					countdownLabel.TextFormatted = new SpannedString(Html.FromHtml("<u>" + countdownText + "</u>"));
					//countdownLabel.TextFormatted = new SpannableString().AddSpan(new Span { Text = countdownText, FontAttributes = FontAttributes.Underlined }, 0, 1, SpanTypes.Composing);
				}
				else
					countdownLabel.Text = countdownText;*/
				
                countdownLabel.SetTypeface(baseTypeface, CurrentSession.locked ? TypefaceStyle.BoldItalic : TypefaceStyle.Normal);
				countdownLabel.Visibility = ViewStates.Visible;
			}
			else // if stopped
			{
				timeLeftBar_clip.SetLevel((int)(10000 * 0));
				timeOverBar_clip.SetLevel((int)(10000 * 0));
				countdownLabel.Visibility = ViewStates.Gone;
			}
		}
		Notification.Builder notificationBuilder;

		bool IsScreenOn()
		{
			var dm = (DisplayManager)GetSystemService(DisplayService);
			foreach (Display display in dm.GetDisplays())
				if (display.State != DisplayState.Off)
					return true;
			return false;
		}
		//PowerManager.WakeLock sleeper;
		/*void TurnScreenOff()
		{
			//var powerManager = GetSystemService(PowerService) as PowerManager;
			//powerManager.GoToSleep(SystemClock.UptimeMillis());
			/*if (sleeper == null)
				sleeper = powerManager.NewWakeLock(WakeLockFlags.ProximityScreenOff, "turn screen off");
			sleeper.Acquire(1000);*#/

			Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
			WindowManagerLayoutParams attributes = Window.Attributes;
			attributes.ScreenBrightness = 0;
			Window.Attributes = attributes;
			//UpdateKeepScreenOn();

			/*var policyManager = (DevicePolicyManager)GetSystemService(DevicePolicyService);
			policyManager.LockNow();*#/
		}*/
		public class BroadcastReceiverScreenListener : BroadcastReceiver
		{
			public BroadcastReceiverScreenListener(Action onScreenOff) { this.onScreenOff = onScreenOff; }
			Action onScreenOff;
			public override void OnReceive(Context arg0, Intent intent)
			{
				//if (intent.Action == Intent.ActionScreenOff)
				onScreenOff();
			}
		}
		public void TurnScreenOff()
		{
			// store old settings
			var oldStayOnWhilePluggedIn = Android.Provider.Settings.System.GetInt(ContentResolver, Android.Provider.Settings.System.ScreenOffTimeout, (int)BatteryManager.BatteryPluggedUsb);
			var oldTimeout = Android.Provider.Settings.System.GetInt(ContentResolver, Android.Provider.Settings.System.ScreenOffTimeout, 3000);

			// change settings temporarily
			Android.Provider.Settings.System.PutInt(ContentResolver, Android.Provider.Settings.System.StayOnWhilePluggedIn, 0);
			Android.Provider.Settings.System.PutInt(ContentResolver, Android.Provider.Settings.System.ScreenOffTimeout, 0);
			Window.ClearFlags(WindowManagerFlags.KeepScreenOn);

			BroadcastReceiver receiver = null;
			receiver = new BroadcastReceiverScreenListener(()=>
			{
				// restore old settings
				Android.Provider.Settings.System.PutInt(ContentResolver, Android.Provider.Settings.System.StayOnWhilePluggedIn, oldStayOnWhilePluggedIn);
				Android.Provider.Settings.System.PutInt(ContentResolver, Android.Provider.Settings.System.ScreenOffTimeout, oldTimeout);
				UpdateKeepScreenOn();
				UnregisterReceiver(receiver);
			});
			RegisterReceiver(receiver, new IntentFilter(Intent.ActionScreenOff));
		}
		PowerManager.WakeLock waker;
		void TurnScreenOn()
		{
			//Window.AddFlags(WindowManagerFlags.DismissKeyguard | WindowManagerFlags.ShowWhenLocked | WindowManagerFlags.TurnScreenOn);

			var powerManager = GetSystemService(PowerService) as PowerManager;
			//powerManager.WakeUp(SystemClock.UptimeMillis());
			if (waker == null)
				//waker = powerManager.NewWakeLock(WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.ScreenBright, "TimerWentOff");
				waker = powerManager.NewWakeLock(WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.ScreenDim, "turn screen on");
			waker.Acquire(1000);
		}

		void UpdateNotification()
		{
			var timeLeft = CurrentSession?.timeLeft ?? 0; // in seconds
			//var timeOver = -timeLeft; // in seconds
			//var timeOver_withLocking = data.currentTimer_timeOver_withLocking; // in seconds
			if (CurrentSession != null && timeLeft > 0)
			{
				var minutesLeft = Math.Abs(timeLeft / SecondsPerMinute);
				var secondsLeft = Math.Abs(timeLeft % SecondsPerMinute);
				var timeLeftText = (timeLeft < 0 ? "-" : "") + minutesLeft + ":" + secondsLeft.ToString("D2");

				if (notificationBuilder == null)
				{
					// the PendingIntent to launch MainActivity if the user selects this notification
					Intent launchMain = new Intent(this, typeof(MainActivity));
					launchMain.SetFlags(ActivityFlags.SingleTop);
					var launchMain_pending = PendingIntent.GetActivity(this, 0, launchMain, 0);

					//launchMain_pending.Send(Result.Ok);
					//StartActivity(launchMain);

					// set the icon, scrolling text and timestamp
					notificationBuilder = new Notification.Builder(this);
					notificationBuilder.SetContentTitle("Productivity tracker");
					//notificationBuilder.SetContentText("Timer running. Time left: " + timeLeftText);
					//notificationBuilder.SetSubText("[Extra info]");
					notificationBuilder.SetSmallIcon(Resource.Drawable.Icon);
					notificationBuilder.SetContentIntent(launchMain_pending);
					notificationBuilder.SetOngoing(true);
				}

				notificationBuilder.SetContentText($"{CurrentSession.type} timer {(CurrentSession.paused ? "paused" : "running")}. Time left: " + timeLeftText);
				var notification = notificationBuilder.Build();
				var notificationManager = (NotificationManager)GetSystemService(NotificationService);
				// we use a layout id because it is a unique number; we use it later to cancel
				notificationManager.Notify(Resource.Layout.Main, notification);
			}
			else // if stopped, or timer timed-out
			{
				var notificationManager = (NotificationManager)GetSystemService(NotificationService);
				notificationManager.Cancel(Resource.Layout.Main);
				if (timeLeft == 0) // if timer just timed-out
				{
					var launchMain = Intent;
					//Intent launchMain = new Intent(this, typeof(MainActivity));
					//launchMain.SetAction(Intent.ActionMain);
					//launchMain.AddCategory(Intent.CategoryLauncher);
					//launchMain.SetFlags(ActivityFlags.SingleTop);
					launchMain.SetFlags(ActivityFlags.ReorderToFront);
					StartActivity(launchMain);
					TurnScreenOn();
				}
			}
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

		public List<string> recentKeys_strings = new List<string>();
		public override bool OnKeyDown(Keycode key, KeyEvent e)
		{
			recentKeys_strings.Add(DateTime.Now.ToString("HH:mm:ss") + ": " + key);
			while (recentKeys_strings.Count > 30)
				recentKeys_strings.RemoveAt(0);

			var usedKey = false;
			foreach (Hotkey hotkey in mainData.settings.hotkeys)
				if (key == hotkey.key)
				{
					usedKey = true; // consider key used, even if the action is "None" (so, e.g. if user set hotkey for volume-up, they can also absorb volume-down key presses with "None" action hotkey)
					if (hotkey.action == HotkeyAction.StartSession_Rest || hotkey.action == HotkeyAction.StartSession_Work)
						StartSession(hotkey.action == HotkeyAction.StartSession_Rest ? "Rest" : "Work", hotkey.action_startTimer_length);
					else if (hotkey.action == HotkeyAction.PauseSession)
						PauseSession();
					else if (hotkey.action == HotkeyAction.ToggleSessionPaused)
					{
						if (CurrentSession != null)
							if (CurrentSession.paused)
								ResumeSession();
							else
								PauseSession();
					}
					else if (hotkey.action == HotkeyAction.StopSession)
						StopSession();
					else if (hotkey.action == HotkeyAction.TurnScreenOff)
						TurnScreenOff();
					else if (hotkey.action == HotkeyAction.TurnScreenOn)
						TurnScreenOn();
					else if (hotkey.action == HotkeyAction.ToggleScreenOn)
					{
						if (IsScreenOn())
							TurnScreenOff();
						else
							TurnScreenOn();
					}
				}

			if (usedKey)
				return true;
			return base.OnKeyDown(key, e);
		}
		public void ShowRecentKeys(Context context)
		{
			AlertDialog.Builder alert = new AlertDialog.Builder(context);
			alert.SetTitle("Recent keys pressed");

			LinearLayout linear = new LinearLayout(this) { Orientation = Orientation.Vertical };
			var text = linear.AddChild(new TextView(this));
			text.Text = recentKeys_strings.JoinUsing("\n");
			text.SetPadding(30, 30, 30, 30);
			alert.SetView(linear);

			alert.SetPositiveButton("Ok", (sender, e) => { });
			alert.Show();
		}
		/*public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
		{
			return base.OnKeyUp(keyCode, e);
		}*/
	}
}