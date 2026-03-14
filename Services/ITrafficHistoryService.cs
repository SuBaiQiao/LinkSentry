using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LinkSentry.Models;

namespace LinkSentry.Services;

/// <summary>
/// Service for recording and querying historical network traffic data.
/// </summary>
public interface ITrafficHistoryService
{
    /// <summary>
    /// Records current traffic speeds for an interface.
    /// </summary>
    Task RecordAsync(string interfaceName, long speedSent, long speedReceived, long totalSent, long totalReceived);

    /// <summary>
    /// Retrieves aggregated heat map data points.
    /// </summary>
    Task<List<HeatmapDataPoint>> GetHeatmapDataAsync(string interfaceName, DateTime startTime, DateTime endTime, string granularity);

    /// <summary>
    /// Retrieves daily traffic totals for a date range.
    /// </summary>
    Task<List<HeatmapDataPoint>> GetDailyTrafficAsync(string interfaceName, DateTime startTime, DateTime endTime);

    /// <summary>
    /// Deletes records older than the specified days.
    /// </summary>
    Task CleanupOldDataAsync(int keepDays);
}
