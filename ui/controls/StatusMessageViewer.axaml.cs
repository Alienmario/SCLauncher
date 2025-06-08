using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using SCLauncher.model;

namespace SCLauncher.ui.controls;

public partial class StatusMessageViewer : UserControl
{
	public new ObservableCollection<StatusMessage>? DataContext
	{
		get => (ObservableCollection<StatusMessage>?) base.DataContext;
		set => base.DataContext = value;
	}

	public int Limit { get; set; } = int.MaxValue;
	
	public StatusMessageViewer()
	{
		InitializeComponent();
		
		if (!Design.IsDesignMode)
			DataContext = [];
	}

	public void AddMessage(StatusMessage message, bool scroll = true, bool jumpScroll = false)
	{
		bool wasScrolledToMax = Scroller.Offset.NearlyEquals(Scroller.ScrollBarMaximum);
		
		if (DataContext != null)
		{
			DataContext.Add(message);
			if (DataContext.Count > Limit)
			{
				DataContext.RemoveAt(0);
			}
		}
		
		if (scroll && (jumpScroll || wasScrolledToMax))
		{
			UpdateLayout();
			ScrollToEnd();
		}
	}

	public void Clear() => DataContext?.Clear();
	
	public void ScrollToEnd() => Scroller.ScrollToEnd();

	private void Message_PointerTapped(object? sender, TappedEventArgs args)
	{
		if (sender is Control ctl && ctl.DataContext is StatusMessage msg && msg.Details != null)
		{
			FlyoutBase.ShowAttachedFlyout(ctl);
		}
	}
}