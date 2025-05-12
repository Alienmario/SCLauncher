using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.backend.install;
using SCLauncher.model;
using SCLauncher.model.install;
using SCLauncher.model.serverinstall;

namespace SCLauncher.backend.serverinstall.components;

public class SrcdsFixInstaller : IServerComponentInstaller<ComponentInfo>
{
	private const string Executable = "srcds-fix-x86.exe";
	
	public ServerInstallComponent ComponentType => ServerInstallComponent.SrcdsFix;
	
	public async IAsyncEnumerable<StatusMessage> Install(ServerInstallContext ctx,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		await using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
			typeof(SrcdsFixInstaller), Executable);
		if (stream == null)
		{
			throw new InstallException("Unable to read embedded executable");
		}

		string exePath = Path.Join(ctx.InstallDir, Executable);
		await using var fileStream = new FileStream(exePath, FileMode.OpenOrCreate);
		await stream.CopyToAsync(fileStream, cancellationToken);
		yield break;
	}

	public Task<ComponentInfo> GatherInfoAsync(ServerInstallContext ctx, bool checkForUpgrades,
		CancellationToken cancellationToken = default)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return Task.FromResult(ComponentInfo.DoNotInstall);
		}

		string exePath = Path.Join(ctx.InstallDir, Executable);
		if (!File.Exists(exePath))
		{
			return Task.FromResult(ComponentInfo.ReadyToInstall);
		}
		
		return Task.FromResult(new ComponentInfo
		{
			Path = exePath
		});
	}

}