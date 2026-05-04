// -----------------------------------------------------------------------
// <copyright file="VerifyCommand.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using Netclaw.SkillClient;
using Netclaw.SkillServer.Cli.Output;
using Netclaw.SkillServer.Cli.Publishing;

namespace Netclaw.SkillServer.Cli.Commands;

internal static class VerifyCommand
{
    public static async Task<int> ExecuteAsync(ParsedArgs args, SkillServerClient client)
    {
        if (args.Help || args.Positional.Count == 0)
        {
            PrintHelp();
            return args.Help ? 0 : 1;
        }

        var path = args.Positional[0];
        var skill = SkillDirectoryScanner.ScanDirectory(path);

        if (skill is null)
        {
            ConsoleOutput.WriteError($"Error: No valid SKILL.md found in '{path}'.");
            return 1;
        }

        var version = args.VersionOverride ?? skill.Version;
        ConsoleOutput.WriteInfo($"Verifying {skill.Name}@{version}...");

        try
        {
            var serverVersion = await client.GetVersionAsync(skill.Name, version);
            if (serverVersion is null)
            {
                ConsoleOutput.WriteError($"Error: {skill.Name}@{version} not found on server.");
                return 1;
            }

            var allMatch = true;

            // Verify SKILL.md
            var localDigest = await ComputeFileDigestAsync(skill.SkillMdPath);
            var serverDigest = serverVersion.Sha256;
            var match = string.Equals(localDigest, serverDigest, StringComparison.OrdinalIgnoreCase);
            if (match)
                ConsoleOutput.WriteSuccess("  SKILL.md    match");
            else
            {
                ConsoleOutput.WriteError($"  SKILL.md    MISMATCH (local: {localDigest[..15]}... server: {serverDigest[..15]}...)");
                allMatch = false;
            }

            // Verify resources by downloading and comparing
            foreach (var resource in skill.Resources)
            {
                try
                {
                    var serverContent = await client.GetSkillFileAsStringAsync(
                        skill.Name, version, resource.RelativePath);
                    var localContent = await File.ReadAllTextAsync(resource.AbsolutePath);

                    if (localContent == serverContent)
                        ConsoleOutput.WriteSuccess($"  {resource.RelativePath}    match");
                    else
                    {
                        ConsoleOutput.WriteError($"  {resource.RelativePath}    MISMATCH");
                        allMatch = false;
                    }
                }
                catch (HttpRequestException)
                {
                    ConsoleOutput.WriteError($"  {resource.RelativePath}    NOT FOUND on server");
                    allMatch = false;
                }
            }

            if (allMatch)
            {
                ConsoleOutput.WriteSuccess("All files verified");
                return 0;
            }

            ConsoleOutput.WriteError("Verification failed");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            ConsoleOutput.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<string> ComputeFileDigestAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream);
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: skillserver verify <path> [options]");
        Console.WriteLine();
        Console.WriteLine("Verify that a local skill directory matches the published version.");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <path>                Path to skill directory containing SKILL.md");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --version <version>   Version to verify against (default: from frontmatter)");
    }
}
