using ModSupportLib.Exceptions;
using ModSupportLib.Repository;

namespace ModSupportLib.Service;

public static class ModActivatorService
{
	
	/// Sets active mods
	/// <param name="standaloneMod">null for game default</param>
	/// <param name="mountedMods">mods to mount into </param>
	/// <exception cref="ActivationException">if there is an issue</exception>
	public static async Task SetActiveMods(CommonArgs commonArgs, ModInfo? standaloneMod, List<ModInfo> mountedMods)
	{
		// Validate input
		
		if (standaloneMod != null && standaloneMod.Type != ModType.Standalone)
		{
			throw new ActivationException($"Mod '{standaloneMod}' is not standalone type.");
		}
		foreach (ModInfo mountedMod in mountedMods)
		{
			if (mountedMod.Type != ModType.Mount)
			{
				throw new ActivationException($"Mod '{mountedMod}' is not mounted type.");
			}
		}
		
		// foreach (ModInfo modInfo in mountedMods)
		// {
		// 	string srcDir = ModRepositoryService.GetModAbsPath(commonArgs, modInfo);
		// 	
		// }
	}
	
}