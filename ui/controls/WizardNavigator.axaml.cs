using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SCLauncher.ui.controls;

public partial class WizardNavigator : UserControl
{
	public interface IWizardContent
	{
		void OnAttachedToWizard(WizardNavigator wizard, bool reAttached) {}
		void OnNextPageRequest(WizardNavigator wizard) {}
		bool OnPrevPageRequest(WizardNavigator wizard)
		{
			return true;
		}
	}

	private readonly Stack<object> _navStack = new();

	public WizardNavigator()
	{
		InitializeComponent();
		ResetControls();
		ForwardButton.Click += ForwardClicked;
		BackButton.Click += BackClicked;
	}

	public event EventHandler<RoutedEventArgs>? CancelClick
	{
		add => CancelButton.Click += value;
		remove => CancelButton.Click -= value;
	}

	public bool ShowNavBar
	{
		get => NavBar.IsVisible;
		set => NavBar.IsVisible = value;
	}

	private new object? Content
	{
		get => ContentArea.Content;
		set => ContentArea.Content = value;
	}

	private bool AllowBack { get; set; }
	private bool AllowForward { get; set; }
	private bool AllowCancel { get; set; }

	public int GetStackCount() => _navStack.Count;

	private void SetContent(object? content, bool pushStack, bool reAttached)
	{
		if (pushStack && Content != null)
		{
			_navStack.Push(Content);
		}
		Content = content;
		if (content is IWizardContent wc)
		{
			wc.OnAttachedToWizard(this, reAttached);
		}
		UpdateControls();
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

	public void Reset()
	{
		_navStack.Clear();
		Content = null;
		ResetControls();
	}

	public void ResetControls()
	{
		SetControls(true, true, true);
	}

	private void UpdateControls()
	{
		BackButton.IsEnabled = AllowBack && GetStackCount() > 0;
		ForwardButton.IsEnabled = AllowForward;
		CancelButton.IsVisible = AllowCancel;
		ShowNavBar = BackButton.IsEnabled || ForwardButton.IsEnabled || CancelButton.IsVisible;
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

		if (_navStack.TryPop(out var prev))
		{
			SetContent(prev, false, true);
		}
	}
}