using System;

namespace LinkSentry.Models;

/// <summary>
/// Aggregated traffic data for a specific time slot, used for heatmap visualization.
/// </summary>
public record HeatmapDataPoint
{
    /// <summary>The start time of the aggregation period</summary>
    public DateTime Time { get; init; }

    /// <summary>Average upload speed (B/s) in this period</summary>
    public double AvgUpload { get; init; }

    /// <summary>Average download speed (B/s) in this period</summary>
    public double AvgDownload { get; init; }

    /// <summary>Maximum upload speed (B/s) in this period</summary>
    public double MaxUpload { get; init; }

    /// <summary>Maximum download speed (B/s) in this period</summary>
    public double MaxDownload { get; init; }

    /// <summary>
    /// Calculated intensity level for UI representation (e.g., 0-5).
    /// Typically based on AvgDownload or MaxDownload relative to a threshold.
    /// </summary>
    public int IntensityLevel { get; init; }

    // Helpers for XAML Tooltip binding
    public string FormattedTotalUpload => FormatBytes(AvgUpload);
    public string FormattedTotalDownload => FormatBytes(AvgDownload);

    private static string FormatBytes(double bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int unitIndex = 0;
        while (bytes >= 1024 && unitIndex < units.Length - 1)
        {
            bytes /= 1024;
            unitIndex++;
        }
        return $"{bytes:F2} {units[unitIndex]}";
    }
}
