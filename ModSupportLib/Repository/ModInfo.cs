namespace ModSupportLib.Repository;

public record ModInfo
{
	public required string Name { get; set; }
	public string? Short { get; set; }
	public string? FirstMap { get; set; }
	public ulong? Workshop { get; set; }
	public required ModType Type { get; set; }
	public string Path { get; set; } = ".";
	public DateTime? LastUpdated { get; internal set; }

	public virtual bool Equals(ModInfo? other)
	{
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;
		return Name == other.Name && Short == other.Short && FirstMap == other.FirstMap && Workshop == other.Workshop
		       && Type == other.Type && Path == other.Path;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Name, Short, FirstMap, Workshop, (int)Type, Path);
	}

	public ModPathType GetPathType()
	{
		if (System.IO.Path.IsPathFullyQualified(Path))
		{
			return ModPathType.AbsolutePath;
		}
		return Workshop != null ? ModPathType.RelativeWorkshopPath : ModPathType.RelativePath;
	}

	public override string ToString()
	{
		List<string> parts =
		[
			$"Name: '{Name}'"
		];

		if (Short != null)
			parts.Add($"Short: '{Short}'");
		parts.Add($"Type: {Type}");
		if (Workshop != null)
			parts.Add($"Workshop ID: {Workshop}");
		if (FirstMap != null)
			parts.Add($"First map: '{FirstMap}'");
		parts.Add($"Path: '{Path}'");
		parts.Add($"Path type: {GetPathType()}");
	
		return string.Join(", ", parts);
	}
	
}