using Avalonia.Controls;

namespace SCLauncher.ui.util;

internal static class AvaloniaExtensions
{
	extension(Panel panel)
	{
		public Control? VisibleChild
		{
			set
			{
				foreach (Control panelChild in panel.Children)
				{
					panelChild.IsVisible = ReferenceEquals(panelChild, value);
				}
			}
			get
			{
				foreach (Control panelChild in panel.Children)
				{
					if (panelChild.IsVisible)
						return panelChild;
				}
				return null;
			}
		}
	}
}