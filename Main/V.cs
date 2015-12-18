using System;
using System.Threading;

public static class V
{
	public static void Nothing(params object[] args) {}

	public static double Clamp(double min, double max, double val) { return Math.Min(max, Math.Max(min, val)); }
	public static double Lerp(double a, double b, double percentFromAToB) { return a + ((b - a) * percentFromAToB); }

	// android ui constants
	public const int WrapContent = -2;
	public const int MatchParent = -1;

	public static void WaitXSecondsThenRun(double x, Action action)
	{
		Thread thread = null;
		thread = new Thread(()=>
		{
			Thread.Sleep((int)(x * 1000));
			action();
			thread.Abort();
		});
		thread.Start();
	}
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

public static class VDFExtensions
{
	public static void Init() {} // forces the static initializer below to run
	static VDFExtensions() // one time registration of custom exporters/importers/tags
	{
		// type exporter-importer pairs
		// ==========

		//VDFTypeInfo.AddSerializeMethod<Guid>(a=>a.ToString());
		//VDFTypeInfo.AddDeserializeMethod_FromParent<Guid>(node=>new Guid(node));

		VDFTypeInfo.AddSerializeMethod<DateTime>(a=>a.Ticks_Milliseconds());
		VDFTypeInfo.AddDeserializeMethod_FromParent<DateTime>(node=>new DateTime(node * TimeSpan.TicksPerMillisecond));
		VDFTypeInfo.AddSerializeMethod<DateTime?>(a=>a?.Ticks_Milliseconds() ?? -1);
		VDFTypeInfo.AddDeserializeMethod_FromParent<DateTime?>(node=>node.primitiveValue != null ? new DateTime(node * TimeSpan.TicksPerMillisecond) : (DateTime?)null);
	}
}