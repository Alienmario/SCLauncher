using Avalonia.Controls;
using Avalonia.Interactivity;
using SCLauncher.ui.controls;
using SCLauncher.ui.design;

namespace SCLauncher.ui.views.profiles;

public partial class DeleteProfileDialog : BaseDialogWindow
{
	
	public DeleteProfileDialog()
	{
		InitializeComponent();
		if (Design.IsDesignMode)
		{
			DataContext = DAppProfile.Instance;
		}

		CancelButton.Click += OnCancelClick;
		DeleteButton.Click += OnDeleteClick;
	}

	private void OnCancelClick(object? sender, RoutedEventArgs e)
	{
		Close(false);
	}

	private void OnDeleteClick(object? sender, RoutedEventArgs e)
	{
		Close(true);
	}
	
}
