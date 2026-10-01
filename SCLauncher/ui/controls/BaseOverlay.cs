using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace SCLauncher.ui.controls;

/// Overlay baseclass with translucent backdrop.
/// Can be relatively sized with <see cref="WidthPercent"/> and <see cref="HeightPercent"/>
/// or absolutely padded with standard padding.
[TemplatePart("PART_Backdrop", typeof(Control))]
[TemplatePart("PART_ContentCard", typeof(Control))]
public partial class BaseOverlay : ContentControl
{
	protected override Type StyleKeyOverride => typeof(BaseOverlay);
	
	[GeneratedStyledProperty(DefaultValue = 100)]
	public partial double WidthPercent { get; set; }
	
	[GeneratedStyledProperty(DefaultValue = 100)]
	public partial double HeightPercent { get; set; }
	
	[GeneratedDirectProperty]
	public partial bool IsOpen { get; private set; }

	/// <summary>Raised when the overlay opens.</summary>
	public event EventHandler? Opened;

	/// <summary>Raised when the overlay closes.</summary>
	public event EventHandler? Closed;
	
	private Control? _backdrop;
	private Control? _contentCard;

	public virtual void Open(Control target)
    {
	    if (!IsOpen)
	    {
		    var overlayLayer = OverlayLayer.GetOverlayLayer(target);
		    if (overlayLayer == null)
			    throw new Exception("Overlay layer is not available");
		    
		    overlayLayer.Children.Add(this);
		    IsOpen = true;
		    Opened?.Invoke(this, EventArgs.Empty);
	    }
    }

    public virtual void Close()
    {
	    if (IsOpen)
	    {
		    OverlayLayer.GetOverlayLayer(this)?.Children.Remove(this);
		    IsOpen = false;
		    Closed?.Invoke(this, EventArgs.Empty);
	    }
    }
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs args)
    {
	    base.OnApplyTemplate(args);

	    if (_backdrop is not null)
	    {
		    _backdrop.PointerPressed -= OnBackdropPressed;
	    }
	    
	    _backdrop = args.NameScope.Find<Control>("PART_Backdrop");
	    _backdrop?.PointerPressed += OnBackdropPressed;
	    
	    _contentCard = args.NameScope.Find<Control>("PART_ContentCard");
    }
    
    protected override Size MeasureOverride(Size availableSize)
    {
	    return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
	    if (_contentCard is not null)
	    {
		    _contentCard.MaxWidth = finalSize.Width * WidthPercent * 0.01;
		    _contentCard.MaxHeight = finalSize.Height * HeightPercent * 0.01;
	    }
	    return base.ArrangeOverride(finalSize);
    }

    protected virtual void OnBackdropPressed(object? sender, PointerPressedEventArgs args)
    {
	    if (sender == args.Source)
	        Close();
    }
}
