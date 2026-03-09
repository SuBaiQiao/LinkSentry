using System.Collections.Generic;
using System.Threading.Tasks;
using LinkSentry.Models;

namespace LinkSentry.Services;

public interface IFirewallService
{
    /// <summary>
    /// Get the firewall status for all three profiles (Domain, Private, Public).
    /// </summary>
    Task<List<FirewallProfileInfo>> GetFirewallStatusAsync();
}
