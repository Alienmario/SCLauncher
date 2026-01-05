using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SCLauncher.ui.converters;

public class EnumDescriptionConverter : IValueConverter
{
	public static EnumDescriptionConverter Instance { get; } = new();
	
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not Enum enumValue)
			return value;

		return enumValue.GetDescription();
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}