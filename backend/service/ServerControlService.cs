using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.backend.serverinstall.components;
using SCLauncher.model;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.service;

public class ServerControlService
{
	public event DataReceivedEventHandler? OutputReceived;
	public event DataReceivedEventHandler? ErrorReceived;
	public event EventHandler<bool>? StateChanged;

	private Process? serverProcess;
	private readonly GlobalConfiguration config;
	private readonly BackendService backend;
	private readonly ServerInstallService installService;

	public ServerControlService(GlobalConfiguration config, BackendService backend, ServerInstallService installService)
	{
		this.config = config;
		this.backend = backend;
		this.installService = installService;

		AppDomain.CurrentDomain.ProcessExit += (sender, args) => Stop();
	}

	public bool IsRunning
	{
		get
		{
			try
			{
				return serverProcess != null && !serverProcess.HasExited;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}

	public bool Start()
	{
		if (config.ServerPath == null)
			return false;
		if (IsRunning)
			return false;

		string executable;
		try
		{
			executable = Path.Join(config.ServerPath, SrcdsFixInstaller.GetExecForCurrentPlatform());
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
			string srcdsLinux = Path.Join(config.ServerPath, "srcds_linux");
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
			serverProcess = new Process
			{
				EnableRaisingEvents = true,
				StartInfo = new ProcessStartInfo
				{
					FileName = executable,
					WorkingDirectory = config.ServerPath,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					CreateNoWindow = true
				}
			};

			var args = serverProcess.StartInfo.ArgumentList;
			var appInfo = backend.ActiveApp;
			args.Add("-console");
			args.Add("-nocrashdialog");
			args.Add("-game");
			args.Add(appInfo.ModFolder);
			foreach (string confParam in appInfo.NewServerConfig().ToLaunchParams())
			{
				args.Add(confParam);
			}
			
			serverProcess.OutputDataReceived += (s, e) => OutputReceived?.Invoke(s, e);
			serverProcess.ErrorDataReceived += (s, e) => ErrorReceived?.Invoke(s, e);
			serverProcess.Start();
			
			if (IsRunning)
			{
				StateChanged?.Invoke(this, true);
				serverProcess.Exited += (sender, args) => StateChanged?.Invoke(sender, false);
				serverProcess.BeginOutputReadLine();
				serverProcess.BeginErrorReadLine();
				
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
		if (serverProcess == null)
			return;
		
		try
		{
			Process process = serverProcess;
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
			serverProcess = null;
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
			serverProcess!.StandardInput.WriteLine(cmd);
		}
	}

	public async Task<ServerAvailability> IsAvailableAsync(CancellationToken cancellationToken = default)
	{
		if (config.ServerPath == null)
			return ServerAvailability.Unavailable;

		var p = new ServerInstallParams
		{
			AppInfo = backend.ActiveApp,
			Method = ServerInstallMethod.External,
			Path = config.ServerPath,
			CreateSubfolder = false
		};
		
		var infos = await installService.GatherComponentInfoAsync(p, false, cancellationToken);
		bool installIncomplete = infos.Values.Any(info => info is { Installable: true, Installed: false });
		bool installPartial = infos.Values.Any(info => info is { Installable: true, Installed: true }) && installIncomplete;
		return installIncomplete
			? installPartial ? ServerAvailability.PartiallyInstalled : ServerAvailability.Unavailable
			: ServerAvailability.Available;
	}

}