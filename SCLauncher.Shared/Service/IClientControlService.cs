using System;
using System.Collections.Generic;
using SCLauncher.Shared.Model.Exceptions;

namespace SCLauncher.Shared.Service;

public interface IClientControlService
{
	/// <summary>
	/// Starts the game client or hijacks running instance and connects to the specified server address.
	/// </summary>
	/// <param name="address">The server address to connect to, usually in the format "ip:port"</param>
	/// <param name="password"></param>
	/// <returns>True if the game client was started successfully, false otherwise</returns>
	/// <exception cref="InvalidGamePathException">Thrown when the game executable cannot be found at configured path</exception>
	/// <exception cref="PlatformNotSupportedException">Thrown when current platform is not supported for active profile</exception>
	bool ConnectToServer(string address, string? password = null);

	/// <param name="args">Launch parameter arguments, null means use the client config.</param>
	/// <exception cref="InvalidGamePathException">Thrown when the game executable cannot be found at configured path</exception>
	/// <exception cref="PlatformNotSupportedException">Thrown when current platform is not supported for active profile</exception>
	bool RunClient(IEnumerable<string>? args = null);
}
