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
using Java.IO;

public static class ClassExtensions
{
	public static T Append<T>(this ViewGroup s, T view, ViewGroup.LayoutParams layout = null) where T : View
	{
		if (layout != null)
			s.AddView(view, layout);
		else
			s.AddView(view);
		return view;
	}

	// File
	public static File GetFile(this File s, string name) { return new File(s, name); }
}