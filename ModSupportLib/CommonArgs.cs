using System.ComponentModel;

namespace ModSupportLib;

public record CommonArgs
{
	public static readonly Uri DefaultRepositoryBase = new("https://github.com/ampreeT/SourceCoop/blob/master/mods");
	public static readonly Uri DefaultUserRepository = new("mods.json", UriKind.Relative);
	
	/// Main repository Uri from where "official" mods are loaded. Local/Remote.
	public required Uri MainRepository { get; init; }
	/// User repository uri, which is loaded in succession to the main repository. Local/Remote.
	public required Uri? UserRepository { get; init; }
	/// Path where workshop mods are downloaded - contains subfolders named by their workshop id
	public required string? WorkshopPath { get; init; }
	/// AppId for workshop downloads
	public required uint AppId { get; init; }
}

public partial class CommonArgsBuilder(uint appId) : INotifyPropertyChanged
{
	public Uri? MainRepository { get; set; } = new(CommonArgs.DefaultRepositoryBase, appId + ".json");
	public Uri? UserRepository { get; set; } = CommonArgs.DefaultUserRepository;
	public string? WorkshopPath { get; set; }
	
	public CommonArgs Build()
	{
		return new CommonArgs
		{
			MainRepository = MainRepository ?? throw new InvalidOperationException("MainRepository must be set"),
			UserRepository = UserRepository,
			WorkshopPath = WorkshopPath,
			AppId = appId
		};
	}
}