using System;
using System.Windows.Input;

namespace SCLauncher.ui.util;

public class BaseCommand(Action executeAction) : ICommand
{
	public event EventHandler? CanExecuteChanged;

	public bool CanExecute(object? parameter) => true;
	public void Execute(object? parameter) => executeAction();
}