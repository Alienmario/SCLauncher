using Avalonia.Controls;

namespace SCLauncher.UI.Util;

internal static class AvaloniaExtensions
{
	extension(Panel panel)
	{
		/// Gets or sets the currently visible child control of the panel, hiding all others.
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