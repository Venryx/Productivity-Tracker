using System;
using Android.App;
using Android.Content;
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

			// set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			// get our button from the layout resource, and attach an event to it
			var button = FindViewById<Button>(Resource.Id.MainButton);

			button.Click += delegate { button.Text = $"{++count} clicks!"; };
		}
	}
}