using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using File = Java.IO.File;
using Uri = Android.Net.Uri;

public static class ClassExtensions
{
	public static T AddChild<T>(this ViewGroup s, T view, ViewGroup.LayoutParams layout = null, int index = -1) where T : View
	{
		if (layout != null)
			s.AddView(view, index, layout);
		else
			s.AddView(view, index);
		return view;
	}

	// double
	public static double ToPower(this double s, double power) { return Math.Pow(s, power); }

	// File
	public static File GetFile(this File s, string name) { return new File(s, name); }

	// IEnumerable<T>
	public static string JoinUsing(this IEnumerable list, string separator) { return String.Join(separator, list.Cast<string>().ToArray()); }

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

	// File
	public static Uri ToURI_Android(this File s) { return Uri.FromFile(s); }

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
	
	// ColorDrawable
	public static ColorDrawable Clone(this ColorDrawable s) { return new ColorDrawable(s.Color); }

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