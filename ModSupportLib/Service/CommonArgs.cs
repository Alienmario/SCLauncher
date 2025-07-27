using System.ComponentModel;

namespace ModSupportLib.Service;

public record CommonArgs
{
	public const string DefaultRepositoryBase = "https://github.com/ampreeT/SourceCoop/blob/master/mods";
	public static readonly Uri DefaultUserRepository = new("mods.json", UriKind.Relative);
	public static readonly Uri DefaultInstallRepository = new("mods.json", UriKind.Relative);
	
	/// Main repository URI from where "official" mods are loaded. Local/Remote.
	public required Uri MainRepository { get; init; }
	/// Optional user repository URI, which is loaded in succession to the main repository. Local/Remote.
	public Uri? UserRepository { get; init; }
	/// Repository where installed mod information is cached.
	public required Uri InstallRepository { get; init; }
	/// Path where workshop mods are downloaded - contains subfolders named by their workshop id
	public string? WorkshopPath { get; init; }
	/// AppId for workshop downloads
	public required uint AppId { get; init; }
}

public partial class CommonArgsBuilder(uint appId) : INotifyPropertyChanged
{
	public Uri? MainRepository { get; set; } = new($"{CommonArgs.DefaultRepositoryBase}/{appId}.json");
	public Uri? UserRepository { get; set; } = CommonArgs.DefaultUserRepository;
	public Uri? InstallRepository { get; set; } = CommonArgs.DefaultInstallRepository;
	public string? WorkshopPath { get; set; }
	
	public CommonArgs Build()
	{
		return new CommonArgs
		{
			MainRepository = MainRepository ?? throw new InvalidOperationException("MainRepository must be set"),
			UserRepository = UserRepository,
			InstallRepository = InstallRepository ?? throw new InvalidOperationException("InstallRepository must be set"),
			WorkshopPath = WorkshopPath,
			AppId = appId
		};
	}
}