using System.Collections.Generic;
using System.Threading.Tasks;
using LinkSentry.Models;

namespace LinkSentry.Services;

public interface INetworkService
{
    Task<List<NetworkInterfaceModel>> GetAllInterfacesAsync();
    Task UpdateTrafficStatisticsAsync(IEnumerable<NetworkInterfaceModel> models);
    Task<bool> EnableInterfaceAsync(string adapterName);
    Task<bool> DisableInterfaceAsync(string adapterName);
    Task RefreshDhcpAsync(string adapterName);
    Task FlushDnsAsync();
}
