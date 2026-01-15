using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.model.config;
using SCLauncher.model.serverbrowser;
using SteamKit2;
using SteamQuery;
using SteamQuery.Models;

namespace SCLauncher.backend.service;

public class ServerBrowserService(ProfilesService profilesService, GlobalConfiguration globalConfig)
{
	private const int MaxConcurrentQueries = 50;
	private static readonly TimeSpan ServerQuerySendTimeout = TimeSpan.FromSeconds(5);
	private static readonly TimeSpan ServerQueryReadTimeout = TimeSpan.FromSeconds(5);

	public async IAsyncEnumerable<Server> GetServers([EnumeratorCancellation] CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(globalConfig.SteamWebApiKey))
			yield break;
		
		var queryTasks = new List<Task<Server>>();

		using (dynamic gameServersService = WebAPI.GetAsyncInterface("IGameServersService", globalConfig.SteamWebApiKey))
		{
			KeyValue serverList = await gameServersService.GetServerList(
					filter: "appid\\" + profilesService.ActiveProfile.GameAppId, limit: 9999)
				.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			
			foreach (KeyValue serverKv in serverList["servers"].Children)
			{
				string? endpoint = serverKv["addr"].Value;
				if (string.IsNullOrWhiteSpace(endpoint)) continue;
				
				queryTasks.Add(QueryServer(endpoint, ct: ct)
					.ContinueWith(t => t.Result ?? TranslateServer(serverKv), ct));
				
				if (queryTasks.Count >= MaxConcurrentQueries)
				{
					yield return await WaitForQueryResponses().ConfigureAwait(false);
				}
			}
		}
	
		while (queryTasks.Count > 0)
		{
			yield return await WaitForQueryResponses().ConfigureAwait(false);
		}
		
		async Task<Server> WaitForQueryResponses()
		{
			var finished = await Task.WhenAny(queryTasks).ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			queryTasks.Remove(finished);
			return await finished.ConfigureAwait(false);
		}
	}

	public async Task<Server?> QueryServer(string endpoint,
		bool players = false, bool rules = false,
		CancellationToken ct = default)
	{
		return await Task.Run(() =>
		{
			try
			{
				using var gameServer = new GameServer(endpoint);
				gameServer.SendTimeout = ServerQuerySendTimeout;
				gameServer.ReceiveTimeout = ServerQueryReadTimeout;
				gameServer.GetInformation();
				ct.ThrowIfCancellationRequested();
				if (players) gameServer.GetPlayers();
				ct.ThrowIfCancellationRequested();
				if (rules) gameServer.GetRules();
				ct.ThrowIfCancellationRequested();
	
				return TranslateServer(gameServer);
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				if (e is not SocketException) e.Log();
				return null;
			}
		}, ct).ConfigureAwait(false);
	}

	private static Server TranslateServer(KeyValue src)
	{
		return new Server
		{
			IP = src["addr"].Value?.Split(":")[0] ?? "",
			Port = src["gameport"].AsInteger(),
			SpectatePort = src["specport"].AsInteger(),
			Name = src["name"].Value ?? "",
			GameAppId = src["appid"].AsUnsignedLong(),
			GameModDir = src["gamedir"].Value ?? "",
			GameDescription = src["product"].Value ?? "",
			NumPlayers = src["players"].AsInteger() + src["bots"].AsInteger(),
			MaxPlayers = src["max_players"].AsInteger(),
			NumBots = src["bots"].AsInteger(),
			Map = src["map"].Value ?? "",
			Type = src["dedicated"].AsBoolean() ? "Dedicated" : "Non-dedicated",
			Secure = src["secure"].AsBoolean(),
			Version = src["version"].Value ?? "",
			Keywords = src["gametype"].Value ?? "",
			SteamId = src["steamid"].AsUnsignedLong(),
			Environment = src["os"].Value switch { "l" => "Linux", "w" => "Windows", _ => "Other" }
		};
	}

	private static Server TranslateServer(GameServer src)
	{
		return new Server
		{
			Players = src.Players.Select(TranslatePlayer).ToList(),
			Rules = src.Rules.Select(TranslateRule).ToList(),
			IP = src.IpAddress.ToString(),
			Port = src.Port,
			Name = src.Information.ServerName,
			GameAppId = src.Information.GameId,
			GameModDir = src.Information.Folder,
			GameDescription = src.Information.GameName,
			NumPlayers = src.Information.OnlinePlayers,
			MaxPlayers = src.Information.MaxPlayers,
			NumBots = src.Information.Bots,
			Map = src.Information.Map,
			Type = src.Information.ServerType.ToString(),
			Secure = src.Information.VacSecured,
			Password = !src.Information.Visible,
			Version = src.Information.Version,
			Keywords = src.Information.Keywords.Trim(','),
			SteamId = src.Information.SteamId,
			Environment = src.Information.Environment.ToString(),
			Ping = TimeSpan.Zero
		};
	}

	private static ServerPlayer TranslatePlayer(SteamQueryPlayer src)
	{
		return new ServerPlayer
		{
			Name = src.Name,
			Score = src.Score,
			Duration = src.DurationTimeSpan
		};
	}
	
	private static ServerRule TranslateRule(SteamQueryRule src)
	{
		return new ServerRule
		{
			Name = src.Name,
			Value = src.Value
		};
	}
}