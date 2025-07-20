namespace ModSupportLib;

public class ModQuery
{
	public HashSet<string> Names { get; init; } = new(StringComparer.OrdinalIgnoreCase);
	public HashSet<ulong> WorkshopIds { get; init; } = [];
	
	public static ModQuery ForName(string name)
	{
		return new ModQuery { Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { name } };
	}
	
	public static ModQuery ForWorkshopId(ulong wid)
	{
		return new ModQuery { WorkshopIds = [wid] };
	}
}