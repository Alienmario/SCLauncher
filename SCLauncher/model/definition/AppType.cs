using System.ComponentModel;
using SCLauncher.model.serverinstall;

namespace SCLauncher.model.definition;

/// <seealso cref="AppDefinitions"/>
/// <seealso cref="ServerInstallComponent"/>
public enum AppType
{
	[Description("Black Mesa")]
	BlackMesa,
	[Description("Half-Life 2 Deathmatch")]
	HL2DM,
	[Description("Team Fortress 2")]
	TF2,
	[Description("Counter-Strike: Source")]
	CSS,
	[Description("Day of Defeat: Source")]
	DODS,
	[Description("Garry's Mod")]
	GMOD,
	[Description("Left 4 Dead")]
	Left4Dead,
	[Description("Left 4 Dead 2")]
	Left4Dead2,
	[Description("Insurgency")]
	Insurgency,
	[Description("Synergy")]
	Synergy,
	[Description("No More Room in Hell")]
	NMRIH,
	[Description("Source SDK Base 2013")]
	SDK2013,
}
