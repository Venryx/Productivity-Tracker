using System;

public static class V
{
	public static void Nothing(params object[] args) {}

	public static double Clamp(double min, double max, double val) { return Math.Min(max, Math.Max(min, val)); }
	public static double Lerp(double a, double b, double percentFromAToB) { return a + ((b - a) * percentFromAToB); }
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
		//Console.WriteLine(message);
		//System.Diagnostics.Debug.WriteLine(message, tag);
		if (type == DebugType.Info)
			Android.Util.Log.Info(tag, message);
		else if (type == DebugType.Warning)
			Android.Util.Log.Warn(tag, message);
		else //if (type == DebugType.Error)
			Android.Util.Log.Error(tag, message);
	}
}