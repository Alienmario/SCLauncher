using Avalonia.Interactivity;
using SCLauncher.UI.Controls;

namespace SCLauncher.UI.Views.Settings;

public partial class EraseDataDialog : BaseDialogWindow
{
	
	public EraseDataDialog()
	{
		InitializeComponent();

		CancelButton.Click += OnCancelClicked;
		EraseButton.Click += OnEraseClicked;
	}

	private void OnCancelClicked(object? sender, RoutedEventArgs e)
	{
		Close(false);
	}

	private void OnEraseClicked(object? sender, RoutedEventArgs e)
	{
		Close(true);
	}
	
}
