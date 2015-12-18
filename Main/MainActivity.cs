using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using android.support.percent;
using Android.App;
using Android.App.Admin;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.Hardware.Display;
using Android.Media;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Text;
using Android.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
/*
using Math = System.Math;
using File = System.IO.File;
using Timer = System.Timers.Timer;
*/
using Exception = System.Exception;
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
			for (var i = -daysBack; i <= 0; i++)
				LoadDay(today.AddDays(i));
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
				if (day0.sessions.Count > 0)
					return !day0.sessions.Last().timeStopped.HasValue ? day0.sessions.Last() : null;
				var dayNeg1 = days.Count >= 2 ? days[days.Count - 2] : null;
				return dayNeg1 != null && dayNeg1.sessions.Count >= 1 && !dayNeg1.sessions.Last().timeStopped.HasValue ? dayNeg1.sessions.Last() : null;
			}
		}

		int SecondsPerMinute=>mainData.settings.fastMode ? 1 : 60;

		public Typeface baseTypeface;
		FrameLayout graphRoot;
		LinearLayout graph_nonOverlayRoot;
		PercentRelativeLayout daysPanel;
		PercentRelativeLayout graphBottomBar;
		ImageView currentTimeMarker;
		PercentRelativeLayout graph_overlayRoot;
		ImageView timeLeftBar;
		ImageView timeOverBar;
		//TextView countdownLabel;
		Button countdownLabel;
		FrameLayout mouseInputBlocker;
		public Timer dayUpdateTimer;
		protected override void OnCreate(Bundle bundle)
		{
			main = this;
			base.OnCreate(bundle);
			SetContentView(Resource.Layout.Main);
			
			LoadMainData();
			LoadDays(mainData.settings.daysVisibleAtOnce);

			VolumeControlStream = Stream.Alarm;
			baseTypeface = new Button(this).Typeface;
			graphRoot = FindViewById<FrameLayout>(Resource.Id.GraphRoot);
			timeLeftBar = FindViewById<ImageView>(Resource.Id.TimeLeftBar);
			timeOverBar = FindViewById<ImageView>(Resource.Id.TimeOverBar);

			// has to start with something
			timeLeftBar.Background = Drawables.clip_yPlus_blue_dark;
			timeOverBar.Background = Drawables.clip_xPlus_blue_dark;

			// productivity graph
			// ==========

			graph_nonOverlayRoot = graphRoot.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
			{
				daysPanel = graph_nonOverlayRoot.AddChild(new PercentRelativeLayout(this), new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1));
				{
					AddDaysToProductivityGraph(mainData.settings.daysVisibleAtOnce - 1);
				}
				graphBottomBar = graph_nonOverlayRoot.AddChild(new PercentRelativeLayout(this), new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 30));
				{
					//currentTimeMarker = graphBottomBar.AddChild(new ImageView(this), new PercentRelativeLayout.LayoutParams(V.WrapContent, V.WrapContent));
					currentTimeMarker = graphBottomBar.AddChild(new ImageView(this), new PercentRelativeLayout.LayoutParams(80, 48));
					//currentTimeMarker.SetPadding(-40, 0, 0, 0);
					currentTimeMarker.SetImageResource(Resource.Drawable.UpArrow);
					UpdateCurrentTimerMarkerPosition();
					//V.WaitXSecondsThenRun(.1, ()=>RunOnUiThread(UpdateCurrentTimerMarkerPosition));

					UpdateHourMarkers();
				}
			}
			graph_overlayRoot = graphRoot.AddChild(new PercentRelativeLayout(this), new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
			{
				GenerateProductivityGraphOverlay();
			}

			// general
			// ==========

			UpdateKeepScreenOn();

			//var rootHolderGroup = (ViewGroup)Window.DecorView.RootView;
			var rootHolder = FindViewById<FrameLayout>(Android.Resource.Id.Content);
			var root = (LinearLayout)rootHolder.GetChildAt(0);

			var overlayHolder = rootHolder.AddChild(new FrameLayout(this), new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
			var focusTaker = overlayHolder.AddChild(new Button(this), new FrameLayout.LayoutParams(0, 0) {LeftMargin = -1000}); // as first button, takes focus, so gamepad A/shutter button can't click other things
			countdownLabel = overlayHolder.AddChild(new Button(this) {TextSize = 30, Visibility = ViewStates.Gone}, new FrameLayout.LayoutParams(230, 110));
			countdownLabel.SetPadding(0, 0, 0, 0);
			countdownLabel.Text = "10:10";
			countdownLabel.Click += delegate
			{
				CurrentSession.locked = !CurrentSession.locked;
				UpdateDynamicUI();
				UpdateNotification();
			};
			//mouseInputBlocker = overlayHolder.AddChild(new FrameLayout(this) {Visibility = ViewStates.Gone}, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
			//mouseInputBlocker.SetZ(10);
			mouseInputBlocker = rootHolder.AddChild(new FrameLayout(this) {Visibility = ViewStates.Gone}, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
			var mouseInputBlockerMessageLabel = mouseInputBlocker.AddChild(new TextView(this), new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) {Gravity = GravityFlags.Center});
			mouseInputBlockerMessageLabel.Text = "Mouse input is currently blocked. Press the screen with two fingers simultaneously to unblock.";

			// time-left bar
			// ==========

			FindViewById<LinearLayout>(Resource.Id.RestButtons).AddChild(new TextView(this) { Gravity = GravityFlags.CenterHorizontal, Text = "Rest" }, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			FindViewById<LinearLayout>(Resource.Id.WorkButtons).AddChild(new TextView(this) { Gravity = GravityFlags.CenterHorizontal, Text = "Work" }, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

			UpdateTimerStepButtons();
			UpdateDynamicUI();

			// time-over bar
			// ==========

			FindViewById<Button>(Resource.Id.Pause).Click += delegate
			{
				if (CurrentSession.paused)
					ResumeSession();
				else
					PauseSession();
			};
			FindViewById<Button>(Resource.Id.Stop).Click += delegate { StopSession(); };

			// others
			// ==========

			
			UpdateNotification();
			// if current-timer should be running, make sure its running by pausing-and-resuming (scheduled alarm awakening might have been lost by a device reboot)
			// maybe make-so: current-timer's scheduled awakening is rescheduled on device startup as well
			if (CurrentSession != null && !CurrentSession.paused)
			{
				PauseSession(false);
				ResumeSession(false);
			}

			dayUpdateTimer = new Timer(mainData.settings.fastMode ? 1000 : 60000);
			dayUpdateTimer.Elapsed += delegate
			{
				RunOnUiThread(()=>
				{
					UpdateDayBox(CurrentDay);
					UpdateCurrentTimerMarkerPosition();
				});
			};
			dayUpdateTimer.Enabled = true;
		}
		protected override void OnDestroy()
		{
			//currentTimer?.Stop();
			//currentTimer?.Elapsed -= CurrentTimer_Elapsed;
			sessionUpdateTimer?.Dispose();
			sessionUpdateTimer = null;
			base.OnDestroy();
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
		public override void OnWindowFocusChanged(bool hasFocus)
		{
			if (hasFocus) // ui should be laid-out at this point
				UpdateCurrentTimerMarkerPosition();
		}

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

		// productivity graph
		// ==========

		void GenerateProductivityGraphOverlay()
		{
			for (var i = 0; i < 24; i++)
			{
				var hourMarker = graph_overlayRoot.AddChild(new ImageView(this), new PercentRelativeLayout.LayoutParams(1, V.MatchParent));
				hourMarker.Background = Drawables.CreateColor(new Color(255, 255, 255, 50));
				var layoutParams = hourMarker.LayoutParameters as PercentRelativeLayout.LayoutParams;
				layoutParams.PercentLayoutInfo.leftMarginPercent = (float)(i / 24d);
				hourMarker.LayoutParameters = layoutParams;
			}
		}
		void AddDaysToProductivityGraph(int daysBack)
		{
			var today = DateTime.UtcNow.Date;
			//for (var i = 0; i <= daysBack; i++)
			for (var i = -daysBack; i <= 0; i++)
			{
				var date = today.AddDays(i);
				// go back into 'days' list enough that we know we'll find the day's Day object, if it exists
				Day dayObj = null;
				for (var i2 = days.Count - 1; i2 >= days.Count - 1 - daysBack && i2 >= 0; i2--)
					if (days[i2].date == date)
						dayObj = days[i2];
				AddDayToProductivityGraph(dayObj);
			}
		}
		void AddDayToProductivityGraph(Day day)
		{
			var lastOldDayBox = daysPanel.ChildCount >= 1 ? daysPanel.GetChildAt(daysPanel.ChildCount - 1) : null;

			var dayBox = CreateDayBox(day);
			if (lastOldDayBox != null)
			{
				var layoutParams = dayBox.LayoutParameters as PercentRelativeLayout.LayoutParams;
				layoutParams.AddRule(LayoutRules.Below, lastOldDayBox.Id);
				dayBox.LayoutParameters = layoutParams;
			}
			daysPanel.AddChild(dayBox);
			if (day != null)
				day.box = dayBox;
		}
		//int lastViewAutoID = -1;
		int lastViewAutoID = 1000;
		PercentRelativeLayout CreateDayBox(Day day)
		{
			var result = new PercentRelativeLayout(this);
			result.Id = ++lastViewAutoID;
            var layoutParams = new PercentRelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
			layoutParams.PercentLayoutInfo.heightPercent = (float)(1d / mainData.settings.daysVisibleAtOnce);
			result.LayoutParameters = layoutParams;

			var rect = new RectShape();
			var shape = new ShapeDrawable(rect);
			shape.Paint.Color = new Color(255, 255, 255, 128);
			shape.Paint.SetStyle(Paint.Style.Stroke);
			shape.Paint.StrokeWidth = 1;
			//result.Background = new InsetDrawable(shape, -1, -1, -1, 0);
			result.Background = shape;
			result.SetPadding(1, 1, 1, 1);

			if (day != null) // if there's data stored for this day
			{
				const int minutesInDay = 60 * 24;
				Session previousDayOverflowSession = null;
				if (days.Count >= 2 && days[days.Count - 2].date == day.date.AddDays(-1).Date && days[days.Count - 2].sessions.Count >= 1)
				{
					var previousDayLastSession = days[days.Count - 2].sessions.Last();
					if (!previousDayLastSession.timeStopped.HasValue || previousDayLastSession.timeStopped.Value >= day.date)
						previousDayOverflowSession = previousDayLastSession;
				}

				// maybe make-so: there's a better-matching setting for adding fake data like this
				if (mainData.settings.fastMode)
				{
					var session = new Session("Work", day.date.AddMinutes(-10), 19) {timeStopped = day.date.AddMinutes(1)};
					var subsession = new Subsession(session.timeStarted) {timeStopped = session.timeStopped};
					session.subsessions.Add(subsession);
					previousDayOverflowSession = session;
				}

				//var minutesInDay = 60 * 24;
				Session lastSubsessionSession = null;
				//Subsession lastSubsession = null;
				ImageView lastSubsessionView = null;
				DateTime lastSubsessionEndTime = day.date.Date;

				var sessions = day.sessions.ToList();
				if (previousDayOverflowSession != null) // if session from previous day overflowed into current, add fake session for it, so part in this day shows up
				{
					var session = previousDayOverflowSession.Clone();
					session.timeStarted = day.date;
					foreach (Subsession subsession in session.subsessions)
					{
						if (subsession.timeStarted < session.timeStarted)
							subsession.timeStarted = session.timeStarted;
						if (subsession.timeStopped < session.timeStarted)
							subsession.timeStopped = session.timeStarted;
					}
					sessions.Insert(0, session);
				}
				foreach (Session session in sessions)
				{
					var subsessions = session.subsessions.ToList();
					//if (lastSubsession != null && (!lastSubsession.timeStopped.HasValue || lastSubsession.timeStopped >= day.Date)
					if (session.paused) // if session paused, add fake subsession after, so pause gap-segment shows up
						subsessions.Add(session.timeStopped.HasValue ? new Subsession(session.timeStopped.Value) {timeStopped = session.timeStopped} : new Subsession(DateTime.UtcNow));
					foreach (Subsession subsession in subsessions)
					{
						if (subsession.timeStarted.Date > day.date) // if we've reached a subsession started after the current day (as part of overflow-session)
							break;

						var gap = new ImageView(this);
						gap.Id = ++lastViewAutoID;
						if (lastSubsessionSession == session)
							gap.Background = Drawables.CreateFill(new Color(session.type == "Rest" ? new Color(0, 0, 128) : new Color(0, 128, 0)));
						var gap_layout = new PercentRelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
						var gapTime = subsession.timeStarted - lastSubsessionEndTime;
						gap_layout.PercentLayoutInfo.widthPercent = (float)(gapTime.TotalMinutes / minutesInDay);
						gap_layout.PercentLayoutInfo.heightPercent = .1f;
						if (lastSubsessionView != null)
						{
							gap_layout.AddRule(LayoutRules.RightOf, lastSubsessionView.Id);
							gap_layout.AddRule(LayoutRules.AlignParentBottom);
						}
						if (mainData.settings.fastMode && lastSubsessionSession == session) // if fast mode (and pause-type gap), exhaggerate view size to 60x
							gap_layout.PercentLayoutInfo.widthPercent *= 60;
						gap.LayoutParameters = gap_layout;
						result.AddChild(gap, gap_layout);

						var segment = new ImageView(this);
						segment.Id = ++lastViewAutoID;
						segment.Background = Drawables.CreateFill(new Color(session.type == "Rest" ? new Color(0, 0, 255) : new Color(0, 255, 0)));
						var segment_layout = new PercentRelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
						var timeStopped_keptInDay =
							subsession.timeStopped.HasValue
								? (subsession.timeStopped.Value.Date == day.date ? subsession.timeStopped.Value : day.date.AddDays(1).Date)
								: DateTime.UtcNow.Date == day.date ? DateTime.UtcNow : DateTime.UtcNow.AddDays(1).Date;
						var segmentTime = timeStopped_keptInDay - subsession.timeStarted;
						segment_layout.PercentLayoutInfo.widthPercent = (float)(segmentTime.TotalMinutes / minutesInDay);
						if (mainData.settings.fastMode) // if fast mode, exhaggerate view size to 60x
							segment_layout.PercentLayoutInfo.widthPercent *= 60;
						//if (lastSubsessionView != null)
						//	segment_layout.AddRule(LayoutRules.RightOf, lastSubsessionView.Id);
						segment_layout.AddRule(LayoutRules.RightOf, gap.Id);
						segment.LayoutParameters = segment_layout;
						result.AddChild(segment, segment_layout);
						lastSubsessionSession = session;
						//lastSubsession = subsession;
						lastSubsessionView = segment;
						lastSubsessionEndTime = timeStopped_keptInDay;
					}
				}
			}

			return result;
		}
		//DateTime lastUpdateDayBoxTime;
        void UpdateDayBox(Day day)
		{
			if (day.box == null)
			{
				daysPanel.RemoveViewAt(0); // for now, remove the oldest day-row, when we add a new one (otherwise it'd exceed the numbers of rows at the start)
				AddDayToProductivityGraph(day);
				return;
			}

			// maybe make-so: we just update the row, rather than recreate it like this
			daysPanel.RemoveViewAt(daysPanel.ChildCount - 1);
			AddDayToProductivityGraph(day);

			//lastUpdateDayBoxTime = DateTime.UtcNow;
		}

		public void UpdateHourMarkers()
		{
			while (graphBottomBar.ChildCount > 1) // remove the last set (if it exists)
				graphBottomBar.RemoveViewAt(1);
			for (var i = 0; i < 24; i++)
			{
				var hourTime_local = DateTime.UtcNow.Date.AddHours(i).ToLocalTime();
				var hourMarker = graphBottomBar.AddChild(new TextView(this) {TextSize = 10}, new PercentRelativeLayout.LayoutParams(V.WrapContent, V.WrapContent));
				hourMarker.Text = mainData.settings.showLocalTime ? (mainData.settings.show12HourTime ? hourTime_local.ToString("htt").ToLower() : hourTime_local.Hour.ToString()) : i.ToString();
				var layoutParams = hourMarker.LayoutParameters as PercentRelativeLayout.LayoutParams;
				layoutParams.PercentLayoutInfo.leftMarginPercent = (float)(i / 24d);
				hourMarker.LayoutParameters = layoutParams;
			}
		}
		void UpdateCurrentTimerMarkerPosition()
		{
			//var hoursInDay = (DateTime.UtcNow.Date.AddDays(1).ClosestDate() - DateTime.UtcNow.Date).TotalHours;
			//var percentThroughDay = (DateTime.UtcNow - DateTime.UtcNow.Date).TotalHours / hoursInDay;
			var percentThroughDay = (DateTime.UtcNow - DateTime.UtcNow.Date).TotalHours / 24;
			var layoutParams = currentTimeMarker.LayoutParameters as PercentRelativeLayout.LayoutParams;
			var markerHalfWidthPercentOfBarWidth = graphBottomBar.Width != 0 ? (currentTimeMarker.Width / 2d) / graphBottomBar.Width : 0; // (can't access width while ui is still being laid out, apparently)
            layoutParams.PercentLayoutInfo.leftMarginPercent = (float)(percentThroughDay - markerHalfWidthPercentOfBarWidth);
			currentTimeMarker.LayoutParameters = layoutParams;
		}

		// time-left bar
		// ==========

		/*PendingIntent GetLaunchUpdateServicePendingIntent()
		{
			var launchUpdateService = new Intent(this, typeof(UpdateService));
			launchUpdateService.AddFlags(ActivityFlags.SingleTop);
			return PendingIntent.GetService(this, 0, launchUpdateService, PendingIntentFlags.UpdateCurrent);
		}*/
		PendingIntent GetPendingIntent_LaunchMain()
		{
			var launchMain = new Intent(this, typeof(MainActivity));
			launchMain.AddFlags(ActivityFlags.SingleTop);
			return PendingIntent.GetService(this, 0, launchMain, PendingIntentFlags.UpdateCurrent);
        }

		void StartSession(string type, int minutes)
		{
			// data
			if (CurrentSession != null)
				StopSession();
			var session = new Session(type, DateTime.UtcNow, minutes * SecondsPerMinute);
			//session.currentTimer_processedTimeExtent = JavaSystem.CurrentTimeMillis();
			CurrentDay.sessions.Add(session);

			// actors
			ResumeSession();
		}
		void PauseSession(bool endSubsession = true)
		{
			// data
            CurrentSession.paused = true;
			timeLeftBar.Background = CurrentSession.type == "Rest" ? Drawables.clip_yPlus_blue_dark : Drawables.clip_yPlus_green_dark;
			timeOverBar.Background = CurrentSession.type == "Rest" ? Drawables.clip_xPlus_blue_dark : Drawables.clip_xPlus_green_dark;
			Session_ProcessTimeUpToNow();
			if (endSubsession)
				CurrentSession.subsessions.Last().timeStopped = DateTime.UtcNow;

			// actors
			Session_UpdateOutflow();
			if (sessionUpdateTimer != null) // (for if called from OnCreate method)
				sessionUpdateTimer.Enabled = false;
			((AlarmManager)GetSystemService(AlarmService)).Cancel(GetPendingIntent_LaunchMain());
		}
		void ResumeSession(bool startSubsession = true)
		{
			// data
			CurrentSession.processedTimeExtent = DateTime.UtcNow;
			CurrentSession.paused = false;
			timeLeftBar.Background = CurrentSession.type == "Rest" ? Drawables.clip_yPlus_blue : Drawables.clip_yPlus_green;
			timeOverBar.Background = CurrentSession.type == "Rest" ? Drawables.clip_xPlus_blue : Drawables.clip_xPlus_green;
			//ProcessTimeUpToNow();
			if (startSubsession)
				CurrentSession.subsessions.Add(new Subsession(DateTime.UtcNow));

			// actors
			Session_UpdateOutflow();
			if (sessionUpdateTimer == null)
			{
				sessionUpdateTimer = new Timer(1000);
				sessionUpdateTimer.Elapsed += delegate
				{
					/*try
					{
						Session_ProcessTimeUpToNow();
						//Session_UpdateOutflow(false);
						Session_UpdateOutflow();
					}
					catch (Exception ex) { VDebug.Log(ex.StackTrace); }*/
					RunOnUiThread(()=>
					{
						Session_ProcessTimeUpToNow();
						Session_UpdateOutflow();
					});
				};
			}
			sessionUpdateTimer.Enabled = true;
			if (CurrentSession.timeLeft > 0)
				((AlarmManager)GetSystemService(AlarmService)).Set(AlarmType.RtcWakeup, CurrentSession.processedTimeExtent.Ticks_Milliseconds() + (CurrentSession.timeLeft * 1000), GetPendingIntent_LaunchMain());
		}
		void StopSession()
		{
			// data
			//ProcessTimeUpToNow();
			var session = CurrentSession;
			session.timeStopped = DateTime.UtcNow;
			if (!session.subsessions.Last().timeStopped.HasValue)
				session.subsessions.Last().timeStopped = DateTime.UtcNow;

			// actors
			Session_UpdateOutflow();
			UpdateDayBox(CurrentDay);
            sessionUpdateTimer.Enabled = false;
			((AlarmManager)GetSystemService(AlarmService)).Cancel(GetPendingIntent_LaunchMain());
		}

		public Timer sessionUpdateTimer;
		MediaPlayer alarmPlayer;
		void Session_ProcessTimeUpToNow() // actually, processes time up to [now, rounded to nearest second]
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
		//void UpdateOutflow(bool forceUpdateDayBox = true)
		void Session_UpdateOutflow()
		{
			UpdateAudio();
			//RunOnUiThread(UpdateDynamicUI);
			UpdateDynamicUI();
			//RunOnUiThread(UpdateNotification);
			UpdateNotification();
			/*if (forceUpdateDayBox || mainData.settings.fastMode || (DateTime.UtcNow - lastUpdateDayBoxTime).TotalMinutes >= 1) // if day-box-update forced, in fast-mode, or a minute since last
				RunOnUiThread(()=>UpdateDayBox(CurrentDay));*/
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
            var timeOverBar_clip = (ClipDrawable)timeOverBar.Background;

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
		public class QuickBroadcastReceiver : BroadcastReceiver
		{
			public QuickBroadcastReceiver(Action onReceive) { this.onReceive = onReceive; }
			Action onReceive;
			public override void OnReceive(Context arg0, Intent intent) { onReceive(); }
		}
		bool turningScreenOff;
		public void TurnScreenOff()
		{
			if (turningScreenOff) // if already turning screen off, ignore this call (so we don't lose the stored settings)
				return;

			// store old settings
			var oldStayOnWhilePluggedIn = Android.Provider.Settings.System.GetInt(ContentResolver, Android.Provider.Settings.System.StayOnWhilePluggedIn, (int)BatteryPlugged.Usb);
			var oldTimeout = Android.Provider.Settings.System.GetInt(ContentResolver, Android.Provider.Settings.System.ScreenOffTimeout, 3000);

			// change settings temporarily
			Android.Provider.Settings.System.PutInt(ContentResolver, Android.Provider.Settings.System.StayOnWhilePluggedIn, 0);
			Android.Provider.Settings.System.PutInt(ContentResolver, Android.Provider.Settings.System.ScreenOffTimeout, 0);
			//Android.Provider.Settings.System.PutInt(ContentResolver, Android.Provider.Settings.System.ScreenOffTimeout, 15000);
			Window.ClearFlags(WindowManagerFlags.KeepScreenOn);

			BroadcastReceiver receiver = null;
			receiver = new QuickBroadcastReceiver(()=>
			{
				// restore old settings
				Android.Provider.Settings.System.PutInt(ContentResolver, Android.Provider.Settings.System.StayOnWhilePluggedIn, oldStayOnWhilePluggedIn);
				Android.Provider.Settings.System.PutInt(ContentResolver, Android.Provider.Settings.System.ScreenOffTimeout, oldTimeout);
				UpdateKeepScreenOn();
				UnregisterReceiver(receiver);
				turningScreenOff = false;
			});
			RegisterReceiver(receiver, new IntentFilter(Intent.ActionScreenOff));
			turningScreenOff = true;
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
			mouseInputBlocker.Touch += (v, e)=>
			{
				if (e.Event.PointerCount >= 2)
					mouseInputBlocker.Visibility = ViewStates.Gone;
			};

			menu.Add("Settings");
			menu.Add("Block mouse input");
			menu.Add("About");
			return base.OnCreateOptionsMenu(menu);
		}
		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			if (mouseInputBlocker.Visibility == ViewStates.Visible)
				return base.OnOptionsItemSelected(item);

			//if (item.ItemId == Resource.Id.Settings)
			if (item.TitleFormatted.ToString() == "Settings")
				StartActivity(new Intent(this, typeof(SettingsActivity)));
			else if (item.TitleFormatted.ToString() == "Block mouse input")
				mouseInputBlocker.Visibility = ViewStates.Visible;
			//else if (item.ItemId == Resource.Id.About)
			else if (item.TitleFormatted.ToString() == "About")
			{
				AlertDialog.Builder alert = new AlertDialog.Builder(this);
				alert.SetTitle("About Productivity Tracker");
				alert.SetMessage("\"Improve productivity using a timer-assisted work-and-rest cycle, and track it on your lifetime productivity graph.\"");

				LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
				var text = linear.AddChild(new TextView(this));
				text.Text = @"
Author: Stephen Wicklund (Venryx)

This is an open source project, under the GPLv2 license.
The source code is available to view and modify.
Link: http://github.com/Venryx/Productivity-Tracker".Trim();
				text.SetPadding(30, 30, 30, 30);
				alert.SetView(linear);

				alert.SetPositiveButton("Ok", (sender, e)=> { });
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

			if (usedKey || mainData.settings.blockUnusedKeys)
				return true;
			return base.OnKeyDown(key, e);
		}
		public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
		{
			if (mainData.settings.blockUnusedKeys)
				return true;
			return base.OnKeyUp(keyCode, e);
		}
		public override bool OnKeyLongPress(Keycode keyCode, KeyEvent e)
		{
			if (mainData.settings.blockUnusedKeys)
				return true;
			return base.OnKeyLongPress(keyCode, e);
		}
		public override bool OnKeyMultiple(Keycode keyCode, int repeatCount, KeyEvent e)
		{
			if (mainData.settings.blockUnusedKeys)
				return true;
			return base.OnKeyMultiple(keyCode, repeatCount, e);
		}
		public override bool OnKeyShortcut(Keycode keyCode, KeyEvent e)
		{
			if (mainData.settings.blockUnusedKeys)
				return true;
			return base.OnKeyShortcut(keyCode, e);
		}
		/*public override bool OnGenericMotionEvent(MotionEvent e)
		{
			/*if ((e.Source & InputDevice.SourceClassJoystick) != 0)
				return true;*#/
			if (mainData.settings.blockMouseEvents)
				return true;
			return base.OnGenericMotionEvent(e);
		}
		public override bool OnTouchEvent(MotionEvent e)
		{
			if (mainData.settings.blockMouseEvents)
				return true;
			return base.OnTouchEvent(e);
		}
		public override bool OnTrackballEvent(MotionEvent e)
		{
			if (mainData.settings.blockMouseEvents)
				return true;
			return base.OnTrackballEvent(e);
		}*/
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
	}
}