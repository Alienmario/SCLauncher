namespace SCLauncher.model.install;

public readonly record struct ProgressUpdate
{
	public int? Step { get; init; }
	public int? NumSteps { get; init; }
	public string? Text { get; init; }
}