using System.Diagnostics;

namespace ToDoapp.Services;

internal static class StartupDiagnostics
{
    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

    internal static void Mark(string stage)
    {
        Trace.WriteLine($"[Startup] {Stopwatch.Elapsed.TotalMilliseconds,8:F1} ms  {stage}");
    }
}
