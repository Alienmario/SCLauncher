using System;
using System.ComponentModel;

namespace SCLauncher.Shared.Extensions;

public static class EnumExtensions
{
	/// Extracts an enum value's description (<see cref="DescriptionAttribute"/>).
	/// Falls back to the definition string.
	public static string GetDescription(this Enum enumValue)
	{
		var field = enumValue.GetType().GetField(enumValue.ToString());
		if (field == null)
			return enumValue.ToString();

		if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
		{
			return attribute.Description;
		}

		return enumValue.ToString();
	}
}
