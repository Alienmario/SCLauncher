using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SCLauncher.ui.util;

namespace SCLauncher.ui.controls;

public partial class FilePickerTextBox : TextBox
{
	protected override Type StyleKeyOverride => typeof(TextBox);

	public enum PickerMode
	{
		File,
		Folder
	}
	
	public PickerMode Mode { get; set; } = PickerMode.File;
	
	public string Title { get; set; } = "";

	public FilePickerTextBox()
	{
		AvaloniaXamlLoader.Load(this);
	}

	private void BrowseClicked(object? sender, RoutedEventArgs e)
	{
		string? start = Text;
		Task.Run(async () =>
		{
			string? result = await FilePickerUtil.PickFolder(this, Title, start);
			if (result != null)
			{
				Dispatcher.UIThread.Post(() => Text = result);
			}
		});
	}
	
}