using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia;

namespace LinkSentry
{
    internal class Program
    {
        private static readonly string DiagLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LinkSentry", "crash.log");

        [STAThread]
        public static void Main(string[] args)
        {
            // Global exception handlers to catch and log ALL unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                LogCrash($"AppDomain.UnhandledException: {e.ExceptionObject}");
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                LogCrash($"TaskScheduler.UnobservedTaskException: {e.Exception}");
                e.SetObserved(); // Prevent crash from unobserved task exceptions
            };

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                LogCrash($"Main caught: {ex}");
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        private static void LogCrash(string message)
        {
            try
            {
                var dir = Path.GetDirectoryName(DiagLogPath);
                if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(DiagLogPath,
                    $"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n",
                    Encoding.UTF8);
            }
            catch { /* cannot fail here */ }
        }
    }
}
