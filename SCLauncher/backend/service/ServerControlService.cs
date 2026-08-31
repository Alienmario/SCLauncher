using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.backend.serverinstall.components;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.service;

public class ServerControlService
{
	public event DataReceivedEventHandler? OutputReceived;
	public event DataReceivedEventHandler? ErrorReceived;
	public event EventHandler<bool>? StateChanged;

	private readonly ProfilesService _profilesService;
	private Process? _serverProcess;

	public ServerControlService(ProfilesService profilesService)
	{
		_profilesService = profilesService;

		AppDomain.CurrentDomain.ProcessExit += (sender, args) => Stop();
	}

	public bool IsRunning
	{
		get
		{
			try
			{
				return _serverProcess != null && !_serverProcess.HasExited;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}

	public bool Start()
	{
		if (_profilesService.ActiveProfile.ServerPath == null)
			return false;
		if (IsRunning)
			return false;

		string executable;
		try
		{
			executable = Path.Join(_profilesService.ActiveProfile.ServerPath, SrcdsFixInstaller.GetExecForCurrentPlatform());
		}
		catch (PlatformNotSupportedException e)
		{
			e.Log();
			return false;
		}

		Trace.WriteLine("Using srcds executable: " + executable);
		
		if (!File.Exists(executable))
		{
			Trace.WriteLine("Executable not found!");
			return false;
		}
		
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			string srcdsLinux = Path.Join(_profilesService.ActiveProfile.ServerPath, "srcds_linux");
			try
			{
				File.SetUnixFileMode(executable, File.GetUnixFileMode(executable) | UnixFileMode.UserExecute);
				File.SetUnixFileMode(srcdsLinux, File.GetUnixFileMode(srcdsLinux) | UnixFileMode.UserExecute);
			}
			catch (Exception e)
			{
				e.Log();
				return false;
			}
		}
		
		try
		{
			_serverProcess = new Process
			{
				EnableRaisingEvents = true,
				StartInfo = new ProcessStartInfo
				{
					FileName = executable,
					WorkingDirectory = _profilesService.ActiveProfile.ServerPath,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					CreateNoWindow = true
				}
			};

			var args = _serverProcess.StartInfo.ArgumentList;
			var appInfo = _profilesService.ActiveProfile;
			args.Add("-console");
			args.Add("-nocrashdialog");
			args.Add("-game");
			args.Add(appInfo.ModFolder);
			foreach (string arg in _profilesService.ActiveProfile.ServerConfig.ToLaunchParams())
			{
				args.Add(arg);
			}
			
			_serverProcess.OutputDataReceived += (s, e) => OutputReceived?.Invoke(s, e);
			_serverProcess.ErrorDataReceived += (s, e) => ErrorReceived?.Invoke(s, e);
			_serverProcess.Start();
			
			if (IsRunning)
			{
				StateChanged?.Invoke(this, true);
				_serverProcess.Exited += (sender, _) => StateChanged?.Invoke(sender, false);
				_serverProcess.BeginOutputReadLine();
				_serverProcess.BeginErrorReadLine();
				
				return true;
			}
		}
		catch (Exception e)
		{
			e.Log();
			Stop();
		}

		return false;
	}

	public void Stop()
	{
		if (_serverProcess == null)
			return;
		
		try
		{
			Process process = _serverProcess;
			try
			{
				process.Kill(true);
			}
			catch (Exception e)
			{
				e.Log();
			}

			Task.Run(() =>
			{
				process.WaitForExit();
				process.Dispose();
			});
			_serverProcess = null;
		}
		catch (Exception e)
		{
			e.Log();
		}
	}

	public void Command(string cmd)
	{
		if (IsRunning)
		{
			_serverProcess!.StandardInput.WriteLine(cmd);
		}
	}

}