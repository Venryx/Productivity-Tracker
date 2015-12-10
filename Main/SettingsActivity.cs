using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using VFileChooser;

namespace Main
{
	[Activity(Label = "Settings")]
	public class SettingsActivity : Activity
	{
		const int largeTextSize = 15;
		const int smallTextSize = 12;

		void Reload()
		{
			Finish();
			StartActivity(Intent);
		}

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			//SetContentView(Resource.Layout.Settings);

			var root = new LinearLayout(this) {Orientation = Orientation.Vertical};
			SetContentView(root, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

			var settings = MainActivity.main.settings;

			//var alarmSoundPanel = root.Append(new LinearLayout(this) {Orientation = Orientation.Vertical}, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, 50));
			var alarmSoundPanel = AddRow(root);
			alarmSoundPanel.Append(new TextView(this) {Text = "Alarm sound", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
			var alarmSoundLabel = alarmSoundPanel.Append(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
			alarmSoundLabel.Text = settings.alarmSoundFilePath != null && new FileInfo(settings.alarmSoundFilePath).Exists ? settings.alarmSoundFilePath : "[none]";
			alarmSoundPanel.Click += delegate
			{
				FileChooserDialog dialog = new FileChooserDialog(this);
				if (settings.alarmSoundFilePath != null && new FileInfo(settings.alarmSoundFilePath).Directory.Exists)
					dialog.loadFolder(new FileInfo(settings.alarmSoundFilePath).Directory.FullName);
				dialog.addListener((file, create)=>
				{
					settings.alarmSoundFilePath = file.Path;
					alarmSoundLabel.Text = settings.alarmSoundFilePath != null && new FileInfo(settings.alarmSoundFilePath).Exists ? settings.alarmSoundFilePath : "[none]";
					dialog.Dismiss();

					//MainActivity.main.SaveSettings();
					//Reload();
				});
				dialog.Show();
			};

			var maxVolumePanel = AddRow(root);
			maxVolumePanel.Append(new TextView(this) {Text = "Max volume", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
			var maxVolumeLabel = maxVolumePanel.Append(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
			maxVolumeLabel.Text = settings.maxVolume + "%";
			maxVolumePanel.Click += delegate
			{
				AlertDialog.Builder alert = new AlertDialog.Builder(this);
				alert.SetTitle("Max volume");

				LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
				var text = linear.Append(new TextView(this) {Text = settings.maxVolume + "%", Gravity = GravityFlags.CenterHorizontal});
				text.SetPadding(10, 10, 10, 10);
				SeekBar seek = linear.Append(new SeekBar(this));
				seek.Progress = settings.maxVolume;
				seek.ProgressChanged += (sender, e)=>{ text.Text = seek.Progress + "%"; };
				alert.SetView(linear);

				alert.SetPositiveButton("Ok", (sender, e)=>
				{
					settings.maxVolume = seek.Progress;
					maxVolumeLabel.Text = settings.maxVolume + "%";
				});
				alert.SetNegativeButton("Cancel", (sender, e)=>{});
				alert.Show();
			};

			var timeToMaxVolumePanel = AddRow(root);
			timeToMaxVolumePanel.Append(new TextView(this) {Text = "Time to max volume", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
			var timeToMaxVolumeLabel = timeToMaxVolumePanel.Append(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
			timeToMaxVolumeLabel.Text = settings.timeToMaxVolume + " minutes";
			timeToMaxVolumePanel.Click += delegate
			{
				AlertDialog.Builder alert = new AlertDialog.Builder(this);
				alert.SetTitle("Time to max volume");

				LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
				var text = linear.Append(new TextView(this) {Text = settings.timeToMaxVolume + " minutes", Gravity = GravityFlags.CenterHorizontal});
				text.SetPadding(10, 10, 10, 10);
				SeekBar seek = linear.Append(new SeekBar(this) {Max = 60});
				seek.Progress = settings.timeToMaxVolume;
				seek.ProgressChanged += (sender, e)=>{ text.Text = seek.Progress + " minutes"; };
				alert.SetView(linear);

				alert.SetPositiveButton("Ok", (sender, e)=>
				{
					settings.timeToMaxVolume = seek.Progress;
					timeToMaxVolumeLabel.Text = settings.timeToMaxVolume + " minutes";
				});
				alert.SetNegativeButton("Cancel", (sender, e)=>{});
				alert.Show();
			};
		}

		protected override void OnPause()
		{
			base.OnPause();

			MainActivity.main.SaveSettings();
		}

		LinearLayout AddRow(LinearLayout root)
		{
			var result = root.Append(new LinearLayout(this) {Orientation = Orientation.Vertical}, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, 110));
			result.SetPadding(15, 15, 15, 15);
			if (root.ChildCount > 1)
				result.SetBackgroundResource(Resource.Drawable.Border_1_Top_LightGray);
			return result;
		}
	}
}