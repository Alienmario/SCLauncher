using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
using SCLauncher.backend.steam;

namespace SCLauncher.backend.util;

// Credits to DasDarki/JBPPP2

public static class SteamUtils
{
	
	public static void LaunchApp(int appId)
	{
		Process.Start(new ProcessStartInfo("steam://launch/" + appId + "/dialog")
		{
			UseShellExecute = true,
			Verb = "open"
		});
	}

	public static void InstallApp(int appId)
	{
		Process.Start(new ProcessStartInfo("steam://install/" + appId)
		{
			UseShellExecute = true,
			Verb = "open"
		});
	}
	
	public static async Task<SteamAppManifest?> FindAppManifestAsync(string steamDir, int appId, CancellationToken ct = default)
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

		VProperty vdf = VdfConvert.Deserialize(await File.ReadAllTextAsync(path, ct));

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

			SteamLibrary library = new SteamLibrary(obj);
			var libPath = library.Path;

			if (string.IsNullOrEmpty(libPath))
			{
				continue;
			}

			var manifestPath = Path.Combine(libPath, "steamapps", "appmanifest_" + appId + ".acf");

			if (!File.Exists(manifestPath))
			{
				continue;
			}

			VProperty vManifest = VdfConvert.Deserialize(await File.ReadAllTextAsync(manifestPath, ct));
			if (vManifest.Value is not VObject vManifestVal)
				continue;
			
			return new SteamAppManifest(vManifestVal, library);
		}

		return null;
	}

	public static async Task<string?> FindAppPathAsync(string steamDir, int appId, CancellationToken ct = default)
	{
		var manifest = await FindAppManifestAsync(steamDir, appId, ct);
		return manifest?.GetAbsInstallPath();
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

	public static bool IsValidSteamInstallDir(string? path)
	{
		return !string.IsNullOrWhiteSpace(path) && File.Exists(Path.Join(path, "steamapps", "libraryfolders.vdf"));
	}
	
}