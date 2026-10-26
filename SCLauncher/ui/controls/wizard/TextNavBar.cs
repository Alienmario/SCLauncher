using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace SCLauncher.ui.controls.wizard;

public partial class TextNavBar : UserControl, IWizardNavBar
{
	// Attached property for individual pages, also usable directly
	public static readonly AttachedProperty<string?> TextProperty =
		AvaloniaProperty.RegisterAttached<TextNavBar, Control, string?>("Text");

	public static void SetText(Control ctl, string? value) =>
		ctl.SetValue(TextProperty, value, BindingPriority.Template);

	public static string? GetText(Control ctl) => ctl.GetValue(TextProperty);

	private WizardNavigator? _wizard;
	private IDisposable? _wizardPageDisposable;
	private IDisposable? _wizardPageTextDisposable;

	public TextNavBar()
	{
		InitializeComponent();
	}

	public void Reset()
	{
	}

	protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		base.OnAttachedToLogicalTree(e);

		_wizard = this.FindLogicalAncestorOfType<WizardNavigator>();
		if (_wizard != null)
		{
			_wizardPageDisposable = _wizard.GetPageContentObservable().Subscribe(new AnonymousObserver<object?>(page =>
			{
				_wizardPageTextDisposable?.Dispose();
				if (page is AvaloniaObject pao)
				{
					_wizardPageTextDisposable = pao.GetObservable(TextProperty).Subscribe(
						new AnonymousObserver<string?>(text =>
						{
							if (pao.IsSet(TextProperty))
								SetValue(TextProperty, text);
							else
								ClearValue(TextProperty);
						})
					);
				}
			}));
		}
	}

	protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromLogicalTree(e);

		_wizardPageTextDisposable?.Dispose();
		_wizardPageDisposable?.Dispose();
		_wizard = null;
	}
	
}