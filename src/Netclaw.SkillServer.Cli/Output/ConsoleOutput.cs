// -----------------------------------------------------------------------
// <copyright file="ConsoleOutput.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

namespace Netclaw.SkillServer.Cli.Output;

internal static class ConsoleOutput
{
    private static readonly bool IsTty = !Console.IsOutputRedirected;

    public static void WriteSuccess(string message)
    {
        if (IsTty) Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        if (IsTty) Console.ResetColor();
    }

    public static void WriteError(string message)
    {
        if (IsTty) Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        if (IsTty) Console.ResetColor();
    }

    public static void WriteWarning(string message)
    {
        if (IsTty) Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Error.WriteLine(message);
        if (IsTty) Console.ResetColor();
    }

    public static void WriteInfo(string message)
    {
        Console.WriteLine(message);
    }

    public static void WriteDim(string message)
    {
        if (IsTty) Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(message);
        if (IsTty) Console.ResetColor();
    }

    public static void WriteTable(string[] headers, IReadOnlyList<string[]> rows)
    {
        var widths = new int[headers.Length];
        for (var i = 0; i < headers.Length; i++)
            widths[i] = headers[i].Length;

        foreach (var row in rows)
        {
            for (var i = 0; i < row.Length && i < widths.Length; i++)
            {
                if (row[i].Length > widths[i])
                    widths[i] = row[i].Length;
            }
        }

        var header = string.Join("  ", headers.Select((h, i) => h.PadRight(widths[i])));
        Console.WriteLine(header);

        foreach (var row in rows)
        {
            var line = string.Join("  ", row.Select((v, i) => i < widths.Length ? v.PadRight(widths[i]) : v));
            Console.WriteLine(line);
        }
    }

    public static int HandleHttpError(HttpRequestException ex)
    {
        WriteError($"Error: {ex.Message}");
        return 1;
    }

    public static string MaskApiKey(string key)
    {
        if (key.Length <= 10)
            return new string('*', key.Length);
        return key[..6] + "..." + key[^4..];
    }
}
