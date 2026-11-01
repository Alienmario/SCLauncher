namespace SCLauncher.Shared.Service;

public interface IServerMessageAnalyzerService
{
	bool AutoReset { get; set; }

	string? LocalIp { get; }

	int? LocalServerPort { get; }

	string? PublicIp { get; }

	int? PublicServerPort { get; }

	bool IsFakeIp { get; }

	void Reset();

	/// Analyzes server console messages, stores contextual data for later retrieval
	/// and returns helpful details when applicable.
	string? AnalyzeMessage(string msg);
}
