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

			var settings = MainActivity.main.mainData.settings;

			{
				var row = AddRow(root, vertical: false);
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
					settings.keepScreenOnWhileOpen = !checkbox.Checked;
					checkbox.Checked = settings.keepScreenOnWhileOpen;
					MainActivity.main.RefreshKeepScreenOn();
				};
			}

			{
				var row = AddRow(root);
				row.AddChild(new TextView(this) {Text = "Number of timer steps", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
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
						MainActivity.main.RefreshTimerStepButtons();
					});
					alert.SetNegativeButton("Cancel", (sender, e)=> { });
					alert.Show();
				};
			}

			{
				var row = AddRow(root);
				row.AddChild(new TextView(this) {Text = "Time increment for timer steps", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
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
						MainActivity.main.RefreshTimerStepButtons();
					});
					alert.SetNegativeButton("Cancel", (sender, e)=> { });
					alert.Show();
				};
			}

			{
				//var alarmSoundPanel = root.Append(new LinearLayout(this) {Orientation = Orientation.Vertical}, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, 50));
				var row = AddRow(root);
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
				var row = AddRow(root);
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
				var row = AddRow(root);
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
				var row = AddRow(root);
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

			{
				var row = AddRow(root, ViewGroup.LayoutParams.WrapContent);
				row.AddChild(new TextView(this) {Text = "Hotkeys", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 55));

				var header = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Horizontal});
				header.AddChild(new TextView(this) {Text = "Key"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .45f));
				header.AddChild(new TextView(this) {Text = "Action"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .45f));
				header.AddChild(new TextView(this) {Text = ""}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .1f));

				Action refreshHotkeys = null;
				refreshHotkeys = ()=>
				{
					while (row.ChildCount > 3) // while there are controls other than the title, header, and add-hotkey-button (i.e. when there are old hotkey entry controls)
						row.RemoveViewAt(2);
					foreach (Hotkey hotkey in settings.hotkeys)
					{
						var row2 = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Horizontal}, null, row.ChildCount - 1);
						var keyLabel = row2.AddChild(new Button(this) {Text = hotkey.key.ToString()}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .45f));
						keyLabel.Click += (sender, e)=>
						{
							AlertDialog dialog = null;
							var builder = new AlertDialog.Builder(this);
                            builder.SetTitle("Key");
							var keys = Enum.GetValues(typeof(VKey)).OfType<VKey>().ToList();
							builder.SetSingleChoiceItems(keys.Select(a=>a.ToString()).ToArray(), keys.IndexOf(hotkey.key), (sender2, e2)=>{});
							builder.SetPositiveButton("Ok", (sender2, e2)=>
							{
								ListView listView = dialog.ListView;
								hotkey.key = (VKey)Enum.Parse(typeof(VKey), listView.Adapter.GetItem(listView.CheckedItemPosition).ToString());
								refreshHotkeys();
							});
							builder.SetNegativeButton("Cancel", (sender2, e2)=>{});
							dialog = builder.Show();
						};
						var actionLabel = row2.AddChild(new Button(this), new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .45f));
						actionLabel.Text = hotkey.action + (hotkey.action == HotkeyAction.StartTimer_Rest || hotkey.action == HotkeyAction.StartTimer_Work ? $" (length: {hotkey.action_startTimer_length} minutes)" : "");
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
								if (hotkey.action == HotkeyAction.StartTimer_Rest || hotkey.action == HotkeyAction.StartTimer_Work)
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
				
				var addHotkeyButton = row.AddChild(new Button(this) {Text = "Add hotkey"});
				addHotkeyButton.Click += (sender, e)=>
				{
					settings.hotkeys.Add(new Hotkey());
					refreshHotkeys();
				};

				refreshHotkeys();
			}
		}

		protected override void OnPause()
		{
			base.OnPause();

			MainActivity.main.SaveMainData();
		}

		LinearLayout AddRow(LinearLayout root, int height = 110, bool vertical = true)
		{
			var result = root.AddChild(new LinearLayout(this) {Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal}, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, height));
			result.SetPadding(15, 15, 15, 15);
			//if (root.ChildCount > 1)
			result.SetBackgroundResource(Resource.Drawable.Border_1_Bottom_LightGray);
			return result;
		}
	}
}