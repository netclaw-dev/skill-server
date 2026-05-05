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

            var verifyTasks = skill.Resources.Select(resource =>
                VerifyResourceAsync(client, skill.Name, version, resource));
            var results = await Task.WhenAll(verifyTasks);

            foreach (var (resource, matched) in results)
            {
                if (matched)
                    ConsoleOutput.WriteSuccess($"  {resource}    match");
                else
                {
                    ConsoleOutput.WriteError($"  {resource}    MISMATCH");
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
            return ConsoleOutput.HandleHttpError(ex);
        }
    }

    private static async Task<(string RelativePath, bool Matched)> VerifyResourceAsync(
        SkillServerClient client, string skillName, string version, ScannedResource resource)
    {
        try
        {
            var serverTask = client.GetSkillFileAsStringAsync(
                skillName, version, resource.RelativePath);
            var localTask = File.ReadAllTextAsync(resource.AbsolutePath);
            await Task.WhenAll(serverTask, localTask);

            return (resource.RelativePath, localTask.Result == serverTask.Result);
        }
        catch (HttpRequestException)
        {
            return (resource.RelativePath, false);
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
