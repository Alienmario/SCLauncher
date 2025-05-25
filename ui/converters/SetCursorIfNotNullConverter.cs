using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Input;

namespace SCLauncher.ui.converters;

public class SetCursorIfNotNullConverter(string cursorStr) : IValueConverter
{
	public static readonly SetCursorIfNotNullConverter Hand = new("Hand");

	private readonly Cursor cursor = Cursor.Parse(cursorStr);

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is not null ? cursor : null;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}