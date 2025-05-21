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

public class ServerControlService(ConfigHolder config, BackendService backend, ServerInstallService installService)
{
	public event DataReceivedEventHandler? OutputReceived;
	public event DataReceivedEventHandler? ErrorReceived;
	public event EventHandler<bool>? StateChanged;
	public bool Running => serverProcess != null && !serverProcess.HasExited;
	
	private Process? serverProcess;
	
	public void Start()
	{
		if (config.ServerPath == null)
			return;
		if (Running)
			return;

		string executable;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			executable = Path.Join(config.ServerPath, SrcdsFixInstaller.Executable32);
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			executable = Path.Join(config.ServerPath, "srcds_run");
		}
		else return;
		
		Trace.WriteLine("Using srcds executable: " + executable);
		
		if (!File.Exists(executable))
			return;
		
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

		serverProcess.OutputDataReceived += (s, e) => OutputReceived?.Invoke(s, e);
		serverProcess.ErrorDataReceived += (s, e) => ErrorReceived?.Invoke(s, e);
		serverProcess.Start();
		if (Running)
		{
			StateChanged?.Invoke(this, true);
			serverProcess.Exited += (sender, args) => StateChanged?.Invoke(sender, false);
			serverProcess.BeginOutputReadLine();
			serverProcess.BeginErrorReadLine();
		}
	}

	public void Stop()
	{
		if (serverProcess != null)
		{
			Process process = serverProcess;
			process.Kill();
			Task.Run(() =>
			{
				process.WaitForExit();
				process.Dispose();
			});
			serverProcess = null;
		}
	}

	public void Command(string cmd)
	{
		if (Running)
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