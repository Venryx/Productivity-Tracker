using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;
using Android.Graphics;
using FluentAssertions;
using Xunit;
using Random = System.Random;

namespace Tests
{
	public class GeneralTests
	{
		// general
		// ==========

		[Fact] void Regex() { new Regex("(^| )not | not( |$)").Replace("can not fly", "$1", 1).Should().Be("can fly"); }
		[Fact] void ColorToHexStr() { new Color(0, 128, 255, 0).ToHexStr().Should().Be("#000080FF"); }

		[Fact] void ShowAllClassSizes()
		{
			var monoAssembly = Assembly.LoadFrom(new FileInfo("C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\MonoAndroid\\v5.1\\Mono.Android.dll").FullName);
			ShowAssemblyClassSizes("Main", Assembly.LoadFrom(new FileInfo("../../../Main/bin/Debug/Productivity Tracker.dll").FullName));
			Console.WriteLine("");
			ShowAssemblyClassSizes("Tests", Assembly.GetExecutingAssembly());
		}
		[Fact] void ShowAllClassSizes_WithMethodSizes()
		{
			var monoAssembly = Assembly.LoadFrom(new FileInfo("C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\MonoAndroid\\v5.1\\Mono.Android.dll").FullName);
			ShowAssemblyClassSizes("Main", Assembly.LoadFrom(new FileInfo("../../../Main/bin/Debug/Productivity Tracker.dll").FullName), true);
			Console.WriteLine("");
			ShowAssemblyClassSizes("Tests", Assembly.GetExecutingAssembly(), true);
		}
		[Fact] void ShowAllMethodSizes()
		{
			// old: no need to load assembly, since it's automatically loaded for this test-method (it uses the custom Dictionary<,>.AddDictionary method, in the Main assembly, which references the MonoAndroid assembly)
			var monoAssembly = Assembly.LoadFrom(new FileInfo("C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\MonoAndroid\\v5.1\\Mono.Android.dll").FullName);
			ShowAssemblyMethodSizes("Main", Assembly.LoadFrom(new FileInfo("../../../Main/bin/Debug/Productivity Tracker.dll").FullName));
			Console.WriteLine("");
			ShowAssemblyMethodSizes("Tests", Assembly.GetExecutingAssembly());
		}
		void ShowAssemblyClassSizes(string title, Assembly assembly, bool showMethodSizes = false)
		{
			var classMethodSizes = new Dictionary<string, Dictionary<string, int>>();
			foreach (Type classType in assembly.GetTypes())
			{
				var className = classType.Name;
				if (classType.FullName.Contains("+<>"))
					//className += " (in " + classType.FullName.Substring(0, classType.FullName.IndexOf("+<>")) + ")";
					className = classType.FullName;
				while (classMethodSizes.ContainsKey(className))
					className += "_2";
				classMethodSizes.Add(className, GetClassMethodSizes(classType));
			}
			Console.WriteLine($"{title} (total: {classMethodSizes.Select(a=>a.Value.Values.Sum()).Sum()} bytes)");
			Console.WriteLine("==========");
			foreach (KeyValuePair<string, Dictionary<string, int>> pair in classMethodSizes.OrderByDescending(a=>a.Value.Values.Sum()))
			{
				Console.WriteLine($"    {pair.Key}: {pair.Value.Values.Sum()} bytes");
				if (showMethodSizes)
					foreach (KeyValuePair<string, int> pair2 in pair.Value.OrderByDescending(a=>a.Value))
						Console.WriteLine($"        {pair2.Key}: {pair2.Value} bytes");
			}
		}
		void ShowAssemblyMethodSizes(string title, Assembly assembly)
		{
			var methodSizes = new Dictionary<string, int>();
			foreach (Type classType in assembly.GetTypes())
				//methodSizes.AddDictionary(GetClassMethodSizes(classType, true));
				foreach (KeyValuePair<string, int> pair in GetClassMethodSizes(classType, true))
					methodSizes.Add(pair.Key, pair.Value);
			Console.WriteLine($"{title} (total: {methodSizes.Values.Sum()} bytes)");
			Console.WriteLine("==========");
			foreach (KeyValuePair<string, int> pair in methodSizes.OrderByDescending(a=>a.Value))
				Console.WriteLine($"    {pair.Key}: {pair.Value} bytes");
		}
		/*int GetClassSize(Type classType)
		{
			var result = 0;
			foreach (var method in classType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.DeclaredOnly))
			{
				MethodBody methodBody = method.GetMethodBody();
				if (methodBody != null)
					result += methodBody.GetILAsByteArray().Length;
			}
			return result;
		}*/
		Dictionary<string, int> GetClassMethodSizes(Type classType, bool includeClassTypeName = false)
		{
			var result = new Dictionary<string, int>();
			foreach (var method in classType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.DeclaredOnly))
			{
				MethodBody methodBody = method.GetMethodBody();
				if (methodBody != null)
				{
					var methodName = method.Name;
					if (includeClassTypeName)
					{
						var className = classType.Name;
						if (classType.FullName.Contains("+<>"))
							//className += " (in " + classType.FullName.Substring(0, classType.FullName.IndexOf("+<>")) + ")";
							className = classType.FullName;
						methodName += $" (in {className})";
					}
					while (result.ContainsKey(methodName))
						methodName += "_2";

					result.Add(methodName, methodBody.GetILAsByteArray().Length);
				}
			}
			return result;
		}
	}
}