using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SCLauncher.ui.converters;

public class PluralizationConverter : IValueConverter
{
	public static readonly PluralizationConverter Instance = new();
	
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not IConvertible convertibleValue || parameter is not string format)
			return null;
		
		var count = Math.Abs(convertibleValue.ToInt64(culture));
		var formats = format.Split('|');
		
		// [bike] 0 bikes, 1 bike, 2 bikes
		if (formats.Length == 1)
			return string.Format(culture, "{0} {1}{2}", value, formats[0], count != 1 ? 's' : "");
		
		// [knife|knives] 0 knives, 1 knife, 2 knives
		// [At most {0} knife|Max knives: {0}] Max knives: 0, At most 1 knife, Max knives: 2
		// Note:
		// Only the 3 param variation can discard numbers from the result if not included in the format string.
		// If 2 params are provided and no formatting done, simple singular|plural word pair is assumed and the number is prepended.
		if (formats.Length == 2)
		{
			int i = (count == 1) ? 0 : 1;
			string s = string.Format(culture, formats[i], value);
			if (s.Equals(formats[i], StringComparison.Ordinal))
			{
				return convertibleValue.ToString(culture) + ' ' + s;
			}

			return s;
		}

		// [empty pack|lone wolf|pack of {0} wolves] empty pack, lone wolf, pack of 2 wolves
		if (formats.Length == 3)
			return string.Format(culture, formats[count >= 2 ? 2 : count], value);

		return null;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

}