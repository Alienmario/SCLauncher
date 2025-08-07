using DotMake.CommandLine;
using ModSupportLib.Local;
using ModSupportLib.Repository;
using ModSupportLib.Service;
using Tmds.Utils;

namespace ModSupportCli;

class Program
{
	private static int Main(string[] args)
	{
		// Starting a subprocess function? [Tmds.ExecFunction]
		if (ExecFunction.IsExecFunctionCommand(args))
		{
			return ExecFunction.Program.Main(args);
		}
		
		try
		{
			return Cli.Run<RootCommand>(args);
		}
		catch (ArgumentException e)
		{
			Console.Error.WriteLine(e.GetAllMessages());
		}
		return 1;
	}
}

public abstract class BaseCommand
{
	[CliOption(
		Required = false,
		Description = "Main repository from where \"official\" mods are loaded. Local/Remote." +
		              " [default: " + CommonArgs.DefaultRepositoryBase + "/{appId}.json]",
		HelpName = "URI"
	)]
	public Uri? MainRepo { get; set; }

	[CliOption(
		Description =
			"Optional user repository, which is loaded in succession to the main repository. Local/Remote.",
		HelpName = "URI"
	)]
	public Uri UserRepo { get; set; } = CommonArgs.DefaultUserRepository;

	[CliOption(
		Description = "Repository where installed mod information is cached.",
		HelpName = "URI"
	)]
	public Uri InstallRepo { get; set; } = CommonArgs.DefaultInstallRepository;

	[CliOption(
		Required = false,
		Description = "Path where workshop mods are downloaded - contains subfolders named by their Workshop ID.",
		HelpName = "path"
	)]
	public string? WorkshopPath { get; set; }

	[CliOption(
		Alias = "app",
		Description = "Steam AppId for workshop downloads. Also used to determine the main repository path.",
		HelpName = "appid"
	)]
	public required uint AppId { get; set; }

	protected CommonArgsBuilder GetCommonArgs()
	{
		CommonArgsBuilder builder = new CommonArgsBuilder(AppId);
		if (MainRepo != null)
			builder.MainRepository = MainRepo;
		builder.UserRepository = UserRepo;
		builder.InstallRepository = InstallRepo;
		if (WorkshopPath != null)
			builder.WorkshopPath = WorkshopPath;
		return builder;
	}
}

public abstract class BaseModQueryCommand : BaseCommand
{
	[CliArgument(Description = "A list of mods by name or short name. You'll probably want to wrap each in quotes.")]
	public required List<string> Mods { get; set; }

	[CliOption(Description = "Indicates that mods are specified by their workshop ID rather than by name.")]
	public bool WorkshopIds { get; set; }

	protected async Task<List<ModInfo>> GetFilteredModsAsync(CommonArgs args)
	{
		List<ModRepository> repos = [];
		if (args.UserRepository != null)
		{
			repos.Add(await ModSupportService.LoadRepositoryAsync(args.UserRepository));
		}
		repos.Add(await ModSupportService.LoadRepositoryAsync(args.MainRepository));
		
		return GetFilteredMods(repos);
	}

	protected List<ModInfo> GetFilteredMods(List<ModRepository> repos)
	{
		List<ModInfo> modInfos = [];
		foreach (string mod in Mods)
		{
			ModQuery query;
			try
			{
				query = WorkshopIds ? ModQuery.ForWorkshopId(Convert.ToUInt64(mod)) : ModQuery.ForName(mod);
			}
			catch (Exception e)
			{
				throw new ArgumentException($"Invalid mod identifier '{mod}'", e);
			}
			ModInfo? modInfo = ModSupportService.QueryMod(repos, query);
			if (modInfo != null)
			{
				modInfos.Add(modInfo);
			}
			else
			{
				throw new ArgumentException($"Mod not found '{mod}'");
			}
		}
		return modInfos;
	}
}

[CliCommand(Description = "SourceCoop mod support utility",
	NameCasingConvention = CliNameCasingConvention.LowerCase,
	TreatUnmatchedTokensAsErrors = false)]
public class RootCommand
{
	[CliCommand(Alias = "li", Name = "installed", Description = "Lists installed mods")]
	public class ListInstalledModsCommand : BaseCommand
	{
		public async Task RunAsync()
		{
			var repo = await ModSupportService.LoadRepositoryAsync(InstallRepo);
			if (repo.Installed.Count > 0)
			{
				Console.Out.WriteLine($"Listing installed mods (Total: {repo.Installed.Count})");
				Utils.PrintMods(repo.Installed);
			}
			else Console.Out.WriteLine("No mods are installed.");
		}
	}
	
	[CliCommand(Alias = "ls", Name = "supported", Description = "Lists supported mods")]
	public class ListSupportedModsCommand : BaseCommand
	{
		public async Task RunAsync()
		{
			var args = GetCommonArgs().Build();
			Console.Out.WriteLine();
			
			var mainRepo = await ModSupportService.LoadRepositoryAsync(args.MainRepository);
			if (mainRepo.LoadException == null)
			{
				if (mainRepo.Supported.Count > 0)
				{
					Console.Out.WriteLine($"Listing main repository (Total: {mainRepo.Supported.Count})");
					Utils.PrintMods(mainRepo.Supported);
				}
				else Console.Out.WriteLine("No mods found in the main repository.");
			}
			else
			{
				Console.Error.WriteLine($"There was an issue loading main repository. ({mainRepo.Location})");
				Console.Error.WriteLine(mainRepo.LoadException.GetAllMessages());
			}

			if (args.UserRepository != null)
			{
				Console.Out.WriteLine();
				var userRepo = await ModSupportService.LoadRepositoryAsync(args.UserRepository);
				if (userRepo.LoadException == null)
				{
					if (userRepo.Supported.Count > 0)
					{
						Console.Out.WriteLine($"Listing user repository (Total: {userRepo.Supported.Count})");
						Utils.PrintMods(userRepo.Supported);
					}
					else Console.Out.WriteLine("No mods found in the user repository.");
				}
				else if (userRepo.LoadException is not FileNotFoundException
				         || !ReferenceEquals(UserRepo, CommonArgs.DefaultUserRepository))
				{
					Console.Error.WriteLine($"There was an issue loading user repository. ({userRepo.Location})");
					Console.Error.WriteLine(userRepo.LoadException.GetAllMessages());
				}
			}
		}
	}
	
	[CliCommand(Alias = "i", Name = "install", Description = "Installs mods")]
	public class InstallModsCommand : BaseModQueryCommand
	{
		public async Task RunAsync()
		{
			CommonArgs commonArgs = GetCommonArgs().Build();
			List<ModInfo> modList = await GetFilteredModsAsync(commonArgs);
			
			foreach (ModInfo modInfo in modList)
			{
				Console.Out.WriteLine($"Installing mod '{modInfo.Name}'");
				try
				{
					ModLocalState state = await ModSupportService.InstallMod(commonArgs, modInfo, (_, evArgs) =>
					{
						if (evArgs.Data != null) Console.Out.WriteLine("  " + evArgs.Data);
					});
					Console.Out.WriteLine($"Mod '{modInfo.Name}' successfully installed at '{state.AbsoluteInstallPath}'");
				}
				catch (Exception e)
				{
					Console.Out.WriteLine($"Install failed - {e.GetAllMessages()}");
				}
			}
		}
	}
		
	[CliCommand(Alias = "u", Name = "uninstall", Description = "Uninstalls mods")]
	public class UninstallModsCommand : BaseModQueryCommand
	{
		public async Task RunAsync()
		{
			CommonArgs commonArgs = GetCommonArgs().Build();
			List<ModInfo> modList = await GetFilteredModsAsync(commonArgs);
			
			foreach (ModInfo modInfo in modList)
			{
				Console.Out.Write($"Uninstalling mod '{modInfo.Name}'");
				try
				{
					await ModSupportService.UninstallMod(commonArgs, modInfo);
					Console.Out.WriteLine($"Mod '{modInfo.Name}' successfully uninstalled");
				}
				catch (Exception e)
				{
					Console.Out.WriteLine($"Uninstall failed - {e.GetAllMessages()}");
				}
			}
		}
	}
}