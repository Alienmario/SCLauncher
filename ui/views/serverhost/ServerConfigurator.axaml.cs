using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using SCLauncher.backend.service;
using SCLauncher.model;

namespace SCLauncher.ui.views.serverhost;

public partial class ServerConfigurator : UserControl
{
	
	public ServerConfigurator()
	{
		InitializeComponent();
		
		if (Design.IsDesignMode)
			return;

		DataContext = App.GetService<BackendService>().GetServerConfig();
	}

	public void ResetToDefaults()
	{
		DataContext = App.GetService<BackendService>().GetServerConfig(true);
	}
	
	private void AddNewParameterClicked(object? sender, RoutedEventArgs e)
	{
		if (DataContext is ServerConfiguration cfg)
		{
			cfg.CustomParams.Add(new ServerConfiguration.CustomParam());

			// focus the new param
			UpdateLayout();
			this.GetVisualDescendants().OfType<TextBox>()
				.LastOrDefault(x => x.Classes.Contains("CustomParamKey"))
				?.Focus();
		}
	}

	private void OnCustomParamTextLostFocus(object? sender, RoutedEventArgs e)
	{
		if (e.Source is Control { DataContext: ServerConfiguration.CustomParam p })
		{
			// check if new focus is on the same row
			if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement()
				    is not Control focusedElement || !ReferenceEquals(focusedElement.DataContext, p))
			{
				// remove row that just lost focus if it's empty
				if (string.IsNullOrEmpty(p.Key) && string.IsNullOrEmpty(p.Value))
				{
					(DataContext as ServerConfiguration)?.CustomParams.Remove(p);
				}
			}
		}
	}
}