using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components;

public class SrcdsFixInstaller : IServerComponentInstaller<ComponentInfo>
{
	public const string ExecutableWin32 = "srcds_sclauncher_x86.exe";
	public const string ExecutableWin64 = "srcds_sclauncher_x64.exe";
	public const string ExecutableLinux = "srcds_sclauncher";
	
	public ServerInstallComponent ComponentType => ServerInstallComponent.SrcdsFix;
	
	public async IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken ct)
	{
		string executable = GetExecForCurrentPlatform();
		await using Stream? stream = Assembly.GetExecutingAssembly()
			.GetManifestResourceStream(typeof(SrcdsFixInstaller), executable);
		if (stream == null)
		{
			throw new InstallException("Unable to read embedded executable");
		}

		string exePath = Path.Join(ctx.InstallDir, executable);
		await using var fileStream = new FileStream(exePath, FileMode.OpenOrCreate);
		await stream.CopyToAsync(fileStream, ct);
		yield break;
	}

	public Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades,
		CancellationToken ct = default)
	{
		string execPath;
		try
		{
			execPath = Path.Join(ctx.InstallDir, GetExecForCurrentPlatform());
		}
		catch (PlatformNotSupportedException)
		{
			return Task.FromResult(ComponentInfo.DoNotInstall);
		}

		if (!File.Exists(execPath))
		{
			return Task.FromResult(ComponentInfo.ReadyToInstall);
		}
		
		return Task.FromResult(new ComponentInfo
		{
			Path = execPath
		});
	}

	public static string GetExecForCurrentPlatform(bool x64 = false)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return x64 ? ExecutableWin64 : ExecutableWin32;
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return ExecutableLinux;
		}
		throw new PlatformNotSupportedException();
	}

}