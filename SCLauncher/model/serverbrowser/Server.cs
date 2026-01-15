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
	public required string GameDescription { get; init; }
	public required int NumPlayers { get; init; }
	public required int MaxPlayers { get; init; }
	public required int NumBots { get; init; }
	public required string Map { get; init; }
	public required string Type { get; init; }
	public required bool Secure { get; init; }
	public required string Version { get; init; }
	public required string Keywords { get; init; }
	public required ulong? SteamId { get; init; }
	public required string Environment { get; init; }
	public bool? Password { get; set; }
	public TimeSpan? Ping { get; set; }

	public string Endpoint => IP + ':' + Port;
	public int NumRealPlayers => NumPlayers - NumBots;
}