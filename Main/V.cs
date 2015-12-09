using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

public static class V
{
}
public enum DebugType
{
	Info,
	Warning,
	Debug
}
public static class VDebug
{
	public static void Log(string message, string tag = "main", DebugType type = DebugType.Info)
	{
		if (type == DebugType.Info)
			Android.Util.Log.Info(tag, message);
		else if (type == DebugType.Warning)
			Android.Util.Log.Warn(tag, message);
		else //if (type == DebugType.Error)
			Android.Util.Log.Error(tag, message);
	}
}