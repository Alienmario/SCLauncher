using System.Collections.ObjectModel;
using Avalonia.Controls;
using SCLauncher.model;

namespace SCLauncher.ui.controls;

public partial class StatusMessageViewer : UserControl
{
	public new ObservableCollection<StatusMessage>? DataContext
	{
		get => (ObservableCollection<StatusMessage>?) base.DataContext;
		set => base.DataContext = value;
	}

	public StatusMessageViewer()
	{
		InitializeComponent();
		
		if (!Design.IsDesignMode)
			DataContext = [];
	}

	public void AddMessage(StatusMessage message, bool scroll = true)
	{
		DataContext?.Add(message);
		if (scroll && Scroller.Offset.NearlyEquals(Scroller.ScrollBarMaximum))
		{
			Scroller.ScrollToEnd();
		}
	}

	public void Clear() => DataContext?.Clear();

}