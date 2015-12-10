using System;
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
	[VDFType(propIncludeRegexL1:"", popOutL1: true)] public class Settings
	{
		public string alarmSoundFilePath;
		public int maxVolume = 50;
		public int timeToMaxVolume = 10;
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

				var timeLeftLabel = timeLeftPanel.Append(new TextView(this) {Gravity = GravityFlags.Center, TextSize = 30}, new FrameLayout.LayoutParams(200, 100) {Gravity = GravityFlags.Center});
				timeLeftLabel.SetSingleLine(true);
				timeLeftLabel.Text = "10:10";
			}

			var timeOverPanel = FindViewById<FrameLayout>(Resource.Id.TimeOverPanel);
			{
				var timeOverBar = FindViewById<ImageView>(Resource.Id.TimeOverBar);
				var timeOverBar_clip = (ClipDrawable)timeOverBar.Drawable;
				timeOverBar_clip.SetLevel((int)(10000 * 1));

				var soundIconButton = timeOverPanel.Append(new ImageButton(this), new FrameLayout.LayoutParams(30, 30) {Gravity = GravityFlags.CenterVertical});
				soundIconButton.SetBackgroundResource(Resource.Drawable.Volume);

				var timeOverLabel = timeOverPanel.Append(new TextView(this) {Gravity = GravityFlags.Center, TextSize = 30}, new FrameLayout.LayoutParams(200, 100) {Gravity = GravityFlags.Center});
				timeOverLabel.SetSingleLine(true);
				timeOverLabel.Text = "10:10";
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
			}
			return base.OnOptionsItemSelected(item);
		}
	}
}