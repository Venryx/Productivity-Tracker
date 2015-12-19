using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
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

			var root = new ScrollView(this);
			SetContentView(root, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
			var list = root.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			
			var settings = MainActivity.main.mainData.settings;

			AddSeparator(list, "General");
			// ==========

			{
				var row = AddRow(list, vertical: false);
				var leftSide = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				leftSide.AddChild(new TextView(this) {Text = "Keep screen on while open", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				//var label = leftSide.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				//var rightSide = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				//var checkbox = rightSide.AddChild(new CheckBox(this) {Gravity = GravityFlags.Right | GravityFlags.CenterVertical, Checked = settings.keepScreenOnWhileRunning});
				var rightSide = row.AddChild(new RelativeLayout(this), new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				var layoutParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
				layoutParams.AddRule(LayoutRules.AlignParentRight);
				layoutParams.AddRule(LayoutRules.CenterVertical);
				var checkbox = rightSide.AddChild(new CheckBox(this) {Checked = settings.keepScreenOnWhileOpen}, layoutParams);

				row.Click += delegate
				{
					checkbox.Checked = !checkbox.Checked;
					settings.keepScreenOnWhileOpen = checkbox.Checked;
					MainActivity.main.UpdateKeepScreenOn();
				};
			}

			{
				var row = AddRow(list, vertical: false, addSeparator: false);
				var leftSide = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				leftSide.AddChild(new TextView(this) {TextSize = largeTextSize, Text = "Fast mode"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = leftSide.AddChild(new TextView(this) {TextSize = smallTextSize, Text = "Have each 'minute' last only a second (for testing)"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				//var rightSide = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				//var checkbox = rightSide.AddChild(new CheckBox(this) {Gravity = GravityFlags.Right | GravityFlags.CenterVertical, Checked = settings.keepScreenOnWhileRunning});
				var rightSide = row.AddChild(new RelativeLayout(this), new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				var layoutParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
				layoutParams.AddRule(LayoutRules.AlignParentRight);
				layoutParams.AddRule(LayoutRules.CenterVertical);
				var checkbox = rightSide.AddChild(new CheckBox(this) {Checked = settings.fastMode}, layoutParams);

				row.Click += delegate
				{
					checkbox.Checked = !checkbox.Checked;
					settings.fastMode = checkbox.Checked;
				};
			}

			AddSeparator(list, "Productivity Graph");
			// ==========

			{
				var row = AddRow(list);
				row.AddChild(new TextView(this) {TextSize = largeTextSize, Text = "Days visible at once"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				label.Text = settings.daysVisibleAtOnce.ToString();
				row.Click += delegate
				{
					AlertDialog.Builder alert = new AlertDialog.Builder(this);
					alert.SetTitle("Days visible at once");

					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = settings.daysVisibleAtOnce.ToString(), Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 100});
					seek.Progress = settings.daysVisibleAtOnce;
					seek.ProgressChanged += (sender, e)=>{ text.Text = seek.Progress.ToString(); };
					alert.SetView(linear);

					alert.SetPositiveButton("Ok", (sender, e)=>
					{
						settings.daysVisibleAtOnce = seek.Progress;
						label.Text = settings.daysVisibleAtOnce.ToString();
						MainActivity.main.UpdateGraphVisibleRows();
					});
					alert.SetNegativeButton("Cancel", (sender, e)=>{});
					alert.Show();
				};
			}

			{
				var row = AddRow(list);
				row.AddChild(new TextView(this) {TextSize = largeTextSize, Text = "Start graph at local hour X"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				Func<string> getText = ()=>settings.startGraphAtLocalHourX == -1 ? "[utc 0]" : settings.startGraphAtLocalHourX.ToString();
				label.Text = getText();
                row.Click += delegate
				{
					AlertDialog.Builder alert = new AlertDialog.Builder(this);
					alert.SetTitle("Start graph at local hour X");

					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = getText(), Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					var minValue = -1;
					SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 23 - minValue});
					seek.SetValue(minValue, settings.startGraphAtLocalHourX);
					seek.ProgressChanged += (sender, e)=>{ text.Text = seek.GetValue(minValue) == -1 ? "[utc 0]" : seek.GetValue(minValue).ToString(); };
					alert.SetView(linear);

					alert.SetPositiveButton("Ok", (sender, e)=>
					{
						settings.startGraphAtLocalHourX = seek.GetValue(minValue);
						label.Text = getText();
						MainActivity.main.UpdateHourMarkers();
						MainActivity.main.UpdateCurrentTimerMarkerPosition();
						MainActivity.main.UpdateGraphVisibleRows();
					});
					alert.SetNegativeButton("Cancel", (sender, e)=>{});
					alert.Show();
				};
			}

			{
				var row = AddRow(list, vertical: false);
				var leftSide = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				leftSide.AddChild(new TextView(this) {Text = "Show local time", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = leftSide.AddChild(new TextView(this) {TextSize = smallTextSize, Text = "Show local time, rather than UTC time"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var rightSide = row.AddChild(new RelativeLayout(this), new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				var layoutParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
				layoutParams.AddRule(LayoutRules.AlignParentRight);
				layoutParams.AddRule(LayoutRules.CenterVertical);
				var checkbox = rightSide.AddChild(new CheckBox(this) {Checked = settings.showLocalTime}, layoutParams);

				row.Click += delegate
				{
					checkbox.Checked = !checkbox.Checked;
					settings.showLocalTime = checkbox.Checked;
					MainActivity.main.UpdateHourMarkers();
				};
			}

			{
				var row = AddRow(list, vertical: false, addSeparator: false);
				var leftSide = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				leftSide.AddChild(new TextView(this) {Text = "Show 12-hour time", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = leftSide.AddChild(new TextView(this) {TextSize = smallTextSize, Text = "Show 12-hour/am-pm time, rather than 24-hour time"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var rightSide = row.AddChild(new RelativeLayout(this), new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				var layoutParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
				layoutParams.AddRule(LayoutRules.AlignParentRight);
				layoutParams.AddRule(LayoutRules.CenterVertical);
				var checkbox = rightSide.AddChild(new CheckBox(this) {Checked = settings.show12HourTime}, layoutParams);

				row.Click += delegate
				{
					checkbox.Checked = !checkbox.Checked;
					settings.show12HourTime = checkbox.Checked;
					MainActivity.main.UpdateHourMarkers();
				};
			}

			AddSeparator(list, "Timer");
			// ==========

			{
				var row = AddRow(list);
				row.AddChild(new TextView(this) {TextSize = largeTextSize, Text = "Number of timer steps"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				label.Text = settings.numberOfTimerSteps.ToString();
				row.Click += delegate
				{
					AlertDialog.Builder alert = new AlertDialog.Builder(this);
					alert.SetTitle("Number of timer steps");

					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = settings.numberOfTimerSteps.ToString(), Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 21});
					seek.Progress = settings.numberOfTimerSteps;
					seek.ProgressChanged += (sender, e)=>{ text.Text = seek.Progress.ToString(); };
					alert.SetView(linear);

					alert.SetPositiveButton("Ok", (sender, e)=>
					{
						settings.numberOfTimerSteps = seek.Progress;
						label.Text = settings.numberOfTimerSteps.ToString();
						MainActivity.main.UpdateTimerStepButtons();
					});
					alert.SetNegativeButton("Cancel", (sender, e)=>{});
					alert.Show();
				};
			}

			{
				var row = AddRow(list, addSeparator: false);
				row.AddChild(new TextView(this) {TextSize = largeTextSize, Text = "Time increment for timer steps"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				label.Text = settings.timeIncrementForTimerSteps + " minutes";
				row.Click += delegate
				{
					AlertDialog.Builder alert = new AlertDialog.Builder(this);
					alert.SetTitle("Time increment for timer steps");

					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = settings.timeIncrementForTimerSteps + " minutes", Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 60});
					seek.Progress = settings.timeIncrementForTimerSteps;
					seek.ProgressChanged += (sender, e)=>{ text.Text = seek.Progress + " minutes"; };
					alert.SetView(linear);

					alert.SetPositiveButton("Ok", (sender, e)=>
					{
						settings.timeIncrementForTimerSteps = seek.Progress;
						label.Text = settings.timeIncrementForTimerSteps + " minutes";
						MainActivity.main.UpdateTimerStepButtons();
					});
					alert.SetNegativeButton("Cancel", (sender, e)=>{});
					alert.Show();
				};
			}

			AddSeparator(list, "Alarm");
			// ==========

			{
				var row = AddRow(list);
				row.AddChild(new TextView(this) {TextSize = largeTextSize, Text = "Set master alarm volume to X, before sounding alarm"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				Func<string> getText = ()=>settings.setMasterAlarmVolume == -1 ? "[don't set]" : settings.setMasterAlarmVolume + "%";
				label.Text = getText();
                row.Click += delegate
				{
					AlertDialog.Builder alert = new AlertDialog.Builder(this);
					alert.SetTitle("Set master alarm volume to X, before sounding alarm");

					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = getText(), Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					var minValue = -1;
					SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 100 - minValue});
					seek.SetValue(minValue, settings.setMasterAlarmVolume);
					seek.ProgressChanged += (sender, e)=>{ text.Text = seek.GetValue(minValue) == -1 ? "[don't set]" : seek.GetValue(minValue) + "%"; };
					alert.SetView(linear);

					alert.SetPositiveButton("Ok", (sender, e)=>
					{
						settings.setMasterAlarmVolume = seek.GetValue(minValue);
						label.Text = getText();
					});
					alert.SetNegativeButton("Cancel", (sender, e)=>{});
					alert.Show();
				};
			}

			{
				//var alarmSoundPanel = root.Append(new LinearLayout(this) {Orientation = Orientation.Vertical}, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, 50));
				var row = AddRow(list);
				row.AddChild(new TextView(this) {Text = "Alarm sound", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				label.Text = settings.alarmSoundFilePath != null && new FileInfo(settings.alarmSoundFilePath).Exists ? settings.alarmSoundFilePath : "[none]";
				row.Click += delegate
				{
					FileChooserDialog dialog = new FileChooserDialog(this);
					if (settings.alarmSoundFilePath != null && new FileInfo(settings.alarmSoundFilePath).Directory.Exists)
						dialog.loadFolder(new FileInfo(settings.alarmSoundFilePath).Directory.FullName);
					dialog.addListener((file, create)=>
					{
						settings.alarmSoundFilePath = file.Path;
						label.Text = settings.alarmSoundFilePath != null && new FileInfo(settings.alarmSoundFilePath).Exists ? settings.alarmSoundFilePath : "[none]";
						dialog.Dismiss();

						//MainActivity.main.SaveSettings();
						//Reload();
					});
					dialog.Show();
				};
			}

			{
				var row = AddRow(list);
				row.AddChild(new TextView(this) {Text = "Min volume", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				label.Text = settings.minVolume + "%";
				row.Click += delegate
				{
					AlertDialog.Builder alert = new AlertDialog.Builder(this);
					alert.SetTitle("Min volume");

					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = settings.minVolume + "%", Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					SeekBar seek = linear.AddChild(new SeekBar(this));
					seek.Progress = settings.minVolume;
					seek.ProgressChanged += (sender, e)=>{ text.Text = seek.Progress + "%"; };
					alert.SetView(linear);

					alert.SetPositiveButton("Ok", (sender, e)=>
					{
						settings.minVolume = seek.Progress;
						label.Text = settings.minVolume + "%";
					});
					alert.SetNegativeButton("Cancel", (sender, e)=>{});
					alert.Show();
				};
			}

			{
				var row = AddRow(list);
				row.AddChild(new TextView(this) {Text = "Max volume", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				label.Text = settings.maxVolume + "%";
				row.Click += delegate
				{
					AlertDialog.Builder alert = new AlertDialog.Builder(this);
					alert.SetTitle("Max volume");

					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = settings.maxVolume + "%", Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					SeekBar seek = linear.AddChild(new SeekBar(this));
					seek.Progress = settings.maxVolume;
					seek.ProgressChanged += (sender, e)=>{ text.Text = seek.Progress + "%"; };
					alert.SetView(linear);

					alert.SetPositiveButton("Ok", (sender, e)=>
					{
						settings.maxVolume = seek.Progress;
						label.Text = settings.maxVolume + "%";
					});
					alert.SetNegativeButton("Cancel", (sender, e)=> { });
					alert.Show();
				};
			}

			{
				var row = AddRow(list, addSeparator: false);
				row.AddChild(new TextView(this) {Text = "Time to max volume", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				label.Text = settings.timeToMaxVolume + " minutes";
				row.Click += delegate
				{
					AlertDialog.Builder alert = new AlertDialog.Builder(this);
					alert.SetTitle("Time to max volume");

					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = settings.timeToMaxVolume + " minutes", Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 60});
					seek.Progress = settings.timeToMaxVolume;
					seek.ProgressChanged += (sender, e)=>{ text.Text = seek.Progress + " minutes"; };
					alert.SetView(linear);

					alert.SetPositiveButton("Ok", (sender, e)=>
					{
						settings.timeToMaxVolume = seek.Progress;
						label.Text = settings.timeToMaxVolume + " minutes";
					});
					alert.SetNegativeButton("Cancel", (sender, e)=> { });
					alert.Show();
				};
			}

			/*{
				var row = AddRow(list);
				row.AddChild(new TextView(this) {Text = "Volume fade type", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				label.Text = settings.volumeFadeType.ToString();
				row.Click += delegate
				{
					AlertDialog dialog = null;
					var builder = new AlertDialog.Builder(this);
                    builder.SetTitle("Volume fade type");
					var keys = Enum.GetValues(typeof(VolumeScaleType)).OfType<VolumeScaleType>().ToList();
					builder.SetSingleChoiceItems(keys.Select(a=>a.ToString()).ToArray(), keys.IndexOf(settings.volumeFadeType), (sender2, e2)=>{});
					builder.SetPositiveButton("Ok", (sender2, e2)=>
					{
						ListView listView = dialog.ListView;
						settings.volumeFadeType = (VolumeScaleType)Enum.Parse(typeof(VolumeScaleType), listView.Adapter.GetItem(listView.CheckedItemPosition).ToString());
						label.Text = settings.volumeFadeType.ToString();
					});
					builder.SetNegativeButton("Cancel", (sender2, e2)=>{});
					dialog = builder.Show();
				};
			}*/

			AddSeparator(list, "Hotkeys");
			// ==========
			
			{
				var row = AddRow(list, vertical: false, addSeparator: false);
				var leftSide = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				leftSide.AddChild(new TextView(this) {TextSize = largeTextSize, Text = "Block unused keys"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = leftSide.AddChild(new TextView(this) {TextSize = smallTextSize, Text = "Block any key events not used by the hotkeys specified"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var rightSide = row.AddChild(new RelativeLayout(this), new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				var layoutParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
				layoutParams.AddRule(LayoutRules.AlignParentRight);
				layoutParams.AddRule(LayoutRules.CenterVertical);
				var checkbox = rightSide.AddChild(new CheckBox(this) {Checked = settings.blockUnusedKeys}, layoutParams);
				row.Click += delegate
				{
					checkbox.Checked = !checkbox.Checked;
					settings.blockUnusedKeys = checkbox.Checked;
				};
			}

			/*{
				var row = AddRow(list, vertical: false, addSeparator: false);
				var leftSide = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				leftSide.AddChild(new TextView(this) {TextSize = largeTextSize, Text = "Block mouse events"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = leftSide.AddChild(new TextView(this) {TextSize = smallTextSize, Text = "Block all (physical/usb/bluetooth) mouse events"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var rightSide = row.AddChild(new RelativeLayout(this), new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				var layoutParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
				layoutParams.AddRule(LayoutRules.AlignParentRight);
				layoutParams.AddRule(LayoutRules.CenterVertical);
				var checkbox = rightSide.AddChild(new CheckBox(this) {Checked = settings.blockMouseEvents}, layoutParams);
				row.Click += delegate
				{
					checkbox.Checked = !checkbox.Checked;
					settings.blockMouseEvents = checkbox.Checked;
				};
			}*/

			{
				var row = AddRow(list, ViewGroup.LayoutParams.WrapContent, addSeparator: false);
				//row.AddChild(new TextView(this) {Text = "Hotkeys", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 55));

				var header = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Horizontal});
				header.AddChild(new TextView(this) {Text = "Key"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .45f));
				header.AddChild(new TextView(this) {Text = "Action"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .45f));
				header.AddChild(new TextView(this) {Text = ""}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .1f));

				Action refreshHotkeys = null;
				refreshHotkeys = ()=>
				{
					//while (row.ChildCount > 3) // while there are controls other than the title, header, and add-hotkey-button (i.e. when there are old hotkey entry controls)
					//	row.RemoveViewAt(2);
					while (row.ChildCount > 2) // while there are controls other than the header and add-hotkey-button (i.e. when there are old hotkey entry controls)
						row.RemoveViewAt(1);
					foreach (Hotkey hotkey in settings.hotkeys)
					{
						var row2 = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Horizontal}, null, row.ChildCount - 1);
						var keyLabel = row2.AddChild(new Button(this) {Text = hotkey.key.ToString()}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .45f));
						keyLabel.Click += (sender, e)=>
						{
							AlertDialog dialog = null;
							var builder = new AlertDialog.Builder(this);
                            builder.SetTitle("Key");
							var keys = Enum.GetValues(typeof(Keycode)).OfType<Keycode>().ToList();
							builder.SetSingleChoiceItems(keys.Select(a=>a.ToString()).ToArray(), keys.IndexOf(hotkey.key), (sender2, e2)=>{});
							builder.SetPositiveButton("Ok", (sender2, e2)=>
							{
								ListView listView = dialog.ListView;
								hotkey.key = (Keycode)Enum.Parse(typeof(Keycode), listView.Adapter.GetItem(listView.CheckedItemPosition).ToString());
								refreshHotkeys();
							});
							builder.SetNegativeButton("Cancel", (sender2, e2)=>{});
							dialog = builder.Show();
						};
						var actionLabel = row2.AddChild(new Button(this), new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .45f));
						actionLabel.Text = hotkey.action + (hotkey.action == HotkeyAction.StartSession_Rest || hotkey.action == HotkeyAction.StartSession_Work ? $" (length: {hotkey.action_startTimer_length} minutes)" : "");
						actionLabel.Click += (sender, e)=>
						{
							AlertDialog dialog = null;
							var builder = new AlertDialog.Builder(this);
							builder.SetTitle("Action");
							var actions = Enum.GetValues(typeof(HotkeyAction)).OfType<HotkeyAction>().ToList();
							builder.SetSingleChoiceItems(actions.Select(a=>a.ToString()).ToArray(), actions.IndexOf(hotkey.action), (sender2, e2)=>{});
							builder.SetPositiveButton("Ok", (sender2, e2)=>
							{
								ListView listView = dialog.ListView;
								hotkey.action = (HotkeyAction)Enum.Parse(typeof(HotkeyAction), listView.Adapter.GetItem(listView.CheckedItemPosition).ToString());
								if (hotkey.action == HotkeyAction.StartSession_Rest || hotkey.action == HotkeyAction.StartSession_Work)
								{
									AlertDialog.Builder alert = new AlertDialog.Builder(this);
									alert.SetCancelable(false);
									alert.SetTitle("Start timer - length");

									LinearLayout linear = new LinearLayout(this) { Orientation = Orientation.Vertical };
									var text = linear.AddChild(new TextView(this) { Text = hotkey.action_startTimer_length + " minutes", Gravity = GravityFlags.CenterHorizontal });
									text.SetPadding(10, 10, 10, 10);
									SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 180});
									seek.Progress = hotkey.action_startTimer_length;
									seek.ProgressChanged += (sender3, e3)=>{ text.Text = seek.Progress + " minutes"; };
									alert.SetView(linear);

									alert.SetPositiveButton("Ok", (sender3, e3)=>
									{
										hotkey.action_startTimer_length = seek.Progress;
										refreshHotkeys();
									});
									alert.Show();
								}
								else
									refreshHotkeys();
							});
							builder.SetNegativeButton("Cancel", (sender2, e2)=>{});
							dialog = builder.Show();
						};
						var deleteButton = row2.AddChild(new Button(this) {Text = "Delete"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .1f));
						deleteButton.Click += (sender, e)=>
						{
							settings.hotkeys.Remove(hotkey);
							refreshHotkeys();
						};
					}
				};

				var buttonsRow = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Horizontal});
				var addHotkeyButton = buttonsRow.AddChild(new Button(this) {Text = "Add hotkey"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, .7f));
				addHotkeyButton.Click += (sender, e)=>
				{
					settings.hotkeys.Add(new Hotkey());
					refreshHotkeys();
				};
				var showRecentKeysButton = buttonsRow.AddChild(new Button(this) {Text = "(show recent keys)"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, .3f));
				showRecentKeysButton.Click += (sender, e)=>MainActivity.main.ShowRecentKeys(this);

				refreshHotkeys();
			}
		}
		protected override void OnPause()
		{
			base.OnPause();

			MainActivity.main.SaveMainData();
		}

		LinearLayout AddSeparator(LinearLayout root, string text = null)
		{
			var result = root.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, 60));
			//result.SetHorizontalGravity(GravityFlags.Center);

			if (text != null)
			{
				var label = result.AddChild(new TextView(this) {TextSize = largeTextSize, Text = text});
				label.SetPadding(10, 10, 10, 10);
				label.SetTypeface(MainActivity.main.baseTypeface, TypefaceStyle.Bold);
			}

			var rect = new RectShape();
            var shape = new ShapeDrawable(rect);
			shape.Paint.Color = new Color(255, 255, 255, 200);
			shape.Paint.SetStyle(Paint.Style.Stroke);
			shape.Paint.StrokeWidth = 3;
			//shape.SetPadding(3, 3, 3, 3);
			//result.Background = shape;
			result.Background = new InsetDrawable(shape, -3, -3, -3, 3);
			//result.SetPadding(3, 3, 3, 3); // must come after

			return result;
		}
		LinearLayout AddRow(LinearLayout root, int height = 110, bool vertical = true, bool addSeparator = true)
		{
			var result = root.AddChild(new LinearLayout(this) {Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal}, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, height));
			//if (root.ChildCount > 1)
			if (addSeparator)
			{
				//result.SetBackgroundResource(Resource.Drawable.Border_1_Bottom_LightGray);
				var rect = new RectShape();
				var shape = new ShapeDrawable(rect);
				shape.Paint.Color = new Color(255, 255, 255, 128);
				shape.Paint.SetStyle(Paint.Style.Stroke);
				shape.Paint.StrokeWidth = 1;
				result.Background = new InsetDrawable(shape, -1, -1, -1, 0);
			}
			result.SetPadding(15, 15, 15, 15); // must come after
			return result;
		}

		public override bool OnKeyDown(Keycode key, KeyEvent e)
		{
			var recentKeys = MainActivity.main.recentKeys_strings;
			recentKeys.Add(DateTime.Now.ToString("HH:mm:ss") + ": " + key);
			while (recentKeys.Count > 30)
				recentKeys.RemoveAt(0);
			return base.OnKeyDown(key, e);
		}
	}
}