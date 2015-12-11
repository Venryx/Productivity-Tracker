using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using File = Java.IO.File;

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
}