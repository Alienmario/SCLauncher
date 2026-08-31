using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SCLauncher.ui.converters;

public class BoolToStringConverter : IValueConverter
{
	public static BoolToStringConverter Instance { get; } = new();
	
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is bool boolValue)
		{
			if (parameter is string paramString && !string.IsNullOrEmpty(paramString))
			{
				var parts = paramString.Split('|');
				if (parts.Length == 2)
				{
					return boolValue ? parts[0].Trim() : parts[1].Trim();
				}
			}
			// Fallback
			return boolValue ? "Yes" : "No";
		}
		return null;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}