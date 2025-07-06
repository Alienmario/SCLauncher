using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
		InitializeComponent();
	}

	private async void BrowseClicked(object? sender, RoutedEventArgs args)
	{
		try
		{
			string? result = await FilePickerUtil.PickFolder(this, Title, Text);
			if (result != null)
			{
				Text = result;
			}
		}
		catch (Exception e)
		{
			e.Log();
		}
	}
	
}