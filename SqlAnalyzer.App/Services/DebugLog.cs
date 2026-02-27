using System.IO;
using System.Text;
using System.Diagnostics;

namespace SqlAnalyzer.App.Services;

internal static class DebugLog
{
    private static readonly object Sync = new();
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "diagram_debug.log");

    [Conditional("DEBUG")]
    public static void Write(string message)
    {
        try
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
            lock (Sync)
            {
                File.AppendAllText(LogPath, line, Encoding.UTF8);
            }
        }
        catch
        {
            // Debug log must never break app behavior.
        }
    }
}
