using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SCLauncher.ui.util;

namespace SCLauncher.ui.controls;

public partial class ConsoleCommandTextBox : TextBox
{
	protected override Type StyleKeyOverride => typeof(TextBox);
	
	private readonly List<string> commandHistory = [];
	private int commandHistoryIdx = int.MaxValue;
    
	public event EventHandler<string?>? CommandSubmitted;

	public ConsoleCommandTextBox()
	{
		InitializeComponent();
		KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.Up), Command = MoveToPreviousCommand });
		KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.Down), Command = MoveToNextCommand });
	}
	
	public new string? Text
	{
		get => GetValue(TextProperty);
		set
		{
			SetValue(TextProperty, value);
			if (string.IsNullOrEmpty(value))
			{
				CaretIndex = 0; // avalonia caret bug workaround
			}
		}
	}

	private void SubmitClicked(object? sender, RoutedEventArgs e)
	{
		var text = Text?.Trim();
		if (!string.IsNullOrEmpty(text))
		{
			int idx = commandHistory.FindLastIndex(s => s.Equals(text));
			if (idx != -1)
			{
				commandHistory.RemoveAt(idx);
			}
			commandHistory.Add(text);
		}
		commandHistoryIdx = commandHistory.Count;
		CommandSubmitted?.Invoke(this, text);
		Text = null;
	}

	private ICommand MoveToPreviousCommand => new BaseCommand(() =>
	{
		if (commandHistory.Count == 0)
			return;
		
		if (commandHistoryIdx > 0)
			commandHistoryIdx--;
		
		Text = commandHistory[commandHistoryIdx];
		CaretIndex = Text.Length;
	});

	private ICommand MoveToNextCommand => new BaseCommand(() =>
	{
		if (commandHistoryIdx < commandHistory.Count - 1)
		{
			Text = commandHistory[++commandHistoryIdx];
			CaretIndex = Text.Length;
		}
		else
		{
			commandHistoryIdx = commandHistory.Count;
			Text = null;
		}
	});

}