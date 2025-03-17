using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using SCLauncher.model;

namespace SCLauncher.ui.util;

public static class ConsoleTextUtils
{
	
	private static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Colors.Red);
	private static readonly SolidColorBrush WarningBrush = new SolidColorBrush(Colors.Orange);
	private static readonly SolidColorBrush SuccessBrush = new SolidColorBrush(Colors.GreenYellow);
	
	public static void AppendMessage(TextBlock textBlock, ScrollViewer scroller, StatusMessage msg)
	{
		AppendMessage(textBlock, msg);
		if (scroller.Offset.NearlyEquals(scroller.ScrollBarMaximum))
		{
			scroller.ScrollToEnd();
		}
	}
	
	public static void AppendMessage(TextBlock textBlock, StatusMessage msg)
	{
		Run run = new Run($"{msg.Time:HH:mm:ss}  {msg.Text}\n")
		{
			Foreground = msg.MessageStatus switch
			{
				MessageStatus.Success => SuccessBrush,
				MessageStatus.Warning => WarningBrush,
				MessageStatus.Error => ErrorBrush,
				_ => textBlock.Foreground
			}
		};

		textBlock.Inlines ??= new InlineCollection();
		textBlock.Inlines.Add(run);
	}
}