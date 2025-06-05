using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace SCLauncher.ui.controls;

public partial class WizardNavigator : UserControl
{
	public interface IWizardContent
	{
		void OnAttachedToWizard(WizardNavigator wizard, bool unstacked) {}
		void OnDetachedFromWizard(WizardNavigator wizard, bool stacked) {}
		void OnNextPageRequest(WizardNavigator wizard) {}
		bool OnPrevPageRequest(WizardNavigator wizard)
		{
			return true;
		}
	}

	public WizardNavigator()
	{
		InitializeComponent();
		ResetControls();
		ForwardButton.Click += ForwardClicked;
		BackButton.Click += BackClicked;
		CancelButton.Click += CancelClicked;
	}

	public event EventHandler? OnExit;

	public bool ShowNavBar
	{
		get => NavBar.IsVisible;
		set => NavBar.IsVisible = value;
	}

	public bool ShowProgressBar
	{
	    get => ProgressBar.IsVisible;
	    set => ProgressBar.IsVisible = value;
	}
	
	public bool ForwardButtonRunsAction
	{
		set => ForwardButtonIcon.Data = (Geometry?)App.GetResource(value ? "rocket_regular" : "chevron_right_regular");
	}
	
	public bool Completed
	{
		set => CancelLabel.Content = value ? "Close" : "Cancel";
		get => (string)CancelLabel.Content! == "Close";
	}

	private new object? Content
	{
		get => ContentArea.Child;
		set => ContentArea.Child = (Control?)value;
	}

	private readonly Stack<object> navStack = new();
	private bool AllowBack { get; set; }
	private bool AllowForward { get; set; }
	private bool AllowCancel { get; set; }

	public int GetStackCount() => navStack.Count;

	public object? GetContent() => Content;

	private void SetContent(object? content, bool pushStack, bool reAttached)
	{
		if (pushStack && Content != null)
		{
			navStack.Push(Content);
		}
		ResetControls();
		(Content as IWizardContent)?.OnDetachedFromWizard(this, pushStack);
		Content = content;
		(Content as IWizardContent)?.OnAttachedToWizard(this, reAttached);
	}
	
	public void SetContent(object? content, bool pushStack = true)
	{
		SetContent(content, pushStack, false);
	}

	public void SetControls(bool? back = null, bool? forward = null, bool? cancel = null)
	{
		AllowBack = back ?? AllowBack;
		AllowForward = forward ?? AllowForward;
		AllowCancel = cancel ?? AllowCancel;
		UpdateControls();
	}

	public void ResetControls()
	{
		ForwardButtonRunsAction = false;
		ShowProgressBar = Design.IsDesignMode;
		SetControls(true, true, true);
	}

	private void UpdateControls()
	{
		BackButton.IsEnabled = AllowBack && GetStackCount() > 0;
		ForwardButton.IsEnabled = AllowForward;
		CancelButton.IsVisible = AllowCancel;
		ShowNavBar = BackButton.IsEnabled || ForwardButton.IsEnabled || CancelButton.IsVisible;
	}

	public void Reset(bool clearEventHandlers = false)
	{
		navStack.Clear();
		Content = null;
		Completed = false;
		ResetControls();
		if (clearEventHandlers)
		{
			OnExit = null;
		}
	}

	private void ForwardClicked(object? sender, RoutedEventArgs e)
	{
		if (Content is IWizardContent wc)
		{
			wc.OnNextPageRequest(this);
		}
	}

	private void BackClicked(object? sender, RoutedEventArgs e)
	{
		if (Content is IWizardContent wc && !wc.OnPrevPageRequest(this))
		{
			return;
		}

		if (navStack.TryPop(out var prev))
		{
			SetContent(prev, false, true);
		}
	}

	private void CancelClicked(object? sender, RoutedEventArgs e)
	{
		BeforeExit();
	}

	protected virtual void BeforeExit()
	{
		OnExit?.Invoke(this, EventArgs.Empty);
	}
}