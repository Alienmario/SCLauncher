using System;

namespace SCLauncher.model.serverinstall;

public class ServerInstallMessage
{
	public ServerInstallMessage(string text)
	{
		Text = text;
		Time = DateTime.Now;
	}

	public string Text { get; }
	
	public DateTime Time { get; }

	public override string ToString()
	{
		return Text;
	}
}