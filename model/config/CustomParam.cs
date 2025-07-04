namespace SCLauncher.model.config;

public record CustomParam(string Key, string Value)
{
	
	public string Key { get; set; } = Key;
	public string Value { get; set; } = Value;

	public CustomParam() : this(string.Empty, string.Empty) {}
	
}