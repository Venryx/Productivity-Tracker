using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
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
			NotifyStatusBar();
			StopSelf();
			return StartCommandResult.NotSticky;
		}

		// show Countdown Complete notification
		void NotifyStatusBar()
		{
			// the PendingIntent to launch our activity if the user selects this notification
			Intent launchMain = new Intent(this, typeof(MainActivity));
			launchMain.SetFlags(ActivityFlags.SingleTop);
			//launchMain.PutExtra(INTENT_EXTRA_LAUNCH_COUNTDOWN, true);
			//var launchMain_pending = PendingIntent.GetActivity(this, 0, launchMain, PendingIntentFlags.OneShot);

			//launchMain_pending.Send(Result.Ok);
			StartActivity(launchMain);

			// set the icon, scrolling text and timestamp
			/*var builder = new Notification.Builder(this);
			builder.SetContentTitle("Productivity tracker");
			builder.SetContentText("Countdown complete");
			//builder.SetSubText("[Extra info]");
			//builder.SetSmallIcon(R.drawable.notification_icon);
			builder.SetContentIntent(launchMainActivityIntent);
			var notification = builder.Build();

			//try
			//{
			notification.LedARGB = Color.Argb(255, 128, 128, 128); //0xFF808080;
			notification.LedOnMS = 500;
			notification.LedOffMS = 1000;
			//if (((Vibrator)GetSystemService(VibratorService)).HasVibrator)
			notification.Vibrate = new long[] {1000};
			notification.Flags |= NotificationFlags.ShowLights;
			notification.AudioStreamType = Stream.Notification;
			//notification.sound= Uri.parse("android.resource://com.geekyouup.android.ustopwatch/" + R.raw.alarm);
			//}
			//catch (Exception ex) {}

			notification.Defaults |= NotificationDefaults.All;

			NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);
			// we use a layout id because it is a unique number; we use it later to cancel.
			notificationManager.Notify(Resource.Layout.Main, notification);*/
		}

		public override IBinder OnBind(Intent arg0) { return null; }
	}
}