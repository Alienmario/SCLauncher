using Avalonia.Controls;
using Avalonia.Interactivity;
using SCLauncher.UI.Controls;
using SCLauncher.UI.DesignTime;

namespace SCLauncher.UI.Views.Profiles.Crud;

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
