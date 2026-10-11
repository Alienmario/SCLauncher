using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.backend.util;
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
					filter: "appid\\" + profilesService.ActiveProfile.GameAppId,
					limit: 9999)
				.ConfigureAwait(false);
			ct.ThrowIfCancellationRequested();
			
			foreach (KeyValue serverKv in serverList["servers"].Children)
			{
				string? address = serverKv["addr"].Value;
				if (string.IsNullOrWhiteSpace(address)) continue;

				// save unnecessary api calls for fake IPs
				if (SteamUtils.IsFakeIp(address))
				{
					yield return TranslateServer(serverKv);
					continue;
				}

				// pings each server with A2S UDP query, testing reachability and latency (eventually...)
				// unreachable servers are translated and returned as received
				queryTasks.Add(QueryServer(address, ct: ct)
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

	public async Task<Server?> QueryServer(string address, bool players = false, bool rules = false,
		CancellationToken ct = default)
	{
		try
		{
			// Steam networking fake IPs are unreachable through A2S and need a web service call
			if (SteamUtils.IsFakeIp(address))
			{
				return await QueryFakeIpServer(address, players, rules, ct).ConfigureAwait(false);
			}

			return await Task.Run(() =>
			{
				using var gameServer = new GameServer(address);
				gameServer.SendTimeout = ServerQuerySendTimeout;
				gameServer.ReceiveTimeout = ServerQueryReadTimeout;
				gameServer.GetInformation();
				ct.ThrowIfCancellationRequested();
				if (players) gameServer.GetPlayers();
				ct.ThrowIfCancellationRequested();
				if (rules) gameServer.GetRules();
				ct.ThrowIfCancellationRequested();

				return TranslateServer(gameServer);
			}, ct).ConfigureAwait(false);
		}
		catch (Exception e) when (e is not OperationCanceledException)
		{
			if (e is not SocketException)
				e.Log();
			return null;
		}
	}

	private async Task<Server?> QueryFakeIpServer(string address, bool players, bool rules, CancellationToken ct)
	{
		if (!string.IsNullOrWhiteSpace(globalConfig.SteamWebApiKey))
		{
			string[] parts = address.Split(':');
			if (parts.Length != 2)
				throw new FormatException($"Invalid address format \"{address}\"");
			
			var ipAddress = IPAddress.Parse(parts[0]);
			byte[] ipBytes = ipAddress.GetAddressBytes();
			uint encodedIp = (uint)((ipBytes[0] << 24) | (ipBytes[1] << 16) | (ipBytes[2] << 8) | ipBytes[3]);
			uint port = uint.Parse(parts[1]);
			uint appId = profilesService.ActiveProfile.GameAppId;

			using dynamic gameServersService = WebAPI.GetAsyncInterface("IGameServersService", globalConfig.SteamWebApiKey);
			KeyValue response = await gameServersService.QueryByFakeIP(
				fake_ip: encodedIp,
				fake_port: port,
				app_id: appId,
				query_type: 1);
				
			ct.ThrowIfCancellationRequested();
			
			if (response["ping_data"] != KeyValue.Invalid)
			{
				Server server = TranslateServer(response["ping_data"]);
				
				if (players)
				{
					KeyValue playersResponse = await gameServersService.QueryByFakeIP(
						fake_ip: encodedIp,
						fake_port: port,
						app_id: appId,
						query_type: 2);
					ct.ThrowIfCancellationRequested();
					foreach (var playerKv in playersResponse["players_data"]["players"].Children)
					{
						server.Players.Add(new ServerPlayer {
							Name = playerKv["name"].Value ?? "",
							Score = playerKv["score"].AsInteger(),
							Duration = TimeSpan.FromSeconds(playerKv["time_played"].AsInteger())
						});
					}
				}
				
				if (rules)
				{
					KeyValue rulesResponse = await gameServersService.QueryByFakeIP(
						fake_ip: encodedIp,
						fake_port: port,
						app_id: appId,
						query_type: 3);
					ct.ThrowIfCancellationRequested();
					foreach (var ruleKv in rulesResponse["rules_data"]["rules"].Children)
					{
						server.Rules.Add(new ServerRule {
							Name = ruleKv["rule"].Value ?? "",
							Value = ruleKv["value"].Value ?? ""
						});
					}
				}

				return server;
			}
		}
		else
		{
			// barebones un-authenticated check
			using dynamic steamAppsService = WebAPI.GetAsyncInterface("ISteamApps");
			KeyValue response = await steamAppsService.GetServersAtAddress(addr: address);
			ct.ThrowIfCancellationRequested();
			
			if (response["servers"].Children.Count > 0)
			{
				return TranslateServer(response["servers"].Children[0]);
			}
		}
		return null;
	}

	/// Translates server from Steamkit2 Web API
	private static Server TranslateServer(KeyValue src)
	{
		string ip = "";
		int port = 0;
		int? specPort = null;
		string? name = null;
		ulong appId = 0;
		string modDir = "";
		string? description = null;
		int players = 0;
		int bots = 0;
		int maxPlayers = 0;
		string? map = null;
		string? type = null;
		bool secure = false;
		bool? password = null;
		string? version = null;
		string? keywords = null;
		ulong steamId = 0;
		string? env = null;

		foreach (var child in src.Children)
		{
			switch (child.Name?.ToLowerInvariant())
			{
				case "addr":
					ip = child.Value!.Split(":")[0];
					break;
				case "server_ip":
				{
					var v4Ip = child["v4"];
					if (v4Ip != KeyValue.Invalid)
					{
						var val = v4Ip.AsUnsignedInteger();
						ip = $"{(val >> 24) & 0xFF}.{(val >> 16) & 0xFF}.{(val >> 8) & 0xFF}.{val & 0xFF}";
					}
					break;
				}
				case "gameport":
				case "game_port":
					port = child.AsInteger();
					break;
				case "specport":
					specPort = child.AsInteger();
					break;
				case "name":
				case "server_name":
					name = child.Value ?? "";
					break;
				case "appid":
				case "app_id":
					appId = child.AsUnsignedLong();
					break;
				case "gamedir":
					modDir = child.Value ?? "";
					break;
				case "product":
				case "game_description":
					description = string.IsNullOrWhiteSpace(child.Value) ? description : child.Value;
					break;
				case "players":
				case "num_players":
					players = child.AsInteger();
					break;
				case "bots":
				case "num_bots":
					bots = child.AsInteger();
					break;
				case "max_players":
					maxPlayers = child.AsInteger();
					break;
				case "map":
					map = string.IsNullOrWhiteSpace(child.Value) ? map : child.Value;
					break;
				case "dedicated":
					type = child.AsBoolean() ? "Dedicated" : "Non-dedicated";
					break;
				case "secure":
					secure = child.AsBoolean();
					break;
				case "password":
					password = child.AsBoolean();
					break;
				case "version":
					version = string.IsNullOrWhiteSpace(child.Value) ? version : child.Value;
					break;
				case "gametype":
					keywords = string.IsNullOrWhiteSpace(child.Value) ? keywords : child.Value;
					break;
				case "steamid":
					steamId = child.AsUnsignedLong();
					break;
				case "os":
					env = child.Value switch { "l" => "Linux", "w" => "Windows", _ => child.Value };
					break;
			}
		}

		// fallback for minimal result when querying fake IP with ISteamApps.GetServersAtAddress
		name ??= "<Server info unknown>";

		return new Server
		{
			IP = ip,
			Port = port,
			SpectatePort = specPort,
			Name = name,
			GameAppId = appId,
			GameModDir = modDir,
			GameDescription = description,
			NumPlayers = players + bots,
			MaxPlayers = maxPlayers,
			NumBots = bots,
			Map = map,
			Type = type,
			Secure = secure,
			Password = password,
			Version = version,
			Keywords = keywords,
			SteamId = steamId,
			Environment = env
		};
	}

	/// Translates server from SteamQuery.NET
	private static Server TranslateServer(GameServer src)
	{
		string keywords = src.Information.Keywords.Trim(',');
		
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
			Keywords = keywords.IsWhiteSpace() ? null : keywords,
			SteamId = src.Information.SteamId,
			Environment = src.Information.Environment.ToString(),
			Ping = TimeSpan.Zero
		};
	}

	/// Translates player from SteamQuery.NET
	private static ServerPlayer TranslatePlayer(SteamQueryPlayer src)
	{
		return new ServerPlayer
		{
			Name = src.Name,
			Score = src.Score,
			Duration = src.DurationTimeSpan
		};
	}
	
	/// Translates server rule from SteamQuery.NET
	private static ServerRule TranslateRule(SteamQueryRule src)
	{
		return new ServerRule
		{
			Name = src.Name,
			Value = src.Value
		};
	}
}