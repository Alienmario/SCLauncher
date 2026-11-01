using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SCLauncher.Shared.Model.ServerBrowser;

namespace SCLauncher.Shared.Service;

public interface IServerBrowserService
{
	IAsyncEnumerable<Server> GetServers(CancellationToken ct = default);

	Task<Server?> QueryServer(string address, bool players = false, bool rules = false,
		CancellationToken ct = default);
}
