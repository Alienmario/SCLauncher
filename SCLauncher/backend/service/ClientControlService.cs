using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SCLauncher.model.config;
using SCLauncher.model.exception;
using Path = System.IO.Path;

namespace SCLauncher.backend.service;

public class ClientControlService(BackendService backend, GlobalConfiguration globalConfig)
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

		return RunClient(args);
	}

	/// <param name="args">Launch parameter args</param>
	/// <param name="useConfig">Whether to concat game client configuration to the args</param>
	/// <exception cref="InvalidGamePathException">Thrown when the game executable cannot be found at configured path</exception>
	public bool RunClient(IEnumerable<string>? args = null, bool useConfig = true)
	{
		var exec = Path.Join(globalConfig.GamePath, backend.ActiveApp.GameExecutable[Environment.OSVersion.Platform]);

		if (!File.Exists(exec))
			throw new InvalidGamePathException();

		args ??= [];
		IEnumerable<string> finalArgs = useConfig ? backend.GetClientConfig().ToLaunchParams().Concat(args) : args;

		try
		{
			Process? process = Process.Start(
				new ProcessStartInfo(exec, finalArgs)
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