using System;
using System.Diagnostics;

namespace SCLauncher.Shared.Service;

public interface IServerControlService
{
	event DataReceivedEventHandler? OutputReceived;

	event DataReceivedEventHandler? ErrorReceived;

	event EventHandler<bool>? StateChanged;

	bool IsRunning { get; }

	bool Start();

	void Stop();

	void Command(string cmd);
}
