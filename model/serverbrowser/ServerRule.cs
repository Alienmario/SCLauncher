namespace SCLauncher.model.serverbrowser;

public record ServerRule
{
	public required string Name { get; init; }
	public required string Value { get; init; }
}