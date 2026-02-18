using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SCLauncher.model.exception;
using Path = System.IO.Path;

namespace SCLauncher.backend.service;

public class ClientControlService(ProfilesService profilesService)
{
	/// <summary>
	/// Starts the game client or hijacks running instance and connects to the specified server address.
	/// </summary>
	/// <param name="address">The server address to connect to, usually in the format "ip:port"</param>
	/// <param name="password"></param>
	/// <returns>True if the game client was started successfully, false otherwise</returns>
	/// <exception cref="InvalidGamePathException">Thrown when the game executable cannot be found at configured path</exception>
	/// <exception cref="PlatformNotSupportedException">Thrown when current platform is not supported for active profile</exception>
	public bool ConnectToServer(string address, string? password = null)
	{
		List<string> args = ["-hijack", "-steam", "+connect", address];
		if (!string.IsNullOrEmpty(password))
		{
			args.AddRange(["+password", password]);
		}

		List<string> configArgs = profilesService.ActiveProfile.ClientConfig.ToLaunchParams();
		configArgs.Remove("-multirun"); // do not launch extra instances when just connecting

		return RunClient(configArgs.Concat(args));
	}

	/// <param name="args">Launch parameter arguments, null means use the client config.</param>
	/// <exception cref="InvalidGamePathException">Thrown when the game executable cannot be found at configured path</exception>
	/// <exception cref="PlatformNotSupportedException">Thrown when current platform is not supported for active profile</exception>
	public bool RunClient(IEnumerable<string>? args = null)
	{
		var profile = profilesService.ActiveProfile;
		
		// Check if the platform is supported
		if (!profile.GameExecutable.TryGetValue(Environment.OSVersion.Platform, out string? executableName))
		{
			throw new PlatformNotSupportedException("Current game profile does not support your OS.");
		}
    
		// Check if executable exists
		var fullPath = Path.Join(profile.GamePath, executableName);
		if (!File.Exists(fullPath))
		{
			throw new InvalidGamePathException("Configured game path is invalid.");
		}

		try
		{
			Process? process = Process.Start(
				new ProcessStartInfo(fullPath, args ?? profile.ClientConfig.ToLaunchParams())
				{
					UseShellExecute = true,
					Verb = "open"
				});
			return process != null;
		}
		catch (Exception e)
		{
			e.Log();
			return false;
		}
	}
	
}