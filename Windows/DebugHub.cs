using System;
using System.Collections.Generic;

namespace GambaBank;

public static class DebugHub
{
    private static readonly object Sync = new();
    private static readonly List<string> Entries = new();
    private const int MaxEntries = 4000;

    public static void Add(string category, string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] [{category}] {message}";

        lock (Sync)
        {
            Entries.Add(line);
            if (Entries.Count > MaxEntries)
                Entries.RemoveAt(0);
        }

        try
        {
            if (string.Equals(category, "ERR", StringComparison.OrdinalIgnoreCase))
                Plugin.Log.Error(line);
            else if (string.Equals(category, "WARN", StringComparison.OrdinalIgnoreCase))
                Plugin.Log.Warning(line);
        }
        catch
        {
            // Ignore logger failures.
        }
    }

    public static IReadOnlyList<string> Snapshot()
    {
        lock (Sync)
            return Entries.ToArray();
    }

    public static string SnapshotText()
    {
        lock (Sync)
            return string.Join(Environment.NewLine, Entries);
    }

    public static void Clear()
    {
        lock (Sync)
            Entries.Clear();
    }
}
