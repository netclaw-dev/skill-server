// -----------------------------------------------------------------------
// <copyright file="DownloadSubAgentCommand.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Output;
using Netclaw.SkillServer.Cli.Publishing;

namespace Netclaw.SkillServer.Cli.Commands;

internal static class DownloadSubAgentCommand
{
    public static async Task<int> ExecuteAsync(ParsedArgs args, SkillServerClient client)
    {
        if (args.Help || args.Positional.Count < 3)
        {
            PrintHelp();
            return args.Help ? 0 : 1;
        }

        var name = args.Positional[0];
        var version = args.Positional[1];
        var destinationPath = Path.GetFullPath(args.Positional[2]);

        if (!SubAgentFileScanner.IsValidName(name))
        {
            ConsoleOutput.WriteError("Error: Invalid sub-agent name.");
            return 1;
        }

        if (!SubAgentFileScanner.IsValidVersion(version))
        {
            ConsoleOutput.WriteError("Error: Invalid sub-agent version.");
            return 1;
        }

        if (File.Exists(destinationPath) && !args.Force)
        {
            ConsoleOutput.WriteError($"Error: Destination '{destinationPath}' already exists. Use --force to overwrite.");
            return 1;
        }

        if (args.DryRun)
        {
            ConsoleOutput.WriteWarning(
                $"Dry run: would download sub-agent {name}@{version} to {destinationPath}");
            return 0;
        }

        try
        {
            var detail = await client.GetNativeSubAgentVersionAsync(name, version);
            if (detail is null)
            {
                ConsoleOutput.WriteError($"Error: Sub-agent {name}@{version} not found.");
                return 1;
            }

            if (!detail.Type.Equals(SubAgentArtifactTypes.AgentMd, StringComparison.OrdinalIgnoreCase))
            {
                ConsoleOutput.WriteError($"Error: Unsupported sub-agent artifact type '{detail.Type}'.");
                return 1;
            }

            var parentDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(parentDirectory))
                Directory.CreateDirectory(parentDirectory);

            var tempPath = $"{destinationPath}.tmp-{Guid.NewGuid():N}";
            try
            {
                await using (var destination = File.Create(tempPath))
                {
                    if (args.Verbose)
                        ConsoleOutput.WriteDim($"  Downloading {detail.Url}");

                    var download = await client.DownloadNativeSubAgentArtifactAsync(detail, destination);
                    if (args.Verbose)
                        ConsoleOutput.WriteDim($"  Verified {download.Digest} ({FormatSize(download.SizeBytes)})");
                }

                File.Move(tempPath, destinationPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }

            ConsoleOutput.WriteSuccess($"Downloaded sub-agent {name}@{version} to {destinationPath}");
            return 0;
        }
        catch (HttpRequestException ex)
        {
            return ConsoleOutput.HandleHttpError(ex);
        }
        catch (InvalidDataException ex)
        {
            ConsoleOutput.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: skillserver download-subagent <name> <version> <path> [options]");
        Console.WriteLine();
        Console.WriteLine("Download a sub-agent artifact through the native manifest and verify its digest.");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <name>      Sub-agent name");
        Console.WriteLine("  <version>   Sub-agent version");
        Console.WriteLine("  <path>      Caller-controlled destination path");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --force, -f   Overwrite the destination path if it exists");
        Console.WriteLine("  --dry-run     Show what would be downloaded without writing a file");
        Console.WriteLine("  --verbose, -v Show download URL and verified digest");
    }
}
