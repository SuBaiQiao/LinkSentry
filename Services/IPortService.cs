using System.Collections.Generic;
using System.Threading.Tasks;
using LinkSentry.Models;

namespace LinkSentry.Services;

public interface IPortService
{
    /// <summary>
    /// Get all active TCP and UDP connections/listeners with process information.
    /// This is a potentially expensive operation and must be called off the UI thread.
    /// </summary>
    Task<List<PortConnectionInfo>> GetActiveConnectionsAsync();
}
