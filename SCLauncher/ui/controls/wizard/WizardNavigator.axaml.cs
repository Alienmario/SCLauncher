using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace SCLauncher.ui.controls.wizard;

public partial class WizardNavigator : UserControl
{
	public event EventHandler? Exit;
	
	public object? NavBar
	{
		get => NavBarContentControl.Content;
		set => NavBarContentControl.Content = value;
	}

	public bool IsNavBarVisible
	{
		get => NavBarHost.IsVisible;
		set => NavBarHost.IsVisible = value;
	}

	public object? PageContent
	{
		get => ContentControl.Content;
		set => SetPageContent(value);
	}

	public bool ForwardButtonRunsAction
	{
		get;
		set
		{
			ForwardButtonIcon.Data = (Geometry?)App.GetResource(value ? "rocket_regular" : "chevron_right_regular");
			field = value;
		}
	}

	public int StackCount => _navStack.Count;

	private readonly Stack<object> _navStack = new();
	private bool _allowBack;
	private bool _allowForward;

	public WizardNavigator()
	{
		InitializeComponent();
		ResetControls();
		ForwardButton.Click += ForwardClicked;
		BackButton.Click += BackClicked;

		// Setup Window-level mouse navigation listener
		TopLevel? topLevel = null;
		AttachedToVisualTree += delegate
		{
			topLevel = TopLevel.GetTopLevel(this)!;
			topLevel.PointerReleased += OnPointerReleased;
		};
		DetachedFromVisualTree += delegate
		{
			topLevel?.PointerReleased -= OnPointerReleased;
		};
	}
	
	private void SetPageContent(object? content, bool stackOldPage, bool reAttached)
	{
		if (stackOldPage && PageContent != null)
		{
			_navStack.Push(PageContent);
		}
		(PageContent as IWizardPage)?.OnDetachedFromWizard(this, stackOldPage);
		ResetControls();
		ContentControl.Content = content;
		(content as IWizardPage)?.OnAttachedToWizard(this, reAttached);
	}
	
	public void SetPageContent(object? content, bool stackOldPage = true)
	{
		SetPageContent(content, stackOldPage, false);
	}

	public void FastForward(params object[] pages)
	{
		if (pages.Length == 0) return;
		
		SetPageContent(null);

		for (int i = 0; i < pages.Length - 1; i++)
		{
			_navStack.Push(pages[i]);
		}

		SetPageContent(pages[^1]);
	}

	public void SetControls(bool? back = null, bool? forward = null)
	{
		_allowBack = back ?? _allowBack;
		_allowForward = forward ?? _allowForward;
		UpdateControls();
	}

	public void ResetControls()
	{
		ForwardButtonRunsAction = false;
		(NavBar as IWizardNavBar)?.Reset();
		SetControls(true, true);
	}

	private void UpdateControls()
	{
		BackButton.IsEnabled = _allowBack && StackCount > 0;
		ForwardButton.IsEnabled = _allowForward;
	}

	public void Reset(bool clearEventHandlers = false)
	{
		_navStack.Clear();
		(PageContent as IWizardPage)?.OnDetachedFromWizard(this, false);
		ContentControl.Content = null;
		ResetControls();
		if (clearEventHandlers)
		{
			Exit = null;
		}
	}

	private void ForwardClicked(object? sender, RoutedEventArgs e)
	{
		if (PageContent is IWizardPage wp)
		{
			wp.OnNextPageRequest(this);
		}
	}

	private void BackClicked(object? sender, RoutedEventArgs e)
	{
		if (PageContent is IWizardPage wp && !wp.OnPrevPageRequest(this))
		{
			return;
		}

		if (_navStack.TryPop(out var prev))
		{
			SetPageContent(prev, false, true);
		}
	}

	public void CancelClicked()
	{
		RequestExit();
	}
	
	public void RequestExit()
	{
		Exit?.Invoke(this, EventArgs.Empty);
	}
	
	private void OnPointerReleased(object? sender, PointerReleasedEventArgs args)
	{
		if (args.InitialPressMouseButton == MouseButton.XButton1)
		{
			if (_allowBack)
			{
				BackClicked(sender, args);
			}
		}
		else if (args.InitialPressMouseButton == MouseButton.XButton2)
		{
			if (_allowForward && !ForwardButtonRunsAction)
			{
				ForwardClicked(sender, args);
			}
		}
	}
}