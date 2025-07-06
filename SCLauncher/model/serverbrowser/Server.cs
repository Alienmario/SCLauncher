using System;
using System.Collections.Generic;

namespace SCLauncher.model.serverbrowser;

public record Server
{
	public required IList<ServerPlayer> Players { get; init; }
	public required IList<ServerRule> Rules { get; init; }
	
	public required string IP { get; init; }
	public required int Port { get; init; }
	public required string Name { get; init; }
	public required ulong? GameAppId { get; init; }
	public required string GameModDir { get; init; }
	public required string GameDescription { get; init; }
	public required int NumPlayers { get; init; }
	public required int MaxPlayers { get; init; }
	public required int NumBots { get; init; }
	public required string Map { get; init; }
	public required string Type { get; init; }
	public required bool VAC { get; init; }
	public required bool Password { get; init; }
	public required string Version { get; init; }
	public required string Keywords { get; init; }
	public required ulong? SteamId { get; init; }
	public required string Environment { get; init; }
	public required TimeSpan Ping { get; init; }

	public string Endpoint => IP + ':' + Port;
	public int NumRealPlayers => NumPlayers - NumBots;
}