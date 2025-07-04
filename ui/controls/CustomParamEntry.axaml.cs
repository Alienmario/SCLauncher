using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using SCLauncher.model.config;

namespace SCLauncher.ui.controls;

public partial class CustomParamEntry : UserControl
{

	public static readonly StyledProperty<bool> ShowHintProperty =
		AvaloniaProperty.Register<CustomParamEntry, bool>(nameof(ShowHint), defaultValue: true);

	public bool ShowHint
	{
		get => GetValue(ShowHintProperty);
		set => SetValue(ShowHintProperty, value);
	}

	public ObservableCollection<CustomParam>? CustomParams => DataContext as ObservableCollection<CustomParam>;

	public CustomParamEntry()
	{
		InitializeComponent();
	}
	
	private void AddNewParameterClicked(object? sender, RoutedEventArgs e)
	{
		if (CustomParams != null)
		{
			CustomParams.Add(new CustomParam());

			// focus the added param
			UpdateLayout();
			TextBox? textBox = this.GetVisualDescendants().OfType<TextBox>()
				.LastOrDefault(x => x.Classes.Contains("CustomParamKey"));
			textBox?.Focus();
			AddParameterButton.BringIntoView(AddParameterButton.Bounds);
		}
	}

	private void OnParamTextLostFocus(object? sender, RoutedEventArgs e)
	{
		if (e.Source is Control { DataContext: CustomParam p })
		{
			// check if new focus is on the same row
			if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement()
				    is not Control focusedElement || !ReferenceEquals(focusedElement.DataContext, p))
			{
				// remove row that just lost focus if it's empty
				if (string.IsNullOrEmpty(p.Key) && string.IsNullOrEmpty(p.Value))
				{
					CustomParams?.Remove(p);
				}
			}
		}
	}
	
}