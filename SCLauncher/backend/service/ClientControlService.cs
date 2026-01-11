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
	public bool RunClient(IEnumerable<string>? args = null)
	{
		var exec = Path.Join(profilesService.ActiveProfile.GamePath,
			profilesService.ActiveProfile.GameExecutable[Environment.OSVersion.Platform]);

		if (!File.Exists(exec))
			throw new InvalidGamePathException();

		try
		{
			Process? process = Process.Start(
				new ProcessStartInfo(exec, args ?? profilesService.ActiveProfile.ClientConfig.ToLaunchParams())
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