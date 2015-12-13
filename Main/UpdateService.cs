/*using System;
using System.IO;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Java.Lang;
using Stream = Android.Media.Stream;

namespace Main
{
	public class UpdateService : Service
	{
		public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
		{
			// build the widget update for today
			// no need for a screen, this just has to refresh all content in the background
			//cancelCountdownAlarm(this);
			//NotifyStatusBar();
			//StopSelf();
			return StartCommandResult.Sticky;
		}

		/*Timer currentTimer;
		void StartCurrentTimer()
		{
			if (currentTimer == null)
			{
				currentTimer = new Timer(1000);
				currentTimer.Elapsed += delegate
				{
					if (MainActivity.main.paused && MainActivity.main.currentTimer.Enabled)
						MainActivity.main.currentTimer_tick(null, null);
				};
			}
			currentTimer.Enabled = true;
		}*/

		/*void NotifyStatusBar()
		{
			// the PendingIntent to launch our activity if the user selects this notification
			Intent launchMain = new Intent(this, typeof(MainActivity));
			launchMain.SetFlags(ActivityFlags.SingleTop);
			//launchMain.PutExtra(INTENT_EXTRA_LAUNCH_COUNTDOWN, true);
			//var launchMain_pending = PendingIntent.GetActivity(this, 0, launchMain, PendingIntentFlags.OneShot);

			//launchMain_pending.Send(Result.Ok);
			StartActivity(launchMain);

			// set the icon, scrolling text and timestamp
			var builder = new Notification.Builder(this);
			builder.SetContentTitle("Productivity tracker");
			builder.SetContentText("Timer running. Time left: " + MainActivity.);
			//builder.SetSubText("[Extra info]");
			//builder.SetSmallIcon(R.drawable.notification_icon);
			builder.SetContentIntent(launchMain);
			builder.SetOngoing(true)
			var notification = builder.Build();

			NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);
			// we use a layout id because it is a unique number; we use it later to cancel.
			notificationManager.Notify(Resource.Layout.Main, notification);
		}*#/

		public override IBinder OnBind(Intent arg0) { return null; }
	}
}*/