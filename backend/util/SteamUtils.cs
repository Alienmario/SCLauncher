using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;

namespace SCLauncher.backend.util;

// Credits to DasDarki/JBPPP2

public static class SteamUtils
{
	
	public static bool LaunchApp(int appId)
	{
		Process.Start(new ProcessStartInfo("steam://launch/" + appId + "/dialog")
		{
			UseShellExecute = true,
			Verb = "open"
		});

		return true;
	}

	public static bool InstallApp(int appId)
	{
		Process.Start(new ProcessStartInfo("steam://install/" + appId)
		{
			UseShellExecute = true,
			Verb = "open"
		});

		return true;
	}

	public static string? FindAppPath(string steamDir, int appId)
	{
		if (string.IsNullOrEmpty(steamDir))
		{
			return null;
		}

		var path = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");

		if (!File.Exists(path))
		{
			return null;
		}

		VProperty vdf = VdfConvert.Deserialize(File.ReadAllText(path));

		foreach (var val in vdf.Value.Children())
		{
			if (val is not VProperty prop)
			{
				continue;
			}

			if (prop.Value is not VObject obj)
			{
				continue;
			}

			var libPath = obj["path"]?.Value<string>();

			if (string.IsNullOrEmpty(libPath))
			{
				continue;
			}

			var appManifest = Path.Combine(libPath, "steamapps", "appmanifest_" + appId + ".acf");

			if (!File.Exists(appManifest))
			{
				continue;
			}

			VProperty manifest = VdfConvert.Deserialize(File.ReadAllText(appManifest));
			var installDir = manifest.Value["installdir"]?.Value<string>();

			if (string.IsNullOrEmpty(installDir))
			{
				continue;
			}

			return Path.Combine(libPath, "steamapps", "common", installDir);
		}

		return null;
	}

	public static string? FindSteamInstallDir()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return null;
		}

		using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
		if (key == null)
		{
			return null;
		}

		var path = key.GetValue("InstallPath") as string;
		return string.IsNullOrEmpty(path) ? null : path;
	}
	
}