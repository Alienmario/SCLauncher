using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.model.serverbrowser;
using SteamQuery;
using SteamQuery.Models;

namespace SCLauncher.backend.service;

public class ServerBrowserService
{
	private const int MaxConcurrentQueries = 50;
	private static readonly TimeSpan ServerQuerySendTimeout = TimeSpan.FromSeconds(5);
	private static readonly TimeSpan ServerQueryReadTimeout = TimeSpan.FromSeconds(5);

	// This constructor creates connections
	// private readonly MasterServer masterServer = new(MasterServerEndPoint.Source);
	private readonly MasterServerQueryFilters filter;

	public ServerBrowserService(BackendService backend)
	{
		filter = new MasterServerQueryFiltersBuilder()
			.WithAppId((int)backend.ActiveApp.GameAppId)
			.Build();
	}

	public async IAsyncEnumerable<Server> GetServers([EnumeratorCancellation] CancellationToken ct = default)
	{
		var queryTasks = new List<Task<Server?>>();
	
		using MasterServer masterServer = new(MasterServerEndPoint.Source);
		await foreach (var masterServerResponse in masterServer.GetServersAsync(filter, cancellationToken: ct))
		{
			ct.ThrowIfCancellationRequested();
			queryTasks.Add(QueryServer(masterServerResponse, ct));
	
			if (queryTasks.Count >= MaxConcurrentQueries)
			{
				Server? server = await WaitForQueryResponses();
				if (server != null) yield return server;
			}
		}
	
		while (queryTasks.Count > 0)
		{
			Server? server = await WaitForQueryResponses();
			if (server != null) yield return server;
		}
		
		async Task<Server?> WaitForQueryResponses()
		{
			var finished = await Task.WhenAny(queryTasks);
			ct.ThrowIfCancellationRequested();
			queryTasks.Remove(finished);
			return await finished;
		}
	}

	public static async Task<Server?> QueryServer(string endpoint, CancellationToken ct = default)
	{
		try
		{
			using var gameServer = new GameServer(endpoint);
			return await QueryServer(gameServer, ct);
		}
		catch (Exception)
		{
			return null;
		}
	}
	
	private static async Task<Server?> QueryServer(MasterServerResponse masterServerResponse, CancellationToken ct)
	{
		try
		{
			using var gameServer = new GameServer(masterServerResponse);
			return await QueryServer(gameServer, ct);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static async Task<Server?> QueryServer(GameServer gameServer, CancellationToken ct)
	{
		gameServer.SendTimeout = ServerQuerySendTimeout;
		gameServer.ReceiveTimeout = ServerQueryReadTimeout;
			
		var stopwatch = Stopwatch.StartNew();
		await gameServer.PerformQueryAsync(cancellationToken: ct);
		stopwatch.Stop();
		return TranslateServer(gameServer, stopwatch.Elapsed / 3); // did 3? queries
	}

	private static Server TranslateServer(GameServer src, TimeSpan ping)
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
			VAC = src.Information.VacSecured,
			Password = !src.Information.Visible,
			Version = src.Information.Version,
			Keywords = src.Information.Keywords.Trim(','),
			SteamId = src.Information.SteamId,
			Environment = src.Information.Environment.ToString(),
			Ping = ping
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