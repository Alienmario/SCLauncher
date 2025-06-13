using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
	
	public static void ValidateApp(int appId)
	{
		Process.Start(new ProcessStartInfo("steam://validate/" + appId)
		{
			UseShellExecute = true,
			Verb = "open"
		});
	}

	public static void UninstallApp(int appId)
	{
		Process.Start(new ProcessStartInfo("steam://uninstall/" + appId)
		{
			UseShellExecute = true,
			Verb = "open"
		});
	}

	public static void ConnectToServer(string address, int appId, string? password = null)
	{
		Process.Start(new ProcessStartInfo(GetConnectLink(address, appId, password))
		{
			UseShellExecute = true,
			Verb = "open"
		});
	}

	public static string GetConnectLink(string address, int appId, string? password = null)
	{
		return new StringBuilder()
			.Append("steam://connect/")
			.Append(address)
			.Append(!string.IsNullOrEmpty(password) ? "/" + password : null)
			.Append("?appid=").Append(appId)
			.ToString();
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
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{

			using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
			if (key == null)
			{
				return null;
			}

			var path = key.GetValue("InstallPath") as string;
			return string.IsNullOrEmpty(path) ? null : path;
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (home.Equals(string.Empty))
			{
				return null;
			}
			var path = Path.Join(home, ".local/share/Steam");
			if (Directory.Exists(path))
			{
				return path;
			}
			path = Path.Join(home, "snap/steam/common/.local/share/Steam");
			if (Directory.Exists(path))
			{
				return path;
			}
		}

		return null;
	}

	public static bool IsValidSteamInstallDir(string? path)
	{
		return !string.IsNullOrWhiteSpace(path) && File.Exists(Path.Join(path, "steamapps", "libraryfolders.vdf"));
	}

}