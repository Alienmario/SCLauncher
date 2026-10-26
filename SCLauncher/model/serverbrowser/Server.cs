using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SCLauncher.model.serverbrowser;

public partial class Server : INotifyPropertyChanged
{
	public IList<ServerPlayer> Players { get; set; } = [];
	public IList<ServerRule> Rules { get; set; } = [];
	
	public required string IP { get; init; }
	public required int Port { get; init; }
	public int? SpectatePort { get; init; }
	public required string Name { get; init; }
	public required ulong? GameAppId { get; init; }
	public required string GameModDir { get; init; }
	public string? GameDescription { get; init; }
	public required int NumPlayers { get; init; }
	public required int MaxPlayers { get; init; }
	public required int NumBots { get; init; }
	public string? Map { get; init; }
	public string? Type { get; init; }
	public required bool Secure { get; init; }
	public string? Version { get; init; }
	public string? Keywords { get; init; }
	public required ulong? SteamId { get; init; }
	public string? Environment { get; init; }
	public bool? Password { get; set; }
	public TimeSpan? Ping { get; set; }

	public string Address => IP + ':' + Port;
	public int NumRealPlayers => NumPlayers - NumBots;
}