// -----------------------------------------------------------------------
// <copyright file="SkillDirectoryScanner.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Netclaw.SkillServer.Cli.Publishing;

internal sealed record ScannedSkill(
    string DirectoryPath,
    string Name,
    string Version,
    string? Category,
    string? Description,
    string SkillMdPath,
    IReadOnlyList<ScannedResource> Resources);

internal readonly record struct ScannedResource(string RelativePath, string AbsolutePath, int? UnixMode);

internal static partial class SkillDirectoryScanner
{
    private const UnixFileMode PermissionMask = UnixFileMode.UserRead
                                                   | UnixFileMode.UserWrite
                                                   | UnixFileMode.UserExecute
                                                   | UnixFileMode.GroupRead
                                                   | UnixFileMode.GroupWrite
                                                   | UnixFileMode.GroupExecute
                                                   | UnixFileMode.OtherRead
                                                   | UnixFileMode.OtherWrite
                                                   | UnixFileMode.OtherExecute;

    [GeneratedRegex(@"^---\s*\n(.*?)\n---", RegexOptions.Singleline)]
    private static partial Regex FrontmatterRegex();

    public static ScannedSkill? ScanDirectory(string directoryPath)
    {
        var dir = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(dir))
            return null;

        var skillMdPath = Path.Combine(dir, "SKILL.md");
        if (!File.Exists(skillMdPath))
            return null;

        var content = File.ReadAllText(skillMdPath);
        var frontmatter = ParseFrontmatter(content);
        if (frontmatter is null)
            return null;

        var name = frontmatter.GetValueOrDefault("name");
        var version = frontmatter.GetValueOrDefault("version");
        var category = frontmatter.GetValueOrDefault("category");
        var description = frontmatter.GetValueOrDefault("description");

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version))
            return null;

        var resources = ScanResources(dir);

        return new ScannedSkill(dir, name, version, category, description, skillMdPath, resources);
    }

    public static IReadOnlyList<ScannedSkill> ScanAll(string parentDirectory)
    {
        var dir = Path.GetFullPath(parentDirectory);
        if (!Directory.Exists(dir))
            return [];

        var results = new List<ScannedSkill>();
        foreach (var subDir in Directory.EnumerateDirectories(dir).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var scanned = ScanDirectory(subDir);
            if (scanned is not null)
                results.Add(scanned);
        }

        return results;
    }

    private static IReadOnlyList<ScannedResource> ScanResources(string skillDirectory)
    {
        var resources = new List<ScannedResource>();

        foreach (var subDir in Directory.EnumerateDirectories(skillDirectory))
        {
            RejectReparsePoint(subDir);

            foreach (var file in EnumerateResourceFiles(subDir))
            {
                var relativePath = Path.GetRelativePath(skillDirectory, file)
                    .Replace('\\', '/');
                resources.Add(new ScannedResource(relativePath, file, GetUnixMode(file)));
            }
        }

        return resources;
    }

    private static IEnumerable<string> EnumerateResourceFiles(string directory)
    {
        foreach (var childDirectory in Directory.EnumerateDirectories(directory))
        {
            RejectReparsePoint(childDirectory);

            foreach (var file in EnumerateResourceFiles(childDirectory))
                yield return file;
        }

        foreach (var file in Directory.EnumerateFiles(directory))
        {
            RejectReparsePoint(file);
            yield return file;
        }
    }

    private static void RejectReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException($"Skill resources cannot include symbolic links or reparse points: {path}");
    }

    private static int? GetUnixMode(string file)
    {
        if (OperatingSystem.IsWindows())
            return null;

        return (int)(File.GetUnixFileMode(file) & PermissionMask);
    }

    internal static Dictionary<string, string>? ParseFrontmatter(string content)
    {
        var match = FrontmatterRegex().Match(content);
        if (!match.Success)
            return null;

        var yaml = match.Groups[1].Value;
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in yaml.Split('\n', StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
                continue;

            var key = line[..colonIndex].Trim();
            var value = line[(colonIndex + 1)..].Trim();

            // Strip surrounding quotes
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') ||
                 (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            result[key] = value;
        }

        return result.Count > 0 ? result : null;
    }
}
