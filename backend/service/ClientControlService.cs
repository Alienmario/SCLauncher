using System;
using System.Diagnostics;
using SCLauncher.model;
using Path = System.IO.Path;

namespace SCLauncher.backend.service;

public class ClientControlService(BackendService backend, GlobalConfiguration config)
{
	
	public bool ConnectToServer(string address)
	{
		// Will not work if BM server already running:
		// SteamUtils.ConnectToServer(address, backend.ActiveApp.GameAppId);
		
		try
		{
			string executable = Path.Join(config.GamePath, 
				backend.ActiveApp.GameExecutable[Environment.OSVersion.Platform]);
			
			Process? process = Process.Start(
				new ProcessStartInfo(executable, ["-hijack", "-steam", "+connect", address])
				{
					UseShellExecute = true,
					Verb = "open"
				});
			
			if (process == null)
				return false;
		}
		catch (Exception e)
		{
			e.Log();
			return false;
		}

		return true;
	}
}