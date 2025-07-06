using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SCLauncher.ui.converters;

public class DecorateIfNotNullConverter : IValueConverter
{
	private readonly TextDecorationCollection decorationCollection;

	public DecorateIfNotNullConverter(TextDecorationCollection decorationCollection)
	{
		this.decorationCollection = decorationCollection;
	}
	
	public DecorateIfNotNullConverter(TextDecoration decoration)
	{
		decorationCollection = [decoration];
	}

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is not null ? decorationCollection : null;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}