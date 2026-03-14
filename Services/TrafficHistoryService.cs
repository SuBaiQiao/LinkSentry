using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using LinkSentry.Models;

namespace LinkSentry.Services;

public class TrafficHistoryService : ITrafficHistoryService
{
    private readonly SqliteDbFactory _dbFactory;
    private readonly IDiagnosticLogger _diag;

    public TrafficHistoryService(SqliteDbFactory dbFactory, IDiagnosticLogger diag)
    {
        _dbFactory = dbFactory;
        _diag = diag;
    }

    public async Task RecordAsync(string interfaceName, long speedSent, long speedReceived, long totalSent, long totalReceived)
    {
        try
        {
            using var connection = _dbFactory.CreateConnection();
            using var command = connection.CreateCommand();
            
            command.CommandText = @"
                INSERT INTO TrafficHistory (Timestamp, InterfaceName, SpeedSent, SpeedReceived, TotalSent, TotalReceived)
                VALUES ($ts, $name, $sent, $recv, $tSent, $tRecv);
            ";
            
            command.Parameters.AddWithValue("$ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            command.Parameters.AddWithValue("$name", interfaceName);
            command.Parameters.AddWithValue("$sent", (double)speedSent);
            command.Parameters.AddWithValue("$recv", (double)speedReceived);
            command.Parameters.AddWithValue("$tSent", totalSent);
            command.Parameters.AddWithValue("$tRecv", totalReceived);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _diag.Log($"TrafficHistory: Failed to record data - {ex.Message}");
        }
    }

    public async Task<List<HeatmapDataPoint>> GetHeatmapDataAsync(string interfaceName, DateTime startTime, DateTime endTime, string granularity)
    {
        var results = new List<HeatmapDataPoint>();
        try
        {
            using var connection = _dbFactory.CreateConnection();
            using var command = connection.CreateCommand();

            long startTs = new DateTimeOffset(startTime).ToUnixTimeSeconds();
            long endTs = new DateTimeOffset(endTime).ToUnixTimeSeconds();

            // SQLite math for grouping
            // Minute: timestamp / 60 * 60
            // Hour: timestamp / 3600 * 3600
            // Day: timestamp / 86400 * 86400
            int seconds = granularity.ToLower() switch
            {
                "minute" => 60,
                "hour" => 3600,
                "day" => 86400,
                _ => 3600
            };

            command.CommandText = $@"
                SELECT 
                    (Timestamp / {seconds}) * {seconds} as GroupedTime,
                    AVG(SpeedSent) as AvgSent,
                    AVG(SpeedReceived) as AvgRecv,
                    MAX(SpeedSent) as MaxSent,
                    MAX(SpeedReceived) as MaxRecv
                FROM TrafficHistory
                WHERE InterfaceName = $name AND Timestamp BETWEEN $start AND $end
                GROUP BY GroupedTime
                ORDER BY GroupedTime ASC;
            ";

            command.Parameters.AddWithValue("$name", interfaceName);
            command.Parameters.AddWithValue("$start", startTs);
            command.Parameters.AddWithValue("$end", endTs);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var ts = reader.GetInt64(0);
                results.Add(new HeatmapDataPoint
                {
                    Time = DateTimeOffset.FromUnixTimeSeconds(ts).DateTime.ToLocalTime(),
                    AvgUpload = reader.GetDouble(1),
                    AvgDownload = reader.GetDouble(2),
                    MaxUpload = reader.GetDouble(3),
                    MaxDownload = reader.GetDouble(4),
                    IntensityLevel = CalculateIntensity(reader.GetDouble(2))
                });
            }
        }
        catch (Exception ex)
        {
            _diag.Log($"TrafficHistory: Query failed - {ex.Message}");
        }
        return results;
    }

    public async Task<List<HeatmapDataPoint>> GetDailyTrafficAsync(string interfaceName, DateTime startTime, DateTime endTime)
    {
        var results = new List<HeatmapDataPoint>();
        try
        {
            using var connection = _dbFactory.CreateConnection();
            using var command = connection.CreateCommand();

            long startTs = new DateTimeOffset(startTime).ToUnixTimeSeconds();
            long endTs = new DateTimeOffset(endTime).ToUnixTimeSeconds();

            // For daily totals, we look at the difference between max and min total bytes for that day
            // Or sum up the speeds * 2 seconds if total bytes aren't reliable.
            // Using max(Total) - min(Total) is better if available.
            command.CommandText = @"
                SELECT 
                    (Timestamp / 86400) * 86400 as DayTs,
                    (MAX(TotalSent) - MIN(TotalSent)) as DaySent,
                    (MAX(TotalReceived) - MIN(TotalReceived)) as DayRecv
                FROM TrafficHistory
                WHERE InterfaceName = $name AND Timestamp BETWEEN $start AND $end
                GROUP BY DayTs
                ORDER BY DayTs ASC;
            ";

            command.Parameters.AddWithValue("$name", interfaceName);
            command.Parameters.AddWithValue("$start", startTs);
            command.Parameters.AddWithValue("$end", endTs);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var ts = reader.GetInt64(0);
                var sent = reader.IsDBNull(1) ? 0 : reader.GetInt64(1);
                var recv = reader.IsDBNull(2) ? 0 : reader.GetInt64(2);

                results.Add(new HeatmapDataPoint
                {
                    Time = DateTimeOffset.FromUnixTimeSeconds(ts).DateTime.ToLocalTime(),
                    AvgUpload = sent, // Total for the day
                    AvgDownload = recv, // Total for the day
                    MaxUpload = sent, // Reusing for total
                    MaxDownload = recv, // Reusing for total
                    IntensityLevel = CalculateIntensityDaily(recv)
                });
            }
        }
        catch (Exception ex)
        {
            _diag.Log($"TrafficHistory: Daily query failed - {ex.Message}");
        }
        return results;
    }

    private static int CalculateIntensityDaily(double totalRecv)
    {
        // Daily thresholds (Bytes)
        // <= 0 (0), >0-1GB (1), 1-5GB (2), 5-10GB (3), 10-50GB (4), > 50GB (5)
        double gb = 1024.0 * 1024 * 1024;
        if (totalRecv <= 0) return 0;
        if (totalRecv < 1 * gb) return 1;
        if (totalRecv < 5 * gb) return 2;
        if (totalRecv < 10 * gb) return 3;
        if (totalRecv < 50 * gb) return 4;
        return 5;
    }

    public async Task CleanupOldDataAsync(int keepDays)
    {
        try
        {
            using var connection = _dbFactory.CreateConnection();
            using var command = connection.CreateCommand();

            long cutoff = DateTimeOffset.UtcNow.AddDays(-keepDays).ToUnixTimeSeconds();
            command.CommandText = "DELETE FROM TrafficHistory WHERE Timestamp < $cutoff;";
            command.Parameters.AddWithValue("$cutoff", cutoff);

            int deleted = await command.ExecuteNonQueryAsync();
            if (deleted > 0) _diag.Log($"TrafficHistory: Cleaned up {deleted} old records.");
        }
        catch (Exception ex)
        {
            _diag.Log($"TrafficHistory: Cleanup Error - {ex.Message}");
        }
    }

    private static int CalculateIntensity(double speedRecv)
    {
        // Simple 0-5 mapping based on download speed
        // 0: < 1KB/s, 1: < 100KB/s, 2: < 1MB/s, 3: < 10MB/s, 4: < 50MB/s, 5: > 50MB/s
        if (speedRecv < 1024) return 0;
        if (speedRecv < 1024 * 100) return 1;
        if (speedRecv < 1024 * 1024) return 2;
        if (speedRecv < 1024 * 1024 * 10) return 3;
        if (speedRecv < 1024 * 1024 * 50) return 4;
        return 5;
    }
}
