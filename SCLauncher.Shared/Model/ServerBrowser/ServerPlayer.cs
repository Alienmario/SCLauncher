using System;
using System.Globalization;

namespace SCLauncher.Shared.Model.ServerBrowser;

public record ServerPlayer
{
	public required string Name { get; init; }
	public required long Score { get; init; }
	public required TimeSpan Duration { get; init; }
	
	public string DurationFormatted => Duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
}