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
	private readonly ConfigHolder config;
	private readonly BackendService backend;
	private readonly ServerInstallService installService;

	public ServerControlService(ConfigHolder config, BackendService backend, ServerInstallService installService)
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
			// Windows srcds-fix errors in "CTextConsoleWin32::GetLine: !GetNumberOfConsoleInputEvents",
			// but this disappears in published app !?
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
					CreateNoWindow = true,
					ArgumentList =
					{
						"-console",
						"-nocrashdialog",
						"-game", backend.GetActiveApp().ModFolder,
						"-ip", "0.0.0.0",
						"+maxplayers", "32",
						"+mp_teamplay", "1",
						"+map", "bm_c0a0a"
					}
				}
			};

			// if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			// {
			// 	serverProcess.StartInfo.Environment.TryGetValue("LD_LIBRARY_PATH", out string? current);
			// 	serverProcess.StartInfo.Environment["LD_LIBRARY_PATH"] = ".:bin" + (current != null ? ":" + current : "");
			// }

			// Trace.WriteLine("Using environment variables:");
			// Trace.WriteLine(string.Join("\n", serverProcess.StartInfo.Environment.Select(kvp => $"{kvp.Key}={kvp.Value}")));
			
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
			serverProcess?.Dispose();
			serverProcess = null;
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
			process.Kill(true);
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

	public async Task<(bool IsAvailable, bool IsPartial)> IsAvailableAsync(CancellationToken cancellationToken = default)
	{
		if (config.ServerPath == null)
			return (false, false);

		var p = new ServerInstallParams
		{
			AppInfo = backend.GetActiveApp(),
			Method = ServerInstallMethod.External,
			Path = config.ServerPath,
			CreateSubfolder = false
		};
		
		var infos = await installService.GatherComponentInfoAsync(p, false, cancellationToken);
		bool installIncomplete = infos.Values.Any(info => info is { Installable: true, Installed: false });
		bool installPartial = infos.Values.Any(info => info is { Installable: true, Installed: true }) && installIncomplete;
		return (!installIncomplete, installPartial);
	}

}