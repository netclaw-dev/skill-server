// -----------------------------------------------------------------------
// <copyright file="SubAgentFileScanner.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Netclaw.SkillServer.Cli.Publishing;

internal sealed record ScannedSubAgent(
    string FilePath,
    string Name,
    string? Version,
    string Description,
    string Body,
    string ModelRole,
    int TimeoutSeconds,
    int? PrefillTimeoutSeconds,
    string Visibility,
    bool EmitStructuredFindings);

internal sealed record SubAgentLintResult(
    IReadOnlyList<string> Issues,
    IReadOnlyList<string> Warnings,
    ScannedSubAgent? SubAgent);

internal static partial class SubAgentFileScanner
{
    private static readonly HashSet<string> KnownFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "version",
        "metadata.version",
        "description",
        "tools",
        "modelRole",
        "timeoutSeconds",
        "prefillTimeoutSeconds",
        "visibility",
        "emitStructuredFindings"
    };

    public static SubAgentLintResult ValidateFile(string filePath)
    {
        var path = Path.GetFullPath(filePath);
        var displayName = Path.GetFileName(path);

        if (!File.Exists(path))
        {
            return new SubAgentLintResult(
                [$"{displayName}: File does not exist"],
                [],
                null);
        }

        if (!path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return new SubAgentLintResult(
                [$"{displayName}: Sub-agent definition must be a markdown file"],
                [],
                null);
        }

        var content = File.ReadAllText(path);
        return ValidateContent(displayName, path, content);
    }

    internal static SubAgentLintResult ValidateContent(string displayName, string filePath, string content)
    {
        var issues = new List<string>();
        var warnings = new List<string>();
        var match = FrontmatterRegex().Match(content);

        if (!match.Success)
        {
            issues.Add($"{displayName}: Missing or invalid YAML frontmatter");
            return new SubAgentLintResult(issues, warnings, null);
        }

        var fields = ParseFrontmatter(match.Groups[1].Value, warnings, displayName);
        var body = match.Groups[2].Value.Trim();

        var name = fields.GetValueOrDefault("name");
        if (string.IsNullOrWhiteSpace(name))
        {
            issues.Add($"{displayName}: Missing required 'name' field in frontmatter");
        }
        else if (!ValidNameRegex().IsMatch(name) || name.Length > 64)
        {
            issues.Add($"{displayName}: Invalid 'name' format '{name}' - must be 1-64 lowercase alphanumeric characters and hyphens, with no leading, trailing, or consecutive hyphens");
        }

        var description = fields.GetValueOrDefault("description");
        if (string.IsNullOrWhiteSpace(description))
            issues.Add($"{displayName}: Missing required 'description' field in frontmatter");

        if (string.IsNullOrWhiteSpace(body))
            issues.Add($"{displayName}: Sub-agent prompt body must not be empty");

        var modelRole = NormalizeModelRole(fields.GetValueOrDefault("modelRole"), issues, displayName);
        var visibility = NormalizeVisibility(fields.GetValueOrDefault("visibility"), issues, displayName);
        var timeoutSeconds = ParseTimeout(fields.GetValueOrDefault("timeoutSeconds"), "timeoutSeconds", 60, 5, 600, issues, displayName);
        var prefillTimeoutSeconds = ParseOptionalTimeout(fields.GetValueOrDefault("prefillTimeoutSeconds"), "prefillTimeoutSeconds", 5, 3600, issues, displayName);
        var emitStructuredFindings = ParseBool(fields.GetValueOrDefault("emitStructuredFindings"), "emitStructuredFindings", false, issues, displayName);
        var version = NormalizeVersion(
            fields.GetValueOrDefault("version") ?? fields.GetValueOrDefault("metadata.version"),
            issues,
            displayName);

        if (issues.Count > 0)
            return new SubAgentLintResult(issues, warnings, null);

        return new SubAgentLintResult(
            issues,
            warnings,
            new ScannedSubAgent(
                Path.GetFullPath(filePath),
                name!,
                version,
                description!,
                body,
                modelRole,
                timeoutSeconds,
                prefillTimeoutSeconds,
                visibility,
                emitStructuredFindings));
    }

    public static IReadOnlyList<SubAgentLintResult> ValidateDirectory(string directoryPath)
    {
        var dir = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(dir))
        {
            return [new SubAgentLintResult([$"{Path.GetFileName(dir)}: Directory does not exist"], [], null)];
        }

        return Directory.EnumerateFiles(dir, "*.md", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Select(ValidateFile)
            .ToList();
    }

    private static Dictionary<string, string> ParseFrontmatter(string yaml, List<string> warnings, string displayName)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in yaml.Split('\n', StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(rawLine) || rawLine.StartsWith('#') || rawLine.StartsWith('-'))
                continue;

            var colonIndex = rawLine.IndexOf(':');
            if (colonIndex <= 0)
                continue;

            var key = rawLine[..colonIndex].Trim();
            var value = rawLine[(colonIndex + 1)..].Trim();
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') ||
                 (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            if (!KnownFields.Contains(key))
                warnings.Add($"{displayName}: Unknown frontmatter field '{key}' will be ignored by SkillServer");

            fields[key] = value;
        }

        return fields;
    }

    private static string NormalizeModelRole(string? value, List<string> issues, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("Compaction", StringComparison.OrdinalIgnoreCase))
            return "Compaction";

        if (value.Equals("Main", StringComparison.OrdinalIgnoreCase))
            return "Main";

        issues.Add($"{displayName}: Invalid modelRole. Must be 'Compaction' or 'Main'");
        return "";
    }

    private static string NormalizeVisibility(string? value, List<string> issues, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Equals("user-facing", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("UserFacing", StringComparison.OrdinalIgnoreCase))
        {
            return "user-facing";
        }

        if (value.Equals("internal", StringComparison.OrdinalIgnoreCase))
            return "internal";

        issues.Add($"{displayName}: Invalid visibility. Must be 'user-facing' or 'internal'");
        return "";
    }

    private static int ParseTimeout(
        string? value,
        string fieldName,
        int defaultValue,
        int min,
        int max,
        List<string> issues,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (!int.TryParse(value, out var parsed) || parsed < min || parsed > max)
        {
            issues.Add($"{displayName}: Invalid {fieldName}. Must be between {min} and {max}");
            return defaultValue;
        }

        return parsed;
    }

    private static int? ParseOptionalTimeout(
        string? value,
        string fieldName,
        int min,
        int max,
        List<string> issues,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!int.TryParse(value, out var parsed) || parsed < min || parsed > max)
        {
            issues.Add($"{displayName}: Invalid {fieldName}. Must be between {min} and {max}");
            return null;
        }

        return parsed;
    }

    private static bool ParseBool(
        string? value,
        string fieldName,
        bool defaultValue,
        List<string> issues,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (bool.TryParse(value, out var parsed))
            return parsed;

        issues.Add($"{displayName}: Invalid {fieldName}. Must be 'true' or 'false'");
        return defaultValue;
    }

    internal static bool IsValidVersion(string? version)
    {
        return !string.IsNullOrWhiteSpace(version) &&
               version.Length <= 64 &&
               version.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '+');
    }

    private static string? NormalizeVersion(string? value, List<string> issues, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (IsValidVersion(value))
            return value;

        issues.Add($"{displayName}: Invalid 'version' format '{value}' - must be 1-64 alphanumeric characters, dots, hyphens, or plus signs");
        return null;
    }

    internal static bool IsValidName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) &&
               name.Length <= 64 &&
               ValidNameRegex().IsMatch(name);
    }

    [GeneratedRegex(@"^---\s*\r?\n(.*?)\r?\n---\s*(?:\r?\n)?(.*)$", RegexOptions.Singleline)]
    private static partial Regex FrontmatterRegex();

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex ValidNameRegex();
}
