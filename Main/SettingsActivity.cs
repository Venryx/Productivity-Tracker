using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Main
{
	[Activity(Label = "Settings")]
	public class SettingsActivity : Activity
	{
		const int largeTextSize = 15;
		const int smallTextSize = 12;
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			//SetContentView(Resource.Layout.Settings);

			var root = new LinearLayout(this) {Orientation = Orientation.Vertical};
			SetContentView(root, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

			//var alarmSoundPanel = root.Append(new LinearLayout(this) {Orientation = Orientation.Vertical}, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, 50));
			var alarmSoundPanel = AddRow(root);
			alarmSoundPanel.Append(new TextView(this) {Text = "Alarm sound", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
			alarmSoundPanel.Append(new TextView(this) {Text = "[none]", TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
			alarmSoundPanel.Click += delegate
			{
				// todo; break point
			};

			var maxVolumePanel = AddRow(root);
			maxVolumePanel.Append(new TextView(this) {Text = "Max volume", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
			maxVolumePanel.Append(new TextView(this) {Text = "100%", TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));

			var timeToMaxVolumePanel = AddRow(root);
			timeToMaxVolumePanel.Append(new TextView(this) {Text = "Time to max volume", TextSize = largeTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
			timeToMaxVolumePanel.Append(new TextView(this) {Text = "30:30", TextSize = smallTextSize}, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, .5f));
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