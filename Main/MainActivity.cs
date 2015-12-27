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
using VDFN;
/*
using Exception = System.Exception;
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
		const int ActivityID = 100;

		static DirectoryInfo RootFolder=>new DirectoryInfo("/storage/sdcard0/Productivity Tracker/");
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
		void LoadDaysTillReachesXCount(int xDays)
		{
			var today = DateTime.UtcNow.Date;
			/*for (var i = -xDays + 1; i <= 0; i++)
				LoadDay(today.AddDays(i));*/
			for (var i = -days.Count; i > -xDays; i--)
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
		ScrollView rowsPanelScroller;
		//PercentRelativeLayout rowsPanel;
		LinearLayout rowsPanel;
		PercentRelativeLayout graphBottomBar;
		ImageView currentTimeMarker;
		PercentRelativeLayout graph_overlayRoot;
		Button stopButton;
		Button pauseButton;
		LinearLayout right;
		ImageView timeLeftBar;
		ImageView timeOverBar;
		//TextView countdownLabel;
		Button countdownLabel;
		FrameLayout mouseInputBlocker;
		public Timer dayUpdateTimer;
		static void LogErrorMessageToFile(string message)
		{
			var file = RootFolder.GetFolder("Errors").GetFile(DateTime.UtcNow.ToString_U() + ".txt").CreateFolders();
			while (file.Exists)
				file = file.Directory.GetFile(file.NameWithoutExtension() + "_2.txt");
			File.WriteAllText(file.FullName, message);
		}
		class JavaExceptionCatcher : Java.Lang.Object, Thread.IUncaughtExceptionHandler
		{
			public void UncaughtException(Thread thread, Throwable ex)
				{ LogErrorMessageToFile($"Exception caught by Thread.DefaultUncaughtExceptionHandler\n==========\nHandle) {ex.Handle}\nClass) {ex.Class}\nCause) {ex.Cause}\nMessage) {ex.Message}\nStackTrace) {ex.StackTrace}"); }
		}
		protected override void OnCreate(Bundle bundle)
		{
			main = this;
			base.OnCreate(bundle);
			//SetContentView(Resource.Layout.Main);
			//var rootHolderGroup = (ViewGroup)Window.DecorView.RootView;
			var rootHolder = FindViewById<FrameLayout>(Android.Resource.Id.Content);
			//var root = (LinearLayout)rootHolder.GetChildAt(0);
			var root = new LinearLayout(this);
			SetContentView(root);

			var left = root.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new LinearLayout.LayoutParams(0, V.MatchParent, 1));
			{
				graphRoot = left.AddChild(new FrameLayout(this), new LinearLayout.LayoutParams(V.MatchParent, 0, .93f));
				/*var shape = new ShapeDrawable(new RectShape());
				shape.Paint.Color = new Color(0, 0, 0, 255);
				shape.Paint.SetStyle(Paint.Style.Stroke);
				shape.Paint.StrokeWidth = 5;
				graphRoot.Background = new InsetDrawable(shape, -5, -5, 5, 5); // top is pushed-out/outset, so bottom should be pulled-in/inset (so final vertical size is same, just offset, keeping the bottom-border on-screen)
				graphRoot.Background = shape;*/
				graphRoot.Background = new BorderDrawable(Color.Black, 0, 0, 5, 5);
				graphRoot.SetPadding(0, 0, 5, 5);

				var timeOverPanelHolder = left.AddChild(new LinearLayout(this), new LinearLayout.LayoutParams(V.MatchParent, 0, .07f));
				{
					stopButton = timeOverPanelHolder.AddChild(new Button(this) {Text = "Stop"}, new LinearLayout.LayoutParams(0, V.MatchParent, .075f));
					stopButton.Click += delegate { StopSession(); };
					pauseButton = timeOverPanelHolder.AddChild(new Button(this) {Text = "Stop"}, new LinearLayout.LayoutParams(0, V.MatchParent, .075f));
					pauseButton.Click += delegate
					{
						if (CurrentSession.paused)
							ResumeSession();
						else
							PauseSession();
					};

					var timeOverPanel = timeOverPanelHolder.AddChild(new FrameLayout(this), new LinearLayout.LayoutParams(0, V.MatchParent, .85f) { LeftMargin = 3});
					{
						timeOverBar = timeOverPanel.AddChild(new ImageView(this), new FrameLayout.LayoutParams(V.MatchParent, V.MatchParent));
					}
				}
			}

			right = root.AddChild(new LinearLayout(this), new LinearLayout.LayoutParams(0, V.MatchParent)); // width is set by UpdateTimerStepButtons
			{
				var timeLeftPanel = right.AddChild(new FrameLayout(this), new LinearLayout.LayoutParams(0, V.MatchParent, .5f));
				{
					timeLeftBar = timeLeftPanel.AddChild(new ImageView(this), new FrameLayout.LayoutParams(V.MatchParent, V.MatchParent));
				}
			}

			// called when C# code crashes?
			AppDomain.CurrentDomain.UnhandledException += (sender, e)=>LogErrorMessageToFile($"Exception caught by AppDomain.CurrentDomain.UnhandledException\n==========\nTerminating: {e.IsTerminating}\nExceptionObject: {e.ExceptionObject}");
			// called when Java code, on the UI thread, crashes?
			AndroidEnvironment.UnhandledExceptionRaiser += (sender, e)=>LogErrorMessageToFile($"Exception caught by AndroidEnvironment.UnhandledExceptionRaiser\n==========\n{e}");
			// called when Java code, (not handled by the above), crashes?
			Thread.DefaultUncaughtExceptionHandler = new JavaExceptionCatcher();
			//Thread.CurrentThread().UncaughtExceptionHandler = new JavaErrorCatcher();

			LoadMainData();
			LoadDaysTillReachesXCount(mainData.settings.daysVisibleAtOnce + 2);
			UpdateTimerStepButtons();

			VolumeControlStream = Stream.Alarm;
			baseTypeface = new Button(this).Typeface;

			// has to start with something
			//timeLeftBar.Background = new ClipDrawable(new ColorDrawable(CurrentSession.sessionType.color), GravityFlags.Bottom, ClipDrawableOrientation.Vertical);
			//timeOverBar.Background = new ClipDrawable(new ColorDrawable(CurrentSession.sessionType.color), GravityFlags.Left, ClipDrawableOrientation.Horizontal);

			// productivity graph
			// ==========

			graph_nonOverlayRoot = graphRoot.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new FrameLayout.LayoutParams(V.MatchParent, V.MatchParent));
			{
				rowsPanelScroller = graph_nonOverlayRoot.AddChild(new ScrollView(this), new LinearLayout.LayoutParams(V.MatchParent, 0, 1));
				/*rowsPanelScroller.ViewTreeObserver.ScrollChanged += (sender, e)=>
				{
					if (rowsPanelScroller.ScrollY == 0) // if scrolled to top of list, load another row
					{
						LoadDaysTillReachesXCount(days.Count + 1);
						AddRowsToGraphTillReachesXCount(rowsPanel.ChildCount + 1);
						//rowsPanelScroller.ScrollY += rowsPanel.GetChildren().Last().Height;
					}
				};*/
				rowsPanelScroller.Touch += (sender, e)=>
				{
					if (e.Event.Action == MotionEventActions.Up && rowsPanelScroller.ScrollY == 0) // if scrolled to top of list, load another row
					{
						LoadDaysTillReachesXCount(days.Count + 1);
						AddRowsToGraphTillReachesXCount(rowsPanel.ChildCount + 1);
						//rowsPanelScroller.ScrollY += rowsPanel.GetChildren().Last().Height;
					}
					e.Handled = false;
				};

				// ugh; need because a scroll-view can't contain a relative-layout directly, apparently (the relative-layout's rules don't all work)
				//var rowsPanelContent = rowsPanelScroller.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new FrameLayout.LayoutParams(V.MatchParent, V.MatchParent));
				//rowsPanel = rowsPanelContent.AddChild(new PercentRelativeLayout(this), new LinearLayout.LayoutParams(V.MatchParent, V.WrapContent));
				rowsPanel = rowsPanelScroller.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new FrameLayout.LayoutParams(V.MatchParent, V.WrapContent));
				{
					AddRowsToGraphTillReachesXCount(mainData.settings.daysVisibleAtOnce + 1);
					//rowsPanelScroller.VScrollTo(0, rowsPanelScroller.Height / mainData.settings.daysVisibleAtOnce);
					onWindowGainInitialFocus += ()=>rowsPanelScroller.VScrollTo(0, rowsPanelScroller.Bottom);
					/*rowsPanel.Post(()=>
					{
						AddRowsToGraphTillReachesXCount(mainData.settings.daysVisibleAtOnce + 1);
						rowsPanelScroller.VScrollTo(0, rowsPanelScroller.Bottom);
					});*/
				}
				graphBottomBar = graph_nonOverlayRoot.AddChild(new PercentRelativeLayout(this), new LinearLayout.LayoutParams(V.MatchParent, 30));
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
			graph_overlayRoot = graphRoot.AddChild(new PercentRelativeLayout(this), new FrameLayout.LayoutParams(V.MatchParent, V.MatchParent));
			{
				GenerateGraphOverlay();
			}

			// general
			// ==========
			
			UpdateKeepScreenOn();

			var overlayHolder = rootHolder.AddChild(new FrameLayout(this), new FrameLayout.LayoutParams(V.MatchParent, V.MatchParent));
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
			//mouseInputBlocker = overlayHolder.AddChild(new FrameLayout(this) {Visibility = ViewStates.Gone}, new FrameLayout.LayoutParams(V.MatchParent, V.MatchParent));
			//mouseInputBlocker.SetZ(10);
			mouseInputBlocker = rootHolder.AddChild(new FrameLayout(this) {Visibility = ViewStates.Gone}, new FrameLayout.LayoutParams(V.MatchParent, V.MatchParent));
			var mouseInputBlockerMessageLabel = mouseInputBlocker.AddChild(new TextView(this), new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) {Gravity = GravityFlags.Center});
			mouseInputBlockerMessageLabel.Text = "Mouse input is currently blocked. Press the screen with two fingers simultaneously to unblock.";

			// others
			// ==========

			UpdateDynamicUI();
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
					if (!dayUpdateTimer.Enabled) // (even after being disabled, timer may tick a couple extra times)
						return;
					UpdateRowBox(0);
					UpdateCurrentTimerMarkerPosition();
				});
			};
			dayUpdateTimer.Enabled = true;
		}
		protected override void OnDestroy()
		{
			// make sure we stop alarm-player, as it can at least sometimes live through the OnDestroy event (e.g. when bluetooth controller is connected/disconnected)
			StopAndDestroyAlarmPlayer();

			//dayUpdateTimer.Enabled = false;
			dayUpdateTimer.Dispose();

			if (sessionUpdateTimer != null)
				//sessionUpdateTimer.Elapsed -= SessionTimer_Elapsed;
				//sessionUpdateTimer.Enabled = false;
				sessionUpdateTimer.Dispose();
				//sessionUpdateTimer = null;

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
		bool hasHadFocus;
		Action onWindowGainInitialFocus = ()=>{};
		public override void OnWindowFocusChanged(bool hasFocus)
		{
			if (hasFocus && !hasHadFocus) // ui should be laid-out at this point
			{
				hasHadFocus = true;
				UpdateCurrentTimerMarkerPosition();
				//rowsPanelScroller.VScrollTo(0, int.MaxValue);
				onWindowGainInitialFocus();
			}
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
			while (right.ChildCount > 1)
				right.RemoveViewAt(1);
			right.LayoutParameters = (right.LayoutParameters as LinearLayout.LayoutParams).VAct(a=>a.Width = (100 * mainData.settings.sessionTypes.Count) + 230);
			foreach (SessionType sessionType in mainData.settings.sessionTypes)
			{
				var column = right.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new LinearLayout.LayoutParams(100, V.MatchParent));
				column.AddChild(new TextView(this) {Gravity = GravityFlags.CenterHorizontal, Text = sessionType.name}, new LinearLayout.LayoutParams(V.MatchParent, ViewGroup.LayoutParams.WrapContent));
				for (var i = 0; i < mainData.settings.numberOfTimerSteps; i++)
				{
					var timerStepButton = column.AddChild(new Button(this), new LinearLayout.LayoutParams(V.MatchParent, 0, .75f) {Gravity = GravityFlags.CenterVertical}, 1);
					var timerStepLength = mainData.settings.timeIncrementForTimerSteps * i; // in minutes
					timerStepButton.Text = timerStepLength.ToString();
					timerStepButton.Click += (sender, e)=> { StartSession(sessionType.name, timerStepLength); };
				}
			}
		}

		// productivity graph
		// ==========

		void GenerateGraphOverlay()
		{
			for (var i = 0; i < 24; i++)
			{
				var hourMarker = graph_overlayRoot.AddChild(new ImageView(this), new PercentRelativeLayout.LayoutParams(1, V.MatchParent));
				hourMarker.Background = Drawables.CreateColor(new Color(128, 128, 128, 70));
				var layoutParams = hourMarker.LayoutParameters as PercentRelativeLayout.LayoutParams;
				layoutParams.PercentLayoutInfo.leftMarginPercent = (float)(i / 24d);
				hourMarker.LayoutParameters = layoutParams;
			}
		}
		public void UpdateGraphVisibleRows()
		{
			var rowsVisible = rowsPanel.ChildCount;
			while (rowsPanel.ChildCount >= 1)
				rowsPanel.RemoveViewAt(0);
			AddRowsToGraphTillReachesXCount(rowsVisible);
		}
		void AddRowsToGraphTillReachesXCount(int xRows)
		{
			/*for (var rowOffset = -xRows + 1; daysPanel.ChildCount < xRows; rowOffset++)
				AddRowToGraph(i);*/
			for (var rowOffset = -rowsPanel.ChildCount; rowOffset > -xRows; rowOffset--)
				AddRowToGraph(rowOffset);
		}
		void AddRowToGraph(int rowOffset)
		{
			// assumes all more-to-bottom rows have been added (e.g. row(-1) expects row(0) to have been added)
			rowsPanel.AddChild(CreateRowBox(rowOffset), index: rowsPanel.ChildCount + rowOffset);

			/*rowsPanelScroller.Post(()=>
			{
				var boxHeight = rowsPanelScroller.Height / mainData.settings.daysVisibleAtOnce;
				rowsPanel.SetMinimumHeight(boxHeight * rowsPanel.GetChildren().Count);
				//rowsPanel.LayoutParameters = rowsPanel.LayoutParameters.VAct(a=>a.Height = boxHeight * rowsPanel.GetChildren().Count);
			});*/
		}
		DateTime Row_GetFirstColumnDateTime(int rowOffset)
		{
			var row0Day = DateTime.UtcNow.Date; // may be off by one day (solved by AddDaysTillDayContainsX call below)
			var rowDay = row0Day.AddDays(rowOffset); // may be off by one day (solved by AddDaysTillDayContainsX call below)
			int firstColumnUtcHour = mainData.settings.startGraphAtLocalHourX == -1 ? 0 : DateTime.Now.Date.AddHours(mainData.settings.startGraphAtLocalHourX).ToUniversalTime().Hour;
			var firstColumnUtcDateTime = rowDay.AddHours(firstColumnUtcHour).AddDaysTillDayContainsX(DateTime.UtcNow.AddDays(rowOffset));
			return firstColumnUtcDateTime;
		}
		DateTime Row_GetJustAfterLastColumnDateTime(int rowOffset) { return Row_GetFirstColumnDateTime(rowOffset).AddHours(24); }
		PercentRelativeLayout CreateRowBox(int rowOffset)
		{
			var result = new PercentRelativeLayout(this).SetID();
			/*//result.Post(()=>
			//{
			var layout = new PercentRelativeLayout.LayoutParams(V.MatchParent, V.WrapContent);
			/*var boxHeight = rowsPanelScroller.Height / mainData.settings.daysVisibleAtOnce;
			layout.AddRule(LayoutRules.AlignParentBottom);
			layout.BottomMargin = boxHeight * -rowOffset;
			layout.Height = boxHeight;*#/
			if (rowOffset == 0) //rowsPanel.ChildCount == 0)
			{
				layout.AddRule(LayoutRules.AlignParentBottom);
				if (rowsPanel.ChildCount >= 1) // if there are already children, update old-row-0 to anchor to this new row-0
				{
					var oldRow0 = rowsPanel.GetChildAt(rowsPanel.ChildCount - 1);
					var oldRow0Layout = oldRow0.LayoutParameters as PercentRelativeLayout.LayoutParams;
					oldRow0Layout.RemoveRule(LayoutRules.AlignParentBottom);
					oldRow0Layout.AddRule(LayoutRules.Above, result.Id);
					oldRow0.LayoutParameters = oldRow0Layout;
				}
			}
			else
				layout.AddRule(LayoutRules.Above, rowsPanel.GetChildAt(0).Id);
				//layout.AddRule(LayoutRules.Above, rowsPanel.GetChildAt(rowsPanel.IndexOfChild(result) + 1).Id);
			result.LayoutParameters = layout;
			//});*/
			result.LayoutParameters = new LinearLayout.LayoutParams(V.MatchParent, V.WrapContent);
			if (rowsPanelScroller.Height <= 1)
				result.Post(()=>result.LayoutParameters = result.LayoutParameters.VAct(a=>a.Height = rowsPanelScroller.Height / mainData.settings.daysVisibleAtOnce));
			else
				result.LayoutParameters = result.LayoutParameters.VAct(a=>a.Height = rowsPanelScroller.Height / mainData.settings.daysVisibleAtOnce);
			//result.LayoutParameters = new LinearLayout.LayoutParams(V.MatchParent, rowsPanelScroller.Height / mainData.settings.daysVisibleAtOnce);

			/*var rect = new RectShape();
			var shape = new ShapeDrawable(rect);
			shape.Paint.Color = new Color(255, 255, 255, 128);
			shape.Paint.SetStyle(Paint.Style.Stroke);
			shape.Paint.StrokeWidth = 1;
			//result.Background = new InsetDrawable(shape, -1, -1, -1, 0);
			result.Background = shape;
			result.SetPadding(1, 1, 1, 1);*/
			result.Background = new BorderDrawable(new Color(255, 255, 255, 128), 0, 0, 0, 1);
			result.SetPadding(0, 0, 0, 1);

			const int minutesInDay = 60 * 24;
			var firstColumnUtcHourTime = Row_GetFirstColumnDateTime(rowOffset);
			result.VTag(firstColumnUtcHourTime);
            var justAfterLastColumnUtcHourTime = Row_GetJustAfterLastColumnDateTime(rowOffset);

			var rowDays = new List<Day>(); // days with data that intersects this row's time-span
			foreach (Day day in days)
				if (day.sessions.Any(a=>a.subsessions.Any(b=>(!b.timeStopped.HasValue || b.timeStopped >= firstColumnUtcHourTime) && b.timeStarted < justAfterLastColumnUtcHourTime)))
					rowDays.Add(day);

			if (rowDays.Count > 0) // if there's data stored for this row
			{
				// maybe make-so: there's a better-matching setting for adding fake data like this
				/*if (mainData.settings.fastMode && previousDay != null)
				{
					var session = new Session("Work", day.date.AddMinutes(-10), 19) {timeStopped = day.date.AddMinutes(1)};
					var subsession = new Subsession(session.timeStarted) {timeStopped = session.timeStopped};
					session.subsessions.Add(subsession);
					previousDay.sessions.Add(session);
				}*/
				
				Session lastSubsessionSession = null;
				DateTime lastSubsessionEndTime = firstColumnUtcHourTime;

				var sessions = rowDays.SelectMany(a=>a.sessions).ToList();
				// add fake session at end of row (if not last row), or current-pos-marker (if last row) (so the gap-segment for the inactive-time period up to that point shows up)
				var finalGapStopTime = justAfterLastColumnUtcHourTime < DateTime.UtcNow ? justAfterLastColumnUtcHourTime : DateTime.UtcNow;
				sessions.Add(new Session(mainData.settings.sessionTypes[0].name, finalGapStopTime, 0).VAct(a=>a.subsessions.Add(new Subsession(finalGapStopTime) {timeStopped = finalGapStopTime})));
				foreach (Session session in sessions)
				{
					//var subsessions = session.subsessions.Where(a=>a.timeStarted < justAfterLastColumnUtcHourTime && (a.timeStopped == null || a.timeStopped >= firstColumnUtcHourTime)).ToList(); // if subsession partially within row's timespan
					var subsessions = session.subsessions.Where(a=>a.timeStarted <= justAfterLastColumnUtcHourTime && (a.timeStopped == null || a.timeStopped >= firstColumnUtcHourTime)).ToList(); // if subsession partially within row's timespan
					if (session.paused) // if session paused, add fake subsession after, so pause gap-segment shows up
						subsessions.Add(session.timeStopped.HasValue ? new Subsession(session.timeStopped.Value) {timeStopped = session.timeStopped} : new Subsession(DateTime.UtcNow));
					foreach (Subsession subsession in subsessions)
					{
						// gap
						// ==========

						var lowSegmentTime = subsession.timeStarted - lastSubsessionEndTime;
						if (lowSegmentTime.TotalMinutes > 0) // if negative, it means it was an overflow session from previous row (and so has no gap)
						{
							var lowSegment = new ImageView(this).SetID();
                            if (lastSubsessionSession == session)
								lowSegment.Background = Drawables.CreateFill(session.sessionType.color);
							var lowSegmentLayout = new PercentRelativeLayout.LayoutParams(V.MatchParent, V.MatchParent).VAddRule(LayoutRules.AlignParentBottom);
							lowSegmentLayout.PercentLayoutInfo.leftMarginPercent = (float)((lastSubsessionEndTime - firstColumnUtcHourTime).TotalMinutes / (24 * 60));
							lowSegmentLayout.PercentLayoutInfo.widthPercent = (float)(lowSegmentTime.TotalMinutes / minutesInDay);
							if (mainData.settings.fastMode && lastSubsessionSession == session) // if fast mode (and pause-type gap), exhaggerate view size to 60x
								lowSegmentLayout.PercentLayoutInfo.widthPercent *= 60;
							lowSegmentLayout.PercentLayoutInfo.heightPercent = .15f;
							result.AddChild(lowSegment, lowSegmentLayout);

							var arc = new ShapeDrawer(this);
							arc.AddShape(new VOval(0, 0, 1, 1) {ClipRect = new RectF(0, 0, 1, .5f), Color = Color.Black.NewA(128)});
							//arc.AddShape(new VRectangle(0, .5, 1, 1) {Op = VShapeOp.Clear});
							arc.AddShape(new VRectangle(0, .5, 1, 1) {Color = Color.Black.NewA(128)});
							var arcLayout = new PercentRelativeLayout.LayoutParams(V.MatchParent, V.MatchParent).VAddRule(LayoutRules.AlignLeft, lowSegment.Id).VAddRule(LayoutRules.AlignParentBottom);
							arcLayout.PercentLayoutInfo.widthPercent = lowSegmentLayout.PercentLayoutInfo.widthPercent;
							arcLayout.PercentLayoutInfo.heightPercent = .15f;
							result.AddChild(arc, arcLayout);

							var label = new TextView(this);
							var labelLayout = new PercentRelativeLayout.LayoutParams(V.MatchParent, V.MatchParent).VAddRule(LayoutRules.AlignLeft, lowSegment.Id).VAddRule(LayoutRules.AlignParentBottom);
							labelLayout.PercentLayoutInfo.widthPercent = lowSegmentLayout.PercentLayoutInfo.widthPercent;
							labelLayout.PercentLayoutInfo.heightPercent = .15f;
							labelLayout.BottomMargin = 2;
							label.Gravity = GravityFlags.Center;
							//label.SetPadding(0, 0, 0, 3);
							label.TextSize = 10;
							label.Text = ((int)lowSegmentTime.TotalMinutes).ToString();
							label.ShrinkTextUntilFitsInWidth(.9);
							result.AddChild(label, labelLayout);
						}

						// segment
						// ==========

						var timeStarted_keptOnGraph = subsession.timeStarted >= firstColumnUtcHourTime ? subsession.timeStarted : firstColumnUtcHourTime;
						var timeStopped_orNow = subsession.timeStopped ?? DateTime.UtcNow;
						var timeStopped_orNow_keptOnGraph = timeStopped_orNow < justAfterLastColumnUtcHourTime ? timeStopped_orNow : justAfterLastColumnUtcHourTime;
						var highSegmentTime = timeStopped_orNow_keptOnGraph - timeStarted_keptOnGraph;
						//highSegmentTime = new TimeSpan(0, 0, 10, 0); // for testing
						//if (highSegmentTime.TotalMinutes >= 1)
						{
							var highSegment = new ImageView(this).SetID();
							highSegment.Background = Drawables.CreateFill(session.sessionType.color);
							var highSegmentLayout = new PercentRelativeLayout.LayoutParams(V.MatchParent, V.MatchParent);
							highSegmentLayout.PercentLayoutInfo.leftMarginPercent = (float)((subsession.timeStarted - firstColumnUtcHourTime).TotalMinutes / (24 * 60));
							highSegmentLayout.PercentLayoutInfo.widthPercent = (float)(highSegmentTime.TotalMinutes / minutesInDay);
							if (mainData.settings.fastMode) // if fast mode, exhaggerate view size to 60x
								highSegmentLayout.PercentLayoutInfo.widthPercent *= 60;
							result.AddChild(highSegment, highSegmentLayout);
							lastSubsessionSession = session;
							lastSubsessionEndTime = timeStopped_orNow_keptOnGraph;

							var arc = new ShapeDrawer(this);
							arc.AddShape(new VOval(0, 0, 1, 1) {ClipRect = new RectF(0, 0, 1, .5f), Color = Color.Black.NewA(128)});
							arc.AddShape(new VRectangle(0, .5, 1, 1) {Color = Color.Black.NewA(128)});
							var arcLayout = new PercentRelativeLayout.LayoutParams(V.MatchParent, V.MatchParent).VAddRule(LayoutRules.AlignLeft, highSegment.Id).VAddRule(LayoutRules.AlignParentBottom);
							arcLayout.PercentLayoutInfo.widthPercent = highSegmentLayout.PercentLayoutInfo.widthPercent;
							arcLayout.PercentLayoutInfo.heightPercent = .15f;
							result.AddChild(arc, arcLayout);

							var label = new TextView(this);
							var labelLayout = new PercentRelativeLayout.LayoutParams(V.MatchParent, V.MatchParent).VAddRule(LayoutRules.AlignLeft, highSegment.Id).VAddRule(LayoutRules.AlignParentBottom);
							labelLayout.PercentLayoutInfo.widthPercent = highSegmentLayout.PercentLayoutInfo.widthPercent;
							labelLayout.PercentLayoutInfo.heightPercent = .15f;
							labelLayout.BottomMargin = 2;
							label.Gravity = GravityFlags.Center;
							label.TextSize = 10;
							label.Text = ((int)highSegmentTime.TotalMinutes).ToString();
							label.ShrinkTextUntilFitsInWidth(.9);
							result.AddChild(label, labelLayout);
						}
					}
				}
			}
			
			return result;
		}
        void UpdateRowBox(int rowOffset)
        {
	        var oldScroll = rowsPanelScroller.ScrollY;
	        var rowBoxIndex = (rowsPanel.ChildCount - 1) + rowOffset;
            var rowBoxAtIndex = rowsPanel.GetChildAt(rowBoxIndex);
			if (rowBoxAtIndex.VTag<DateTime>() != Row_GetFirstColumnDateTime(0)) // if time to add new row-box (since row-offset-0 is now supposed to show data for the next 24-hour time-span)
			{
				rowsPanel.RemoveViewAt(0); // for now, remove the oldest day-row, when we add a new one (otherwise it'd exceed the numbers of rows at the start)
				AddRowToGraph(0);
				//rowsPanel.ScrollY = oldScroll;
				rowsPanelScroller.VScrollTo(0, oldScroll, rowsPanel.GetChildren().Last());
				return;
			}

			// maybe make-so: we just update the row, rather than recreate it like this
			rowsPanel.RemoveViewAt(rowBoxIndex);
			AddRowToGraph(rowOffset);
			//rowsPanel.ScrollY = oldScroll;
			rowsPanelScroller.VScrollTo(0, oldScroll, rowsPanel.GetChildren().Last());
		}

		public void UpdateHourMarkers()
		{
			while (graphBottomBar.ChildCount > 1) // remove the last set (if it exists)
				graphBottomBar.RemoveViewAt(1);

			var firstColumnUtcHourTime = Row_GetFirstColumnDateTime(0);
			for (var columnIndex = 0; columnIndex < 24; columnIndex++)
			{
				var columnUtcHourTime = firstColumnUtcHourTime.AddHours(columnIndex);
				var hourMarker = graphBottomBar.AddChild(new TextView(this) {TextSize = 10}, new PercentRelativeLayout.LayoutParams(V.WrapContent, V.WrapContent));
				if (mainData.settings.showLocalTime)
					hourMarker.Text = mainData.settings.show12HourTime ? columnUtcHourTime.ToLocalTime().ToString("htt").ToLower() : columnUtcHourTime.ToLocalTime().Hour.ToString();
				else
					hourMarker.Text = mainData.settings.show12HourTime ? columnUtcHourTime.ToString("htt").ToLower() : columnUtcHourTime.Hour.ToString();
				var layoutParams = hourMarker.LayoutParameters as PercentRelativeLayout.LayoutParams;
				layoutParams.PercentLayoutInfo.leftMarginPercent = (float)(columnIndex / 24d);
				hourMarker.LayoutParameters = layoutParams;
			}
		}
		public void UpdateCurrentTimerMarkerPosition()
		{
			var firstColumnUtcHourTime = Row_GetFirstColumnDateTime(0);
			var percentThroughDay = (DateTime.UtcNow - firstColumnUtcHourTime).TotalHours / 24;
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
			timeLeftBar.Background = new ClipDrawable(new ColorDrawable(CurrentSession.sessionType.color), GravityFlags.Bottom, ClipDrawableOrientation.Vertical);
			timeOverBar.Background = new ClipDrawable(new ColorDrawable(CurrentSession.sessionType.color), GravityFlags.Left, ClipDrawableOrientation.Horizontal);
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
			timeLeftBar.Background = new ClipDrawable(new ColorDrawable(CurrentSession.sessionType.color), GravityFlags.Bottom, ClipDrawableOrientation.Vertical);
			timeOverBar.Background = new ClipDrawable(new ColorDrawable(CurrentSession.sessionType.color), GravityFlags.Left, ClipDrawableOrientation.Horizontal);
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
						if (!sessionUpdateTimer.Enabled) // (even after being disabled, timer may tick a couple extra times)
							return;
						Session_ProcessTimeUpToNow();
						Session_UpdateOutflow();
					});
				};
			}
			sessionUpdateTimer.Enabled = true;
			if (CurrentSession.timeLeft > 0)
				((AlarmManager)GetSystemService(AlarmService)).Set(AlarmType.RtcWakeup, CurrentSession.processedTimeExtent.TotalMilliseconds() + (CurrentSession.timeLeft * 1000), GetPendingIntent_LaunchMain());
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
			UpdateRowBox(0);
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
		void StopAndDestroyAlarmPlayer()
		{
			if (alarmPlayer != null) //&& alarmPlayer.IsPlaying)
			{
				alarmPlayer.SetVolume(0, 0);
				//alarmPlayer.Pause();
				//alarmPlayer.Stop();
				//alarmPlayer.Reset();
				alarmPlayer.Release();
				alarmPlayer = null; // if alarm-player was stopped, it's as good as null ™ (you'd have to call reset(), which makes it lose all its data anyway), so nullify it
			}
		}
		void UpdateAudio()
		{
			if (CurrentSession == null || CurrentSession.timeLeft > 0 || CurrentSession.paused) // if alarm-playing should not be playing, destroy it
				StopAndDestroyAlarmPlayer();
			else
			{
				var timeOver_withLocking = CurrentSession.timeOver_withLocking; // in seconds
				var timeOverForClipEmpty = CurrentSession.sessionType.timeToMaxVolume * SecondsPerMinute; // in seconds
				var percentThroughTimeOverBar = V.Clamp(0, 1, (double)timeOver_withLocking / timeOverForClipEmpty);

				if (alarmPlayer == null)
				{
					if (CurrentSession.sessionType.setMasterAlarmVolume != -1)
					{
						var audioManager = (AudioManager)GetSystemService(AudioService);
						int maxVolume = audioManager.GetStreamMaxVolume(Stream.Alarm);
						audioManager.SetStreamVolume(Stream.Alarm, (int)(((double)CurrentSession.sessionType.setMasterAlarmVolume / 100) * maxVolume), 0);
					}

					//alarmPlayer = MediaPlayer.Create(this, new FileInfo(data.settings.alarmSoundFilePath).ToFile().ToURI_Android());
					alarmPlayer = new MediaPlayer();
					if (CurrentSession.sessionType.alarmSoundFilePath != null)
					{
						alarmPlayer.SetAudioStreamType(Stream.Alarm);
						alarmPlayer.SetDataSource(this, new FileInfo(CurrentSession.sessionType.alarmSoundFilePath).ToFile().ToURI_Android());
						alarmPlayer.Prepare();
						alarmPlayer.Looping = true;
						//audioPlayer.SeekTo(timeOver_withLocking * 1000);
						//audioPlayer.SetWakeMode(this, WakeLockFlags.AcquireCausesWakeup);
						alarmPlayer.Start();
					}
				}

				var volume = V.Lerp(CurrentSession.sessionType.minVolume / 100d, CurrentSession.sessionType.maxVolume / 100d, percentThroughTimeOverBar);
                alarmPlayer.SetVolume((float)volume, (float)volume);
				//alarmPlayer.VSetVolume(V.Lerp(data.settings.minVolume / 100d, data.settings.maxVolume / 100d, percentThroughTimeOverBar), data.settings.volumeFadeType);
			}
		}
		void UpdateDynamicUI()
		{
			stopButton.Enabled = CurrentSession != null;
			pauseButton.Enabled = CurrentSession != null;
			pauseButton.Text = CurrentSession != null && CurrentSession.paused ? "Resume" : "Pause";
			countdownLabel.Enabled = CurrentSession != null && !CurrentSession.paused;
			
			var timeLeftBar_clip = (ClipDrawable)timeLeftBar.Background;
            var timeOverBar_clip = (ClipDrawable)timeOverBar.Background;
			if (timeLeftBar_clip == null) // if no timer started this launch
				return;

			if (CurrentSession != null)
			{
				//if (!data.currentTimer_paused)
				var timeLeft = CurrentSession.timeLeft; // in seconds
				//var timeOver = -timeLeft; // in seconds
				var timeOver_withLocking = CurrentSession.timeOver_withLocking; // in seconds

				var timeLeftForClipFull = mainData.settings.numberOfTimerSteps * mainData.settings.timeIncrementForTimerSteps * SecondsPerMinute; // in seconds
				var timeLeft_clipPercent = V.Clamp(0, 1, (double)timeLeft / timeLeftForClipFull);
				timeLeftBar_clip.SetLevel((int)(10000 * timeLeft_clipPercent));

				var timeOverForClipEmpty = CurrentSession.sessionType.timeToMaxVolume * SecondsPerMinute; // in seconds
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
				// we use the activity-id because it is a unique number; we use it later to cancel
				notificationManager.Notify(ActivityID, notification);
			}
			else // if stopped, or timer timed-out
			{
				var notificationManager = (NotificationManager)GetSystemService(NotificationService);
				notificationManager.Cancel(ActivityID);
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
				linear.SetPadding(30, 30, 30, 30);
				var text = linear.AddChild(new TextView(this));
				text.Text = @"
Author: Stephen Wicklund (Venryx)

This is an open source project, under the GPLv2 license.
The source code is available to view and modify.
Link: http://github.com/Venryx/Productivity-Tracker".Trim();
				alert.SetView(linear);

				alert.SetPositiveButton("Ok", (sender, e)=> { });
				alert.Show();
			}
			return base.OnOptionsItemSelected(item);
		}

		public List<string> recentKeys_strings = new List<string>();
		Dictionary<Keycode, DateTime> key_lastNonuppedDownTime = new Dictionary<Keycode, DateTime>(); // (for last 'real' down time, i.e. actual act of depression, rather than just key-still-being-held-down event)
		public override bool OnKeyDown(Keycode key, KeyEvent e) // ('on key down or held', is more accurate name)
		{
			recentKeys_strings.Add(DateTime.Now.ToString("HH:mm:ss") + ": " + key);
			while (recentKeys_strings.Count > 30)
				recentKeys_strings.RemoveAt(0);

			if (!key_lastNonuppedDownTime.ContainsKey(key))
				key_lastNonuppedDownTime[key] = DateTime.UtcNow;
			//else
			if ((DateTime.UtcNow - key_lastNonuppedDownTime[key]).TotalSeconds >= mainData.settings.keyHoldLength && key_lastNonuppedDownTime[key] != DateTime.MinValue)
			{
				RunHotkeysFor(key);
				key_lastNonuppedDownTime[key] = DateTime.MinValue;
			}

			if (mainData.settings.blockUnusedKeys)
				return true;
			return base.OnKeyDown(key, e);
		}
		void RunHotkeysFor(Keycode key)
		{
			//var usedKey = false;
			foreach (Hotkey hotkey in mainData.settings.hotkeys)
				if (hotkey.key == key)
				{
					//usedKey = true; // consider key used, even if the action is "None" (so, e.g. if user set hotkey for volume-up, they can also absorb volume-down key presses with "None" action hotkey)
					if (hotkey.action == HotkeyAction.StartSession)
						StartSession(hotkey.action_startSession_type, hotkey.action_startSession_length);
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
			//return usedKey;
		}
		public override bool OnKeyUp(Keycode key, KeyEvent e)
		{
			if (key_lastNonuppedDownTime.ContainsKey(key) && (DateTime.UtcNow - key_lastNonuppedDownTime[key]).TotalSeconds >= mainData.settings.keyHoldLength && key_lastNonuppedDownTime[key] != DateTime.MinValue)
			{
				RunHotkeysFor(key);
				key_lastNonuppedDownTime[key] = DateTime.MinValue;
			}
			key_lastNonuppedDownTime.Remove(key);

			if (mainData.settings.blockUnusedKeys)
				return true;
			return base.OnKeyUp(key, e);
		}
		public override bool OnKeyLongPress(Keycode key, KeyEvent e)
		{
			if (mainData.settings.blockUnusedKeys)
				return true;
			return base.OnKeyLongPress(key, e);
		}
		public override bool OnKeyMultiple(Keycode key, int repeatCount, KeyEvent e)
		{
			if (mainData.settings.blockUnusedKeys)
				return true;
			return base.OnKeyMultiple(key, repeatCount, e);
		}
		public override bool OnKeyShortcut(Keycode key, KeyEvent e)
		{
			if (mainData.settings.blockUnusedKeys)
				return true;
			return base.OnKeyShortcut(key, e);
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
			linear.SetPadding(30, 30, 30, 30);
			var text = linear.AddChild(new TextView(this));
			text.Text = recentKeys_strings.JoinUsing("\n");
			alert.SetView(linear);

			alert.SetPositiveButton("Ok", (sender, e) => { });
			alert.Show();
		}
	}
}