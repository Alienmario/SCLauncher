using System;
using Avalonia.Controls;

namespace SCLauncher.ui.views.serverbrowser;

public partial class ServerDetailsDialog : Window
{
	public ServerDetailsDialog()
	{
		InitializeComponent();
	}

	protected override void OnSizeChanged(SizeChangedEventArgs e)
	{
		base.OnSizeChanged(e);
		
		// Recenter the window on each resize
		
		if (e.HeightChanged)
			Position = Position.WithY(Position.Y + (int)Math.Round((e.PreviousSize.Height - e.NewSize.Height) / 2));
	}
}