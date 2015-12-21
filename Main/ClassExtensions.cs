using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using File = Java.IO.File;
using Path = System.IO.Path;
using Uri = Android.Net.Uri;

public static class ClassExtensions
{
	// ViewGroup
	public static T AddChild<T>(this ViewGroup s, T view, ViewGroup.LayoutParams layout = null, int index = -1) where T : View
	{
		//if (index != -1)
		if (layout != null)
			s.AddView(view, index, layout);
		else
			s.AddView(view, index);
		/*else
			if (layout != null)
				s.AddView(view, layout);
			else
				s.AddView(view);*/
		return view;
	}
	public static List<View> GetChildren(this ViewGroup s)
	{
		var result = new List<View>();
		for (var i = 0; i < s.ChildCount; i++)
			result.Add(s.GetChildAt(i));
		return result;
	}

	// int
	//public static int Minus(this int s, int amount, int modulus) { return (s - amount) % 7; }
	public static int Sign(this int s) { return s >= 0 ? 1 : -1; }
	public static int Modulus(this int s, int modulus, bool keepSignOfFirst = false, bool keepSignOfSecond = true)
	{
		int result = s % modulus;
		if (keepSignOfFirst && result.Sign() != s.Sign())
			result = -result;
		else if (keepSignOfSecond && result.Sign() != modulus.Sign())
			result = -result;
		return result;
	}

	// double
	/*public static double FloorToMultipleOf(this double s, double val) { return Math.Floor(s / val) * val; }
	public static double RoundToMultipleOf(this double s, double val) { return Math.Round(s / val) * val; }
	public static double CeilingToMultipleOf(this double s, double val) { return Math.Ceiling(s / val) * val; }
	public static double Modulus(this double s, double modulus, bool keepSignOfFirst = false, bool keepSignOfSecond = true)
	{
		double result = s % modulus;
		if (keepSignOfFirst && result >= 0 != s >= 0) //result.Sign() != s.Sign())
			result = -result;
		else if (keepSignOfSecond && result >= 0 != modulus >= 0) //result.Sign() != modulus.Sign())
			result = -result;
		return result;
	}
	public static double DivideBy(this double s, double other, bool keepSignOfFirst = true, bool keepSignOfSecond = false)
	{
		double result = s / other;
		if (keepSignOfFirst && result >= 0 != s >= 0) //result.Sign() != s.Sign())
			result = -result;
		else if (keepSignOfSecond && result >= 0 != other >= 0) //result.Sign() != other.Sign())
			result = -result;
		return result;
	}
	public static double ToPower(this double s, double power) { return Math.Pow(s, power); }
	//public static bool EqualsAbout(this double s, double val) { return Math.Abs(s - val) / Math.Max(Math.Abs(s), Math.Abs(val)) <= double.Epsilon; }
	public static bool EqualsAbout(this double s, double val, double maxDifForEquals = .000000000000001) { return Math.Abs(s - val) <= maxDifForEquals; }*/

	// long
	/*public static double FloorToMultipleOf(this long s, double val) { return Math.Floor(s / val) * val; }
	public static double RoundToMultipleOf(this long s, double val) { return Math.Round(s / val) * val; }
	public static double CeilingToMultipleOf(this long s, double val) { return Math.Ceiling(s / val) * val; }*/

	// string
	public static string TrimStart(this string s, int length) { return s.Substring(length); }
	public static string TrimEnd(this string s, int length) { return s.Substring(0, s.Length - length); }
	public static string SubstringSE(this string self, int startIndex, int stopIndex) { return self.Substring(startIndex, stopIndex - startIndex); }
	public static int XthIndexOf(this string s, string str, int x)
	{
		var currentPos = -1;
		for (var i = 0; i <= x; i++)
		{
			var subIndex = s.IndexOf(str, currentPos + 1);
			if (subIndex == -1)
				return -1; // no such xth index
			currentPos = subIndex;
		}
		return currentPos;
	}

	// DateTime
	//public static long TotalTicks(this DateTime s) { return s.Ticks; }
	public static long TotalMilliseconds(this DateTime s) { return s.Ticks / TimeSpan.TicksPerMillisecond; }
	public static long TotalDays(this DateTime s) { return s.Ticks / TimeSpan.TicksPerDay; }
	public static string ToString_U_Date(this DateTime s)
	{
		var result = s.ToString("u");
		result = result.Substring(0, result.IndexOf(" "));
		return result;
	}
	public static string ToString_U_Time(this DateTime s)
	{
		var result = s.ToString("u");
		result = result.SubstringSE(result.IndexOf(" ") + 1, result.IndexOf("Z"));
		return result;
	}
	public static string ToString_U(this DateTime s)
	{
		var result = s.ToString("u");
		result = result.Substring(0, result.IndexOf("Z"));
		return result;
	}
	/*public static DateTime ClosestDate(this DateTime s) // rounds to the nearest date
	{
		if (s.Hour < 12)
			return s.Date;
		return s.AddDays(1).AddHours(3).Date;
	}*/
	public static DateTime AddDaysTillDayContainsX(this DateTime s, DateTime x)
	{
		var result = s;
		while (result < x) // add days till at-or-after x
			result = result.AddDays(1);
		while (result >= x) // remove days till before x (thus being 1-or-less days before x, i.e. our day contains x)
			result = result.AddDays(-1);
		return result;
	}
	public static DateTime AddDaysTillWithinDayX(this DateTime s, DateTime day)
	{
		var result = s;
		while (result.TotalDays() < day.TotalDays()) // add days till at-or-after day-x-start
			result = result.AddDays(1);
		while (result.TotalDays() >= day.AddDays(1).TotalDays()) // remove days till before day-x-stop (thus our being within day-x)
			result = result.AddDays(-1);
		return result;
	}
	/*public static DateTime AddDays_PreserveHour(this DateTime s, int days)
	{
		var result = s.AddDays(days);

		var s_dayOfWeek = (int)s.DayOfWeek;
		var result_expectedDayOfWeek = (s_dayOfWeek - days).Modulus(7);
		if ((int)result.DayOfWeek == result_expectedDayOfWeek)
			result = result.Date.AddHours(s.Hour);
		else // if day-of-week was not what we expected, the AddDays(days) must have hopped over a 23 hour day
			result = result.Date.AddHours(24 + 3).Date.AddHours(s.Hour);
		return result;
	}*/

	// File
	public static File GetFile(this File s, string name) { return new File(s, name); }

	// IEnumerable<T>
	public static string JoinUsing(this IEnumerable list, string separator) { return string.Join(separator, list.OfType<object>().ToArray()); }

	// Dictionary<TKey, TValue>
	public static void AddDictionary<TKey, TValue>(this Dictionary<TKey, TValue> s, Dictionary<TKey, TValue> other)
	{
		foreach (TKey key in other.Keys)
			s.Add(key, other[key]);
	}

	// Array
	public static bool HasIndex(this Array array, int index) { return index >= 0 && index < array.Length; }
	public static bool HasIndex(this Array array, int index0, int index1) { return index0 >= 0 && index0 < array.GetLength(0) && index1 >= 0 && index1 < array.GetLength(1); }

	// List<T>
	public static bool HasIndex<T>(this List<T> list, int index) { return index >= 0 && index < list.Count; }
	//public static T GetValueOrNull<T>(this List<T> list, int index) where T : class { return index >= 0 && index < list.Count ? list[index] : null; }
	//public static T GetValueOrDefault<T>(this List<T> list, int index, T defaultValue = default(T)) { return index >= 0 && index < list.Count ? list[index] : defaultValue; }
	public static T GetValue<T>(this List<T> list, int index, T defaultValue = default(T)) { return index >= 0 && index < list.Count ? list[index] : defaultValue; }

	// MatchCollection
	public static List<Match> ToList(this MatchCollection obj)
	{
		var result = new List<Match>();
		for (int i = 0; i < obj.Count; i++)
			result.Add(obj[i]);
		return result;
	}

	// DirectoryInfo
	public static DirectoryInfo VCreate(this DirectoryInfo folder) { folder.Create(); return folder; }
	public static DirectoryInfo GetFolder(this DirectoryInfo folder, string subpath) { return new DirectoryInfo(folder.FullName + (subpath != null && subpath.StartsWith("/") ? "" : "/") + subpath); }
	public static string GetSubpathOfDescendent(this DirectoryInfo folder, DirectoryInfo descendent) { return descendent.FullName.Substring(folder.FullName.Length); }
	public static string GetSubpathOfDescendent(this DirectoryInfo folder, FileInfo descendent) { return descendent.FullName.Substring(folder.FullName.Length); }
	public static FileInfo GetFile(this DirectoryInfo folder, string subpath) { return new FileInfo(folder.FullName + (subpath != null && subpath.StartsWith("/") ? "" : "/") + subpath); }
	public static void CopyTo(this DirectoryInfo source, DirectoryInfo target)
	{
		if (source.FullName == target.FullName)
			throw new Exception("Source and destination cannot be the same.");
		// fix for if root-call folder has files but not folders
		if (!target.Exists)
			target.Create();
		foreach (DirectoryInfo dir in source.GetDirectories())
			dir.CopyTo(target.CreateSubdirectory(dir.Name));
		foreach (FileInfo file in source.GetFiles())
			file.CopyTo(Path.Combine(target.FullName, file.Name));
	}

	// FileInfo
	public static FileInfo CreateFolders(this FileInfo s) { s.Directory.Create(); return s; }
	public static File ToFile(this FileInfo s) { return new File(s.FullName); }
	public static string NameWithoutExtension(this FileInfo s) { return Path.GetFileNameWithoutExtension(s.Name); }

	// File
	public static Uri ToURI_Android(this File s) { return Uri.FromFile(s); }

	public static Action InvokeThenReturn(this Action s) { s.Invoke(); return s; }

	// View
	public static FrameLayout GetRootFrameLayout(this View s) { return (FrameLayout)((ViewGroup)((ViewGroup)s.RootView).GetChildAt(0)).GetChildAt(0); }
	public static Vector2i GetPositionFrom(this View s, View fromControl = null)
	{
		if (fromControl != null)
			return s.GetPositionFrom() - fromControl.GetPositionFrom();
		var x = 0;
		var y = 0;
		View currentChild = s;
		while (true)
		{
			x += currentChild.Left;
			y += currentChild.Top;
			if (!(currentChild.Parent is View) || currentChild.Parent == currentChild.GetRootFrameLayout())
				break;
			currentChild = (View)currentChild.Parent;
		}
		return new Vector2i(x, y);
	}
	// uses the built-in View.Tag system prop (as opposed to generic VMeta system)
	public static void VTag(this View s, object value) { s.Tag = new ObjectWrapper(value); }
	public static object VTag(this View s) { return (s.Tag as ObjectWrapper)?.value; }
	public static T VTag<T>(this View s) { return (T)s.VTag(); }

	// SeekBar
	public static int GetValue(this SeekBar s, int minValue) { return minValue + s.Progress; }
	public static void SetValue(this SeekBar s, int minValue, int value) { s.Progress = value - minValue; }

	// Color
	public static Color NewA(this Color s, byte alpha) { return new Color(s.R, s.G, s.B, alpha); }

	// ColorDrawable
	public static ColorDrawable Clone(this ColorDrawable s) { return new ColorDrawable(s.Color); }

	// Canvas
	public static RectF GetRect(this Canvas s) { return new RectF(0, 0, s.Width, s.Height); }

	// RectF
	public static RectF Times(this RectF s, float width, float height) { return new RectF(s.Left * width, s.Top * height, s.Right * width, s.Bottom * height); }
	public static RectF Extend(this RectF s, float left, float top, float right, float bottom) { return new RectF(s.Left + left, s.Top + top, s.Right + right, s.Bottom + bottom); }

	// MediaPlayer
	// minor: maybe make-so: it's known for sure what the MediaPlayer treats passed values as!
	// > for now, am just going with what seems to be the case: that the SetVolume method already does, in fact, just expect the loudness value on a linear scale!
	// > (found this through careful listening, after writing this whole thing)
	/*public enum VolumeScaleType
	{
		//Energy, // what MediaPlayer possibly treats passed values as
		Amplitude, // what MediaPlayer most likely treats passed values as
		Loudness // what people treat everyday volume values as (as in "that sounded 2 times as loud")
	}
	/*public static void VSetVolume(this MediaPlayer s, double volume, VolumeScaleType volumeType = VolumeScaleType.Loudness)
	{
		const int maxVolume = 100;
		var volume_toScale = volume * maxVolume;
		double volume_scalar = volumeType == VolumeScaleType.Amplitude ? volume : (1 - (Math.Log(maxVolume - volume_toScale) / Math.Log(maxVolume)));
		s.SetVolume((float)volume_scalar, (float)volume_scalar);
	}*#/
	public static void VSetVolume(this MediaPlayer s, double volume, VolumeScaleType volumeType = VolumeScaleType.Loudness)
	{
		// Links:
		// 1) http://en.wikipedia.org/wiki/Decibel
		// 2) http://trace.wisc.edu/docs/2004-About-dB
		// 3) http://hyperphysics.phy-astr.gsu.edu/hbase/sound/loud.html
		// 4) http://www.animations.physics.unsw.edu.au/jw/dB.htm
		// 5) http://www.soundmaskingblog.com/2012/06/saved_by_the_bell
		// 6) http://www.campanellaacoustics.com/faq.html
		// 7) http://physics.stackexchange.com/questions/9113/how-sound-intensity-db-and-sound-pressure-level-db-are-related
		// 8) http://www.sengpielaudio.com/calculator-loudness.htm (note: page uses terms 'power/intensity' and 'pressure' differently; power/intensity: for whole shell at distance, pressure: field-quantity?)
		// basic idea: you can think of one decibel (of gain), + or -, as *translating into* the given changes-in/multipliers-for energy, amplitude, or loudness
		// (i.e. one decibel provides a specific amount to multiply energy, amplitude, and loudness values, such that they remain aligned realistically)
		// note: the 'one decibel' unit is set up to correspond roughly to a change in loudness just substantial enough to be noticeable
		// note: the 'quietest perceivable sound' example (standard) base has these absolute values: 'e' is 1 pico-watt per square-foot, 'a' is 20 micropascals, 'l' is the quietest-perceivable-loudness

		// references (for q.p.s. base)   | db (gain) | energy           | amplitude            | loudness
		// ===============================================================================================
		// actual silence                 | -inf      | 0                | 0                    | 0
		// (a seeming silence)            | -20       | e / 100          | a / 10               | 0 (would be l / 4, if 'l' weren't already for the quietest-perceivable-sound)
		// (a seeming silence)            | -10       | e / 10           | a / 3.16227/sqrt(10) | 0 (would be l / 2, if 'l' weren't already for the quietest-perceivable-sound)
		// quietest perceivable sound     | 0         | e                | a                    | l
		// ?                              | 1         | e * 1.258925     | a * 1.122018         | l * 1.071773
		// rustling leaves                | 10        | e * 10           | a * 3.16227/sqrt(10) | l * 2
		// whisper, or rural nighttime    | 20        | e * 100          | a * 10               | l * 4
		// watch ticking                  | 30        | e * 1000         | a * 31.622/sqrt(100) | l * 8
		// quiet speech, or rural daytime | 40        | e * 10000        | a * 100              | l * 16
		// dishwasher in next room        | 50        | e * 100000       | a * 316/sqrt(100000) | l * 32
		// ordinary conversation          | 60        | e * 1000000      | a * 1000             | l * 64
		// ===============================================================================================

		// assuming MediaPlayer.SetVolume treats passed values as Amplitude
		Func<double, double> convertLoudnessToAmplitude = loudness=>Math.Pow(10, Math.Log(loudness, 4));
		var volume_amplitude = volumeType == VolumeScaleType.Amplitude ? volume : convertLoudnessToAmplitude(volume);
		s.SetVolume((float)volume_amplitude, (float)volume_amplitude);
		// assuming MediaPlayer.SetVolume treats passed values as Energy
		//Func<double, double> convertLoudnessToEnergy = loudness=>Math.Pow(100, Math.Log(loudness, 4));
		//var volume_energy = volumeType == VolumeScaleType.Energy ? volume : convertLoudnessToEnergy(volume);
		//s.SetVolume((float)volume_energy, (float)volume_energy);
	}*/
}