using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SCLauncher.model;

namespace SCLauncher.ui.converters;

public class MessageStatusColorConverter() : FuncValueConverter<MessageStatus?, object?>(status =>
{
	return status switch
	{
		MessageStatus.Success => SuccessBrush,
		MessageStatus.Warning => WarningBrush,
		MessageStatus.Error => ErrorBrush,
		_ => AvaloniaProperty.UnsetValue
	};
})
{
	public static MessageStatusColorConverter Instance { get; } = new();
	
	private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Colors.Red);
	private static readonly SolidColorBrush WarningBrush = new SolidColorBrush(Colors.Orange);
	private static readonly SolidColorBrush SuccessBrush = new SolidColorBrush(Colors.GreenYellow);
}