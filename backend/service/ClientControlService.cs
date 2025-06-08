using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SCLauncher.model;
using SCLauncher.model.exception;
using Path = System.IO.Path;

namespace SCLauncher.backend.service;

public class ClientControlService(BackendService backend, GlobalConfiguration config)
{
	/// <summary>
	/// Starts the game client process and connects to the specified server address.
	/// </summary>
	/// <param name="address">The server address to connect to, usually in the format "ip:port"</param>
	/// <param name="password"></param>
	/// <returns>True if the game client was started successfully, false otherwise</returns>
	/// <exception cref="InvalidGamePathException">Thrown when the game executable cannot be found at the configured path</exception>
	public bool ConnectToServer(string address, string? password = null)
	{
		// Will not work if BM server already running:
		// SteamUtils.ConnectToServer(address, backend.ActiveApp.GameAppId);
		
		string executable = Path.Join(config.GamePath,
			backend.ActiveApp.GameExecutable[Environment.OSVersion.Platform]);

		if (!File.Exists(executable))
			throw new InvalidGamePathException();

		List<string> args = ["-hijack", "-steam", "+connect", address];
		if (!string.IsNullOrEmpty(password))
		{
			args.AddRange(["+password", password]);
		}

		try
		{
			Process? process = Process.Start(
				new ProcessStartInfo(executable, args)
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