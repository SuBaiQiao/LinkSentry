using System;
using System.IO;
using System.Text;

namespace LinkSentry.Services;

public interface IDiagnosticLogger
{
    void Log(string message);
    string GetLogPath();
}

public class DiagnosticLogger : IDiagnosticLogger
{
    private readonly string _logPath;
    private readonly object _lock = new();

    public DiagnosticLogger()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "LinkSentry");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        _logPath = Path.Combine(dir, "diagnostics.log");
        
        // Clear old log on start
        try { File.WriteAllText(_logPath, $"--- Diagnostic Log Started at {DateTime.Now} ---\n", Encoding.UTF8); } catch { }
    }

    public void Log(string message)
    {
        var timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";
        lock (_lock)
        {
            try { File.AppendAllText(_logPath, timestamped, Encoding.UTF8); } catch { }
        }
        System.Diagnostics.Debug.WriteLine(timestamped);
    }

    public string GetLogPath() => _logPath;
}
