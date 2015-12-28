using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using android.support.percent;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidColorPicker;
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
			var list = root.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, V.WrapContent));
			
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
				var checkbox = rightSide.AddChild(new CheckBox(this) {Clickable = false, Checked = settings.keepScreenOnWhileOpen}, new RelativeLayout.LayoutParams(V.WrapContent, V.WrapContent).VAddRule(LayoutRules.AlignParentRight).VAddRule(LayoutRules.CenterVertical));
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
				var checkbox = rightSide.AddChild(new CheckBox(this) {Clickable = false, Checked = settings.fastMode}, new RelativeLayout.LayoutParams(V.WrapContent, V.WrapContent).VAddRule(LayoutRules.AlignParentRight).VAddRule(LayoutRules.CenterVertical));
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
					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = settings.daysVisibleAtOnce.ToString(), Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 100});
					seek.Progress = settings.daysVisibleAtOnce;
					seek.ProgressChanged += (sender, e)=>{ text.Text = seek.Progress.ToString(); };

					new AlertDialog.Builder(this).SetTitle("Days visible at once")
						.SetView(linear)
						.SetPositiveButton("OK", (sender, e)=>
						{
							settings.daysVisibleAtOnce = seek.Progress;
							label.Text = settings.daysVisibleAtOnce.ToString();
							MainActivity.main.UpdateGraphVisibleRows();
						})
						.SetNegativeButton("Cancel", (sender, e)=>{})
						.Show();
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
					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = getText(), Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					var minValue = -1;
					SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 23 - minValue});
					seek.SetValue(minValue, settings.startGraphAtLocalHourX);
					seek.ProgressChanged += (sender, e)=>{ text.Text = seek.GetValue(minValue) == -1 ? "[utc 0]" : seek.GetValue(minValue).ToString(); };

					new AlertDialog.Builder(this).SetTitle("Start graph at local hour X")
						.SetView(linear)
						.SetPositiveButton("OK", (sender, e)=>
						{
							settings.startGraphAtLocalHourX = seek.GetValue(minValue);
							label.Text = getText();
							MainActivity.main.UpdateHourMarkers();
							MainActivity.main.UpdateCurrentTimerMarkerPosition();
							MainActivity.main.UpdateGraphVisibleRows();
						})
						.SetNegativeButton("Cancel", (sender, e)=>{})
						.Show();
				};
			}

			{
				var row = AddRow(list, vertical: false);
				var leftSide = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				leftSide.AddChild(new TextView(this) {Text = "Show local time", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = leftSide.AddChild(new TextView(this) {TextSize = smallTextSize, Text = "Show local time, rather than UTC time"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var rightSide = row.AddChild(new RelativeLayout(this), new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				var checkbox = rightSide.AddChild(new CheckBox(this) {Clickable = false, Checked = settings.showLocalTime}, new RelativeLayout.LayoutParams(V.WrapContent, V.WrapContent).VAddRule(LayoutRules.AlignParentRight).VAddRule(LayoutRules.CenterVertical));
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
				var checkbox = rightSide.AddChild(new CheckBox(this) {Clickable = false, Checked = settings.show12HourTime}, new RelativeLayout.LayoutParams(V.WrapContent, V.WrapContent).VAddRule(LayoutRules.AlignParentRight).VAddRule(LayoutRules.CenterVertical));
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
					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = settings.numberOfTimerSteps.ToString(), Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 21});
					seek.Progress = settings.numberOfTimerSteps;
					seek.ProgressChanged += (sender, e)=>{ text.Text = seek.Progress.ToString(); };

					new AlertDialog.Builder(this).SetTitle("Number of timer steps")
						.SetView(linear)
						.SetPositiveButton("OK", (sender, e)=>
						{
							settings.numberOfTimerSteps = seek.Progress;
							label.Text = settings.numberOfTimerSteps.ToString();
							MainActivity.main.UpdateTimerStepButtons();
						})
						.SetNegativeButton("Cancel", (sender, e)=>{})
						.Show();
				};
			}

			{
				var row = AddRow(list, addSeparator: false);
				row.AddChild(new TextView(this) {TextSize = largeTextSize, Text = "Time increment for timer steps"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				label.Text = settings.timeIncrementForTimerSteps + " minutes";
				row.Click += delegate
				{
					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = settings.timeIncrementForTimerSteps + " minutes", Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 60});
					seek.Progress = settings.timeIncrementForTimerSteps;
					seek.ProgressChanged += (sender, e)=>{ text.Text = seek.Progress + " minutes"; };
					
					new AlertDialog.Builder(this).SetTitle("Time increment for timer steps")
						.SetView(linear)
						.SetPositiveButton("OK", (sender, e)=>
						{
							settings.timeIncrementForTimerSteps = seek.Progress;
							label.Text = settings.timeIncrementForTimerSteps + " minutes";
							MainActivity.main.UpdateTimerStepButtons();
						})
						.SetNegativeButton("Cancel", (sender, e)=>{})
						.Show();
				};
			}

			AddSeparator(list, "Session Types");
			// ==========

			{
				var row = AddRow_PercentRelativeLayout(list);
				row.AddChild(new TextView(this) {Text = "Untracked - graph export text", TextSize = largeTextSize}, new PercentRelativeLayout.LayoutParams().VAddRule(LayoutRules.AlignParentLeft).VAddRule(LayoutRules.AlignParentTop));
				var valuePreview = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new PercentRelativeLayout.LayoutParams().VAddRule(LayoutRules.AlignParentLeft).VAddRule(LayoutRules.AlignParentBottom));
				valuePreview.Text = settings.untracked_graphExportText;
				row.Click += delegate
				{
					var layout = new FrameLayout(this);
					layout.SetPadding(30, 30, 30, 30);
					var input = layout.AddChild(new EditText(this) {Text = settings.untracked_graphExportText});
					input.SetSingleLine(true);
					new AlertDialog.Builder(this).SetTitle("Untracked - graph export text")
						.SetView(layout)
						.SetPositiveButton("OK", (sender2, e2)=>
						{
							settings.untracked_graphExportText = input.Text;
							valuePreview.Text = settings.untracked_graphExportText;
						})
						.SetNegativeButton("Cancel", (sender2, e2)=>{})
						.Show();
				};
			}

			{
				var row = AddRow(list, V.WrapContent, addSeparator: false);
				//row.AddChild(new TextView(this) {Text = "Session Types", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 55));

				Action refreshSessionTypes = null;
				var buttonsRow = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Horizontal});
				var newButton = buttonsRow.AddChild(new Button(this) {Text = "Add session type"}, new LinearLayout.LayoutParams(0, V.WrapContent, .55f));
				newButton.Click += (sender, e)=>
				{
					var name = "SessionType_1".UntilMatchesX_Increment(newName=>!settings.sessionTypes.Any(a=>a.name == newName));
					settings.sessionTypes.Add(new SessionType(name));
					settings.selectedSessionTypeName = name;
					refreshSessionTypes();
					RefreshSelectedSessionTypeUI(settings, row);
					MainActivity.main.UpdateTimerStepButtons();
				};
				var upButton = buttonsRow.AddChild(new Button(this) {Enabled = false, Text = "Up"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .075f));
				upButton.Click += (sender, e)=>
				{
					var oldIndex = settings.sessionTypes.FindIndex(a=>a.name == settings.selectedSessionTypeName);
					var session = settings.sessionTypes[oldIndex];
					settings.sessionTypes.RemoveAt(oldIndex);
					settings.sessionTypes.Insert(oldIndex - 1, session);
					refreshSessionTypes();
					RefreshSelectedSessionTypeUI(settings, row);
					MainActivity.main.UpdateTimerStepButtons();
				};
				var downButton = buttonsRow.AddChild(new Button(this) {Enabled = false, Text = "Down"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .075f));
				downButton.Click += (sender, e)=>
				{
					var oldIndex = settings.sessionTypes.FindIndex(a=>a.name == settings.selectedSessionTypeName);
					var session = settings.sessionTypes[oldIndex];
					settings.sessionTypes.RemoveAt(oldIndex);
					settings.sessionTypes.Insert(oldIndex + 1, session);
					refreshSessionTypes();
					RefreshSelectedSessionTypeUI(settings, row);
					MainActivity.main.UpdateTimerStepButtons();
				};
				var renameButton = buttonsRow.AddChild(new Button(this) {Enabled = false, Text = "Rename"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .15f));
				renameButton.Click += (sender, e)=>
				{
					var layout = new FrameLayout(this);
					layout.SetPadding(30, 30, 30, 30);
					var input = layout.AddChild(new EditText(this) {Text = settings.selectedSessionTypeName});
					input.SetSingleLine(true);
					new AlertDialog.Builder(this).SetTitle("Rename session type")
						.SetView(layout)
						.SetPositiveButton("OK", (sender2, e2)=>
						{
							var newName = input.Text;
							settings.sessionTypes.First(a=>a.name == settings.selectedSessionTypeName).name = newName;
							settings.selectedSessionTypeName = newName;
							refreshSessionTypes();
							RefreshSelectedSessionTypeUI(settings, row);
							MainActivity.main.UpdateTimerStepButtons();
						})
						.SetNegativeButton("Cancel", (sender2, e2)=>{})
						.Show();
				};
				var deleteButton = buttonsRow.AddChild(new Button(this) {Enabled = false, Text = "Delete"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .15f));
				deleteButton.Click += (sender, e)=>
				{
					settings.sessionTypes.Remove(settings.sessionTypes.First(a=>a.name == settings.selectedSessionTypeName));
					refreshSessionTypes();
					RefreshSelectedSessionTypeUI(settings, row);
					MainActivity.main.UpdateTimerStepButtons();
				};
				
				var listView = row.AddChild(new VListView(this), new LinearLayout.LayoutParams(V.MatchParent, V.WrapContent));
				listView.ChoiceMode = ChoiceMode.Single;

				refreshSessionTypes = ()=>
				{
					listView.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItemSingleChoice, Android.Resource.Id.Text1, settings.sessionTypes.Select(a=>a.name).ToArray());
					//listView.SetSelection(settings.sessionTypes.FindIndex(a=>a.name == settings.selectedSessionTypeName))
					//listView.Post(()=>listView.SetSelection(settings.sessionTypes.FindIndex(a=>a.name == settings.selectedSessionTypeName)));
					listView.SetItemChecked(settings.sessionTypes.FindIndex(a=>a.name == settings.selectedSessionTypeName), true);
                    listView.ItemClick += (sender, e)=>
					{
						settings.selectedSessionTypeName = settings.sessionTypes[e.Parent.IndexOfChild(e.View)].name;
						RefreshSelectedSessionTypeUI(settings, row);
					};
				};
				refreshSessionTypes();
				RefreshSelectedSessionTypeUI(settings, row);
			}

			AddSeparator(list, "Hotkeys");
			// ==========
			
			{
				var row = AddRow(list);
				row.AddChild(new TextView(this) {TextSize = largeTextSize, Text = "Key hold length"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				Action updateLabelText = ()=>label.Text = settings.keyHoldLength == 0 ? "0 [instant]" : settings.keyHoldLength.ToString();
				updateLabelText();
                row.Click += delegate
				{
					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = settings.keyHoldLength.ToString(), Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 100});
					seek.Progress = (int)(settings.keyHoldLength * 10);
					seek.ProgressChanged += (sender, e)=>{ text.Text = seek.Progress == 0 ? "0 [instant]" : (seek.Progress / 10d).ToString("F1"); };

					new AlertDialog.Builder(this).SetTitle("Key hold length")
						.SetView(linear)
						.SetPositiveButton("OK", (sender, e)=>
						{
							settings.keyHoldLength = seek.Progress / 10d;
							updateLabelText();
						})
						.SetNegativeButton("Cancel", (sender, e)=>{})
						.Show();
				};
			}

			{
				var row = AddRow(list, vertical: false, addSeparator: false);
				var leftSide = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				leftSide.AddChild(new TextView(this) {TextSize = largeTextSize, Text = "Block unused keys"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = leftSide.AddChild(new TextView(this) {TextSize = smallTextSize, Text = "Block any key events not used by the hotkeys specified"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var rightSide = row.AddChild(new RelativeLayout(this), new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				var checkbox = rightSide.AddChild(new CheckBox(this) {Clickable = false, Checked = settings.blockUnusedKeys}, new RelativeLayout.LayoutParams(V.WrapContent, V.WrapContent).VAddRule(LayoutRules.AlignParentRight).VAddRule(LayoutRules.CenterVertical));
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
				var checkbox = rightSide.AddChild(new CheckBox(this) {Clickable = false, Checked = settings.blockMouseEvents}, new RelativeLayout.LayoutParams(V.WrapContent, V.WrapContent).VAddRule(LayoutRules.AlignParentRight).VAddRule(LayoutRules.CenterVertical));
				row.Click += delegate
				{
					checkbox.Checked = !checkbox.Checked;
					settings.blockMouseEvents = checkbox.Checked;
				};
			}*/

			{
				var row = AddRow(list, V.WrapContent, addSeparator: false);
				//row.AddChild(new TextView(this) {Text = "Hotkeys", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 55));

				var header = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Horizontal});
				header.AddChild(new TextView(this) {Text = "Key"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .45f));
				header.AddChild(new TextView(this) {Text = "Action"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .45f));
				header.AddChild(new TextView(this) {Text = ""}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .1f));
				
				var buttonsRow = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Horizontal});
				var addHotkeyButton = buttonsRow.AddChild(new Button(this) {Text = "Add hotkey"}, new LinearLayout.LayoutParams(0, V.WrapContent, .7f));
				addHotkeyButton.Click += (sender, e)=>
				{
					settings.hotkeys.Add(new Hotkey());
					RefreshHotkeysUI(settings, row);
				};
				var showRecentKeysButton = buttonsRow.AddChild(new Button(this) {Text = "(show recent keys)"}, new LinearLayout.LayoutParams(0, V.WrapContent, .3f));
				showRecentKeysButton.Click += (sender, e)=>MainActivity.main.ShowRecentKeys(this);
				RefreshHotkeysUI(settings, row);
			}
		}
		protected override void OnPause()
		{
			base.OnPause();

			MainActivity.main.SaveMainData();
		}

		void RefreshSelectedSessionTypeUI(Settings settings, LinearLayout propsUI)
		{
			var upButton = propsUI.GetChild<LinearLayout>(0).GetChild<Button>(1);
			var downButton = propsUI.GetChild<LinearLayout>(0).GetChild<Button>(2);
			var renameButton = propsUI.GetChild<LinearLayout>(0).GetChild<Button>(3);
			var deleteButton = propsUI.GetChild<LinearLayout>(0).GetChild<Button>(4);

			var sessionType = settings.sessionTypes.FirstOrDefault(a=>a.name == settings.selectedSessionTypeName);
			while (propsUI.ChildCount > 2) // while there are controls other than the button-row and list-view (i.e. when there are old selected-session-type controls)
				propsUI.RemoveViewAt(2);

			upButton.Enabled = sessionType != null && settings.sessionTypes.IndexOf(sessionType) > 0;
			downButton.Enabled = sessionType != null && settings.sessionTypes.IndexOf(sessionType) < settings.sessionTypes.Count - 1;
			renameButton.Enabled = sessionType != null; //&& selectedSessionType.name != "Rest" && selectedSessionType.name != "Work";
			deleteButton.Enabled = sessionType != null;
			if (sessionType == null) // if no session-type selected, don't show the selected-session-type ui ("dude, that's kind of obvious...")
				return;

			{
				var row = AddRow(propsUI, vertical: false);
				var leftSide = row.AddChild(new LinearLayout(this) {Orientation = Orientation.Vertical}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				leftSide.AddChild(new TextView(this) {TextSize = largeTextSize, Text = "Color"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var rightSide = row.AddChild(new RelativeLayout(this), new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .5f));
				var colorPreview = rightSide.AddChild(new ImageView(this),
					new RelativeLayout.LayoutParams(50, 50).VAddRule(LayoutRules.AlignParentRight).VAddRule(LayoutRules.CenterVertical));
				colorPreview.Background = new BorderDrawable(Color.Black, 3, 3, 3, 3);
				//colorPreview.SetBackgroundColor(sessionType.color);
				colorPreview.SetPadding(3, 3, 3, 3);
				colorPreview.SetImageDrawable(Drawables.CreateColor(sessionType.color));
				row.Click += delegate
				{
					var dialog = new ColorPickerDialog(this, sessionType.color);
					dialog.OnOK += color=>
					{
						sessionType.color = color;
						RefreshSelectedSessionTypeUI(settings, propsUI);
					};
					dialog.show();
				};
			}

			{
				//var row = AddRow(propsUI, vertical: false);
				var row = AddRow_PercentRelativeLayout(propsUI);
				row.AddChild(new TextView(this) {Text = "Graph export text", TextSize = largeTextSize }, new PercentRelativeLayout.LayoutParams().VAddRule(LayoutRules.AlignParentLeft).VAddRule(LayoutRules.AlignParentTop));
				var valuePreview = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new PercentRelativeLayout.LayoutParams().VAddRule(LayoutRules.AlignParentLeft).VAddRule(LayoutRules.AlignParentBottom));
				valuePreview.Text = sessionType.graphExportText;
				row.Click += delegate
				{
					var layout = new FrameLayout(this);
					layout.SetPadding(30, 30, 30, 30);
					var input = layout.AddChild(new EditText(this) {Text = sessionType.graphExportText });
					input.SetSingleLine(true);
					new AlertDialog.Builder(this).SetTitle("Graph export text")
						.SetView(layout)
						.SetPositiveButton("OK", (sender2, e2)=>
						{
							sessionType.graphExportText = input.Text != "" ? input.Text : null;
							valuePreview.Text = sessionType.graphExportText;
						})
						.SetNegativeButton("Cancel", (sender2, e2)=>{})
						.Show();
				};
			}

			{
				var row = AddRow(propsUI);
				row.AddChild(new TextView(this) {TextSize = largeTextSize, Text = "Set master alarm volume to X, before sounding alarm"}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				Func<string> getText = ()=>sessionType.setMasterAlarmVolume == -1 ? "[don't set]" : sessionType.setMasterAlarmVolume + "%";
				label.Text = getText();
				row.Click += delegate
				{
					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = getText(), Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					var minValue = -1;
					SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 100 - minValue});
					seek.SetValue(minValue, sessionType.setMasterAlarmVolume);
					seek.ProgressChanged += (sender, e)=>text.Text = seek.GetValue(minValue) == -1 ? "[don't set]" : seek.GetValue(minValue) + "%";

					new AlertDialog.Builder(this).SetTitle("Set master alarm volume to X, before sounding alarm")
						.SetView(linear)
						.SetPositiveButton("OK", (sender, e)=>
						{
							sessionType.setMasterAlarmVolume = seek.GetValue(minValue);
							label.Text = getText();
						})
						.SetNegativeButton("Cancel", (sender, e)=>{})
						.Show();
				};
			}

			{
				//var alarmSoundPanel = root.Append(new LinearLayout(this) {Orientation = Orientation.Vertical}, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, 50));
				var row = AddRow(propsUI);
				row.AddChild(new TextView(this) {Text = "Alarm sound", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				label.Text = sessionType.alarmSoundFilePath != null && new FileInfo(sessionType.alarmSoundFilePath).Exists ? sessionType.alarmSoundFilePath : "[none]";
				row.Click += delegate
				{
					FileChooserDialog dialog = new FileChooserDialog(this);
					if (sessionType.alarmSoundFilePath != null && new FileInfo(sessionType.alarmSoundFilePath).Directory.Exists)
						dialog.loadFolder(new FileInfo(sessionType.alarmSoundFilePath).Directory.FullName);
					dialog.addListener((file, create)=>
					{
						sessionType.alarmSoundFilePath = file.Path;
						label.Text = sessionType.alarmSoundFilePath != null && new FileInfo(sessionType.alarmSoundFilePath).Exists ? sessionType.alarmSoundFilePath : "[none]";
						dialog.Dismiss();

						//MainActivity.main.SaveSettings();
						//Reload();
					});
					dialog.Show();
				};
			}

			{
				var row = AddRow(propsUI);
				row.AddChild(new TextView(this) {Text = "Min volume", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				label.Text = sessionType.minVolume + "%";
				row.Click += delegate
				{
					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = sessionType.minVolume + "%", Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					SeekBar seek = linear.AddChild(new SeekBar(this));
					seek.Progress = sessionType.minVolume;
					seek.ProgressChanged += (sender, e)=>text.Text = seek.Progress + "%";

					new AlertDialog.Builder(this).SetTitle("Min volume")
						.SetView(linear)
						.SetPositiveButton("OK", (sender, e)=>
						{
							sessionType.minVolume = seek.Progress;
							label.Text = sessionType.minVolume + "%";
						})
						.SetNegativeButton("Cancel", (sender, e)=>{})
						.Show();
				};
			}

			{
				var row = AddRow(propsUI);
				row.AddChild(new TextView(this) {Text = "Max volume", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				label.Text = sessionType.maxVolume + "%";
				row.Click += delegate
				{
					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = sessionType.maxVolume + "%", Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					SeekBar seek = linear.AddChild(new SeekBar(this));
					seek.Progress = sessionType.maxVolume;
					seek.ProgressChanged += (sender, e)=>text.Text = seek.Progress + "%";

					new AlertDialog.Builder(this).SetTitle("Max volume")
						.SetView(linear)
						.SetPositiveButton("OK", (sender, e)=>
						{
							sessionType.maxVolume = seek.Progress;
							label.Text = sessionType.maxVolume + "%";
						})
						.SetNegativeButton("Cancel", (sender, e)=>{})
						.Show();
				};
			}

			{
				var row = AddRow(propsUI, addSeparator: false);
				row.AddChild(new TextView(this) {Text = "Time to max volume", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				var label = row.AddChild(new TextView(this) {TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
				label.Text = sessionType.timeToMaxVolume + " minutes";
				row.Click += delegate
				{
					LinearLayout linear = new LinearLayout(this) {Orientation = Orientation.Vertical};
					var text = linear.AddChild(new TextView(this) {Text = sessionType.timeToMaxVolume + " minutes", Gravity = GravityFlags.CenterHorizontal});
					text.SetPadding(10, 10, 10, 10);
					SeekBar seek = linear.AddChild(new SeekBar(this) {Max = 60});
					seek.Progress = sessionType.timeToMaxVolume;
					seek.ProgressChanged += (sender, e)=>text.Text = seek.Progress + " minutes";

					new AlertDialog.Builder(this).SetTitle("Time to max volume")
						.SetView(linear)
						.SetPositiveButton("OK", (sender, e)=>
						{
							sessionType.timeToMaxVolume = seek.Progress;
							label.Text = sessionType.timeToMaxVolume + " minutes";
						})
						.SetNegativeButton("Cancel", (sender, e)=>{})
						.Show();
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
					builder.SetPositiveButton("OK", (sender2, e2)=>
					{
						ListView listView = dialog.ListView;
						settings.volumeFadeType = (VolumeScaleType)Enum.Parse(typeof(VolumeScaleType), listView.Adapter.GetItem(listView.CheckedItemPosition).ToString());
						label.Text = settings.volumeFadeType.ToString();
					});
					builder.SetNegativeButton("Cancel", (sender2, e2)=>{});
					dialog = builder.Show();
				};
			}*/
		}
		void RefreshHotkeysUI(Settings settings, LinearLayout row)
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
					builder.SetPositiveButton("OK", (sender2, e2)=>
					{
						ListView listView = dialog.ListView;
						hotkey.key = (Keycode)Enum.Parse(typeof(Keycode), listView.Adapter.GetItem(listView.CheckedItemPosition).ToString());
						RefreshHotkeysUI(settings, row);
					});
					builder.SetNegativeButton("Cancel", (sender2, e2)=>{});
					dialog = builder.Show();
				};
				var actionLabel = row2.AddChild(new Button(this), new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .45f));
				actionLabel.Text = hotkey.action + (hotkey.action == HotkeyAction.StartSession ? $" ({hotkey.action_startSession_type}, {hotkey.action_startSession_length} minutes)" : "");
				actionLabel.Click += (sender, e)=>
				{
					AlertDialog dialog = null;
					var builder = new AlertDialog.Builder(this);
					builder.SetTitle("Action");
					var actions = Enum.GetValues(typeof(HotkeyAction)).OfType<HotkeyAction>().ToList();
					builder.SetSingleChoiceItems(actions.Select(a=>a.ToString()).ToArray(), actions.IndexOf(hotkey.action), (sender2, e2)=>{});
					builder.SetPositiveButton("OK", (sender2, e2)=>
					{
						ListView listView = dialog.ListView;
						hotkey.action = (HotkeyAction)Enum.Parse(typeof(HotkeyAction), listView.Adapter.GetItem(listView.CheckedItemPosition).ToString());
						if (hotkey.action == HotkeyAction.StartSession)
						{
							var layout = new PercentRelativeLayout(this);
							layout.SetPadding(30, 30, 30, 30);

							var typeLabel = layout.AddChild(new TextView(this) {Text = "Type"}, new PercentRelativeLayout.LayoutParams().VAct(a=>a.PercentLayoutInfo.widthPercent = .5f));
							var typeSpinner = layout.AddChild(new Spinner(this), new PercentRelativeLayout.LayoutParams().VAddRule(LayoutRules.RightOf, typeLabel.Id).VAct(a=>a.PercentLayoutInfo.widthPercent = .5f));
							typeSpinner.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, settings.sessionTypes.Select(a=>a.name).ToArray());
							typeSpinner.SetSelection(settings.sessionTypes.Select(a=>a.name).ToList().IndexOf(hotkey.action_startSession_type));

							var lengthLabel = layout.AddChild(new TextView(this) {Gravity = GravityFlags.Center}, new PercentRelativeLayout.LayoutParams(V.MatchParent, V.WrapContent).VAddRule(LayoutRules.Below, typeLabel.Id));
							SeekBar seek = layout.AddChild(new SeekBar(this) {Max = 180}, new PercentRelativeLayout.LayoutParams(V.MatchParent, V.WrapContent).VAddRule(LayoutRules.Below, lengthLabel.Id));
							seek.Progress = hotkey.action_startSession_length;
							Action updateText = ()=>lengthLabel.Text = "Length: " + seek.Progress + " minutes";
							updateText();
                            seek.ProgressChanged += (sender3, e3)=>updateText();

							new AlertDialog.Builder(this).SetCancelable(false).SetTitle("Start timer - length")
								.SetView(layout)
								.SetPositiveButton("OK", (sender3, e3)=>
								{
									hotkey.action_startSession_type = settings.sessionTypes[typeSpinner.SelectedItemPosition].name;
									hotkey.action_startSession_length = seek.Progress;
									RefreshHotkeysUI(settings, row);
								})
								.Show();
						}
						else
							RefreshHotkeysUI(settings, row);
					});
					builder.SetNegativeButton("Cancel", (sender2, e2)=>{});
					dialog = builder.Show();
				};
				var deleteButton = row2.AddChild(new Button(this) {Text = "Delete"}, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, .1f));
				deleteButton.Click += (sender, e)=>
				{
					settings.hotkeys.Remove(hotkey);
					RefreshHotkeysUI(settings, row);
				};
			}
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

			/*var rect = new RectShape();
            var shape = new ShapeDrawable(rect);
			shape.Paint.Color = new Color(255, 255, 255, 200);
			shape.Paint.SetStyle(Paint.Style.Stroke);
			shape.Paint.StrokeWidth = 3;
			//shape.SetPadding(3, 3, 3, 3);
			//result.Background = shape;
			result.Background = new InsetDrawable(shape, -3, -3, -3, 3);
			//result.SetPadding(3, 3, 3, 3); // must come after*/
			result.Background = new BorderDrawable(new Color(255, 255, 255, 200), 0, 0, 0, 3);
			//result.SetPadding(0, 0, 0, 5);

			return result;
		}
		LinearLayout AddRow(LinearLayout root, int height = 110, bool vertical = true, bool addSeparator = true)
		{
			var result = root.AddChild(new LinearLayout(this) {Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal}, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, height));
			//if (root.ChildCount > 1)
			if (addSeparator)
				result.Background = new BorderDrawable(new Color(255, 255, 255, 128), 0, 0, 0, 1);
				//result.SetPadding(0, 0, 0, 1);
			result.SetPadding(15, 15, 15, 15); // must come after
			return result;
		}
		PercentRelativeLayout AddRow_PercentRelativeLayout(LinearLayout root, int height = 110, bool addSeparator = true)
		{
			var result = root.AddChild(new PercentRelativeLayout(this), new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, height));
			if (addSeparator)
				result.Background = new BorderDrawable(new Color(255, 255, 255, 128), 0, 0, 0, 1);
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