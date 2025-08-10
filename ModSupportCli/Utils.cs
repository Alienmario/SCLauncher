using ModSupportLib.Repository;

namespace ModSupportCli;

public static class Utils
{
	public static void PrintMods(List<ModInfo> modList)
	{
		foreach (ModInfo modInfo in modList)
		{
			Console.Out.WriteLine(modInfo);
		}
	}
	
	/// Extension method for extracting all nested messages from exceptions.
	public static string GetAllMessages(this Exception? e, string delimiter = " - ", bool includeTypes = false)
	{
		var messages = new List<string>();
		do
		{
			if (!string.IsNullOrWhiteSpace(e!.Message))
				messages.Add((includeTypes ? e.GetType() + ": " : "") + e.Message);
			e = e.InnerException;
		}
		while (e != null);
		return string.Join(delimiter, messages);
	}
}