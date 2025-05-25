using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SCLauncher.ui.converters;

public class UnderlineIfNotNullConverter : IValueConverter
{
	public static readonly UnderlineIfNotNullConverter Instance = new();
    
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is not null ? TextDecorations.Underline : null;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}