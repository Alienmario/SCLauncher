using System;
using System.Linq;
using NuGet.Versioning;

namespace SCLauncher.backend.util;

public static class VersionUtils
{
	public static string FormatVersion(string version)
	{
		if (NuGetVersion.TryParse(version.TrimStart('v', 'V'), out var nv))
		{
			int fields = nv.Revision > 0 ? 4 : nv.Patch > 0 ? 3 : 2;
			return nv.IsPrerelease ? $"{nv.Version.ToString(fields)}-{nv.Release}" : nv.Version.ToString(fields);
		}

		return version.Split('+')[0];
	}

	public static int SmartCompare(string v1, string v2)
	{
		if (NuGetVersion.TryParse(v1.TrimStart('v', 'V'), out var nv1) &&
		    NuGetVersion.TryParse(v2.TrimStart('v', 'V'), out var nv2))
		{
			return nv1.CompareTo(nv2);
		}

		return SmartCompareInner(v1, v2, ['-'], (vv1, vv2) =>
		{
			return SmartCompareInner(vv1, vv2, ['.'], (vvv1, vvv2) =>
			{
				string numeric1 = string.Concat(vvv1.Where(char.IsDigit));
				string numeric2 = string.Concat(vvv2.Where(char.IsDigit));
				int num1 = int.TryParse(numeric1, out int res1) ? res1 : 0;
				int num2 = int.TryParse(numeric2, out int res2) ? res2 : 0;
				return num1.CompareTo(num2);
			});
		});
	}

	private static int SmartCompareInner(string ver1, string ver2, char[]? separator, Func<string, string, int> compFunc)
	{
		string[] ver1Arr = ver1.Split(separator, StringSplitOptions.TrimEntries);
		string[] ver2Arr = ver2.Split(separator, StringSplitOptions.TrimEntries);
		
		int maxLength = Math.Max(ver1Arr.Length, ver2Arr.Length);
		for (int i = 0; i < maxLength; i++)
		{
			int comp = compFunc(
				ver1Arr.ElementAtOrDefault(i) ?? string.Empty,
				ver2Arr.ElementAtOrDefault(i) ?? string.Empty);
			if (comp != 0)
			{
				return comp;
			}
		}

		return 0;
	}
}
