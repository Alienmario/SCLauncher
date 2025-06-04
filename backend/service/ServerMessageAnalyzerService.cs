using System.Text.RegularExpressions;

namespace SCLauncher.backend.service;

public partial class ServerMessageAnalyzerService
{
	public bool AutoReset { get; set; } = true;
	
	public string? PublicIp { get; private set; }
	
	public string? LocalIp { get; private set; }
	
	public int? ServerPort { get; private set; }
	
	public int? Clientport { get; private set; }

	public ServerMessageAnalyzerService(ServerControlService controlService)
	{
		controlService.StateChanged += OnServerStateChanged;
	}

	public void Reset()
	{
		PublicIp = null;
		LocalIp = null;
		ServerPort = null;
		Clientport = null;
	}

	[GeneratedRegex(@"\[srccoop.*\.smx\] Could not obtain gamedata")]
	private static partial Regex SourceCoopBadGamedataRegex();
	
	[GeneratedRegex(@"^\[STEAM\]\s+Public IP is (.+)\.$")]
	private static partial Regex PublicIpRegex();
	
	[GeneratedRegex(@"^Network: IP (.+), mode (?:.+), dedicated (?:Yes|No), ports (\d+) SV / (\d+) CL$")]
	private static partial Regex NetworkRegex();

	public string? AnalyzeMessage(string msg)
	{
		var publicIpMatch = PublicIpRegex().Match(msg);
		if (publicIpMatch.Success)
		{
			PublicIp = publicIpMatch.Groups[1].Value;
			return null;
		}

		var networkMatch = NetworkRegex().Match(msg);
		if (networkMatch.Success)
		{
			string localIp = networkMatch.Groups[1].Value;
			if (!localIp.Equals("0.0.0.0"))
				LocalIp = localIp;
			if (int.TryParse(networkMatch.Groups[2].Value, out var svPort))
				ServerPort = svPort;
			if (int.TryParse(networkMatch.Groups[3].Value, out var clPort))
				Clientport = clPort;
			return null;
		}

		if (msg.StartsWith("""
		                   Unable to load plugin "addons/metamod/bin/
		                   """)
		    || msg.Contains("metamod/bin/linux64/server.so: wrong ELF class: ELFCLASS64"))
		{
			return """
			       This error message appears because Metamod includes both 32-bit and 64-bit binaries.
			       It can be safely ignored.
			       """;
		}

		// Not using srcds_run anymore, but same solution applies -- "srcds_run: 342: ./srcds_linux: not found"
		if (msg.StartsWith("Failed to open dedicated_srv.so"))
		{
			return """
			       Please install necessary system packages using the following commands:
			       sudo dpkg --add-architecture i386
			       sudo apt update
			       sudo apt install lib32gcc-s1 lib32stdc++6
			       """;
		}

		if (msg.StartsWith("[SM] Blaming: srccoop"))
		{
			return "Please report this error to the SourceCoop team!";
		}

		if (SourceCoopBadGamedataRegex().IsMatch(msg))
		{
			return """
			       This error indicates that there might have been a game update, which has broken SourceCoop.

			       Check for SourceCoop updates.

			       Even if there are none, the developers are probably already working on it, so check back later.
			       """;
		}

		return null;
	}

	private void OnServerStateChanged(object? sender, bool running)
	{
		if (AutoReset && running)
		{
			Reset();
		}
	}
}