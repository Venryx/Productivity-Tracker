using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace Main
{
	[Activity(Label = "Productivity Tracker", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		int count;
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			SetContentView(Resource.Layout.Main);

			var button = FindViewById<Button>(Resource.Id.Rest_100);
			button.Click += delegate { button.Text = $"{++count} clicks!"; };

			var timeLeftBar = FindViewById<ImageView>(Resource.Id.TimeLeftBar);
			var timeLeftBar_clip = (ClipDrawable)timeLeftBar.Drawable;
			timeLeftBar_clip.SetLevel((int)(10000 * .5));

			var timeOverBar = FindViewById<ImageView>(Resource.Id.TimeOverBar);
			var timeOverBar_clip = (ClipDrawable)timeOverBar.Drawable;
			timeOverBar_clip.SetLevel((int)(10000 * .5));

			var linearView = (FrameLayout)timeOverBar.Parent;
			var soundIconButton = new ImageButton(this);
			//soundIconButton.SetBackgroundColor(Color.Transparent);
			//soundIconButton.SetImageResource(Resource.Drawable.Volume);
			soundIconButton.SetBackgroundResource(Resource.Drawable.Volume);
			//soundIconButton.SetScaleType(ImageView.ScaleType.FitCenter);
			//soundIconButton.SetAdjustViewBounds(true);
			linearView.AddView(soundIconButton, new FrameLayout.LayoutParams(30, 30) { Gravity = GravityFlags.CenterVertical });
		}
	}
}