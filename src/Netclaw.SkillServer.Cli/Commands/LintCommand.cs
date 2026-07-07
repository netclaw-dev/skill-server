// -----------------------------------------------------------------------
// <copyright file="LintCommand.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Netclaw.SkillServer.Cli.Output;
using Netclaw.SkillServer.Cli.Publishing;

namespace Netclaw.SkillServer.Cli.Commands;

/// <summary>
/// Result of validating a single skill directory.
/// </summary>
public sealed record SkillLintResult(
    IReadOnlyList<string> Issues,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Lint command: validates skill directories against the AgentSkills.io spec
/// and reports issues. Used in CI to catch problems before publishing.
/// </summary>
internal static class LintCommand
{
    public static async Task<int> ExecuteAsync(ParsedArgs args)
    {
        if (args.Positional.Count > 0 &&
            args.Positional[0].Equals("subagent", StringComparison.OrdinalIgnoreCase))
        {
            return ExecuteSubAgentLint(args);
        }

        if (args.Positional.Count > 0 &&
            args.Positional[0].Equals("subagents", StringComparison.OrdinalIgnoreCase))
        {
            return ExecuteSubAgentsLint(args);
        }

        if (args.Help || args.Positional.Count == 0)
        {
            PrintHelp();
            return args.Help ? 0 : 1;
        }

        var path = args.Positional[0];
        if (!Directory.Exists(path))
        {
            ConsoleOutput.WriteError($"Error: Directory '{path}' does not exist.");
            return 1;
        }

        var dir = Path.GetFullPath(path);
        var issues = new List<string>();
        var warnings = new List<string>();
        var validated = 0;

        // Scan each subdirectory for a skill
        var subDirs = Directory.EnumerateDirectories(dir)
            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var subDir in subDirs)
        {
            var skillName = Path.GetFileName(subDir);
            var skillMdPath = Path.Combine(subDir, "SKILL.md");

            if (!File.Exists(skillMdPath))
            {
                // Check if this looks like it should be a skill (has files but no SKILL.md)
                var hasMd = Directory.GetFiles(subDir, "*.md", SearchOption.TopDirectoryOnly)
                    .Any(f => Path.GetFileName(f).ToLowerInvariant() != "readme.md");
                var hasSubdirs = Directory.GetDirectories(subDir).Any();

                if (hasMd || hasSubdirs)
                {
                    warnings.Add($"{skillName}: No SKILL.md found — directory may be incomplete");
                }
                continue;
            }

            var content = File.ReadAllText(skillMdPath);
            var skillIssues = ValidateSkill(skillName, skillMdPath, content);
            issues.AddRange(skillIssues.Issues);
            warnings.AddRange(skillIssues.Warnings);

            if (skillIssues.Issues.Count == 0)
                validated++;
        }

        // Print results
        ConsoleOutput.WriteInfo($"Linting '{path}'...");
        ConsoleOutput.WriteInfo($"Found {subDirs.Count} potential skill directory(ies).");
        Console.WriteLine();

        bool hasErrors = false;

        foreach (var issue in issues)
        {
            ConsoleOutput.WriteError($"✗ {issue}");
            hasErrors = true;
        }

        foreach (var warning in warnings)
        {
            ConsoleOutput.WriteWarning($"⚠ {warning}");
        }

        Console.WriteLine();

        if (!hasErrors)
        {
            ConsoleOutput.WriteSuccess($"All skills valid ({validated} passed, {warnings.Count} warning(s))");
        }
        else
        {
            ConsoleOutput.WriteError($"{issues.Count} error(s), {warnings.Count} warning(s) — lint failed");
        }

        return hasErrors ? 1 : 0;
    }

    private static int ExecuteSubAgentLint(ParsedArgs args)
    {
        if (args.Help || args.Positional.Count < 2)
        {
            PrintSubAgentHelp();
            return args.Help ? 0 : 1;
        }

        var path = args.Positional[1];
        var result = SubAgentFileScanner.ValidateFile(path);

        ConsoleOutput.WriteInfo($"Linting sub-agent '{path}'...");
        Console.WriteLine();

        foreach (var issue in result.Issues)
            ConsoleOutput.WriteError($"✗ {issue}");

        foreach (var warning in result.Warnings)
            ConsoleOutput.WriteWarning($"⚠ {warning}");

        Console.WriteLine();

        if (result.Issues.Count == 0)
        {
            ConsoleOutput.WriteSuccess($"Sub-agent valid ({result.Warnings.Count} warning(s))");
            return 0;
        }

        ConsoleOutput.WriteError($"{result.Issues.Count} error(s), {result.Warnings.Count} warning(s) — lint failed");
        return 1;
    }

    private static int ExecuteSubAgentsLint(ParsedArgs args)
    {
        if (args.Help || args.Positional.Count < 2)
        {
            PrintSubAgentsHelp();
            return args.Help ? 0 : 1;
        }

        var path = args.Positional[1];
        var results = SubAgentFileScanner.ValidateDirectory(path);
        var issues = results.SelectMany(r => r.Issues).ToList();
        var warnings = results.SelectMany(r => r.Warnings).ToList();
        var validSubAgents = results.Select(r => r.SubAgent).OfType<ScannedSubAgent>().ToList();

        foreach (var group in validSubAgents.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            issues.Add($"Duplicate sub-agent name '{group.Key}' found in input set: {string.Join(", ", group.Select(s => s.FilePath))}");
        }

        ConsoleOutput.WriteInfo($"Linting sub-agents in '{path}'...");
        ConsoleOutput.WriteInfo($"Found {results.Count} markdown file(s).");
        Console.WriteLine();

        foreach (var issue in issues)
            ConsoleOutput.WriteError($"✗ {issue}");

        foreach (var warning in warnings)
            ConsoleOutput.WriteWarning($"⚠ {warning}");

        Console.WriteLine();

        if (issues.Count == 0)
        {
            ConsoleOutput.WriteSuccess($"All sub-agents valid ({validSubAgents.Count} passed, {warnings.Count} warning(s))");
            return 0;
        }

        ConsoleOutput.WriteError($"{issues.Count} error(s), {warnings.Count} warning(s) - lint failed");
        return 1;
    }

    /// <summary>
    /// Validate a single skill's SKILL.md content against the AgentSkills.io spec.
    /// </summary>
    internal static SkillLintResult ValidateSkill(
        string skillName, string skillMdPath, string content)
    {
        var issues = new List<string>();
        var warnings = new List<string>();

        // 1. Check frontmatter exists
        var frontmatter = SkillDirectoryScanner.ParseFrontmatter(content);
        if (frontmatter is null)
        {
            issues.Add($"{skillName}: No YAML frontmatter found in SKILL.md");
            return new SkillLintResult(issues, warnings);
        }

        // 2. Required: name
        var name = frontmatter.GetValueOrDefault("name");
        if (string.IsNullOrWhiteSpace(name))
        {
            issues.Add($"{skillName}: Missing required 'name' field in frontmatter");
        }
        else
        {
            // Check name matches directory name
            var dirName = Path.GetFileName(Path.GetDirectoryName(skillMdPath)!);
            if (!string.Equals(name, dirName, StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"{skillName}: Frontmatter 'name' ({name}) does not match directory name ({dirName})");
            }

            // Validate name format (lowercase alphanumeric and hyphens)
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-z0-9][a-z0-9\-]*$"))
            {
                issues.Add($"{skillName}: Invalid 'name' format '{name}' — must be lowercase alphanumeric with hyphens");
            }
        }

        // 3. Required: version
        var version = frontmatter.GetValueOrDefault("version");
        if (string.IsNullOrWhiteSpace(version))
        {
            issues.Add($"{skillName}: Missing required 'version' field in frontmatter");
        }
        else
        {
            // Validate semantic version format (basic check)
            if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+"))
            {
                warnings.Add($"{skillName}: 'version' '{version}' does not follow semantic versioning (x.y.z)");
            }
        }

        // 4. Recommended: description
        var description = frontmatter.GetValueOrDefault("description");
        if (string.IsNullOrWhiteSpace(description))
        {
            warnings.Add($"{skillName}: Missing recommended 'description' field in frontmatter");
        }

        // 5. Check body has content after frontmatter (find the CLOSING ---, not the opening)
        var firstDash = content.IndexOf("---\n", StringComparison.Ordinal);
        if (firstDash >= 0)
        {
            var secondDash = content.IndexOf("---\n", firstDash + 4, StringComparison.Ordinal);
            if (secondDash >= 0)
            {
                var body = content[(secondDash + 4)..].Trim();
                if (string.IsNullOrEmpty(body))
                {
                    warnings.Add($"{skillName}: SKILL.md has no body content after frontmatter");
                }
            }
        }

        return new SkillLintResult(issues, warnings);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: skillserver lint <path>");
        Console.WriteLine();
        Console.WriteLine("Validate all skills in a directory against the AgentSkills.io specification.");
        Console.WriteLine("Checks for required fields, format issues, and common problems.");
        Console.WriteLine();
        Console.WriteLine("Returns exit code 1 if any errors are found (suitable for CI).");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <path>    Parent directory containing skill subdirectories");
        Console.WriteLine();
        Console.WriteLine("Validates:");
        Console.WriteLine("  - YAML frontmatter exists");
        Console.WriteLine("  - Required 'name' field present");
        Console.WriteLine("  - Required 'version' field present");
        Console.WriteLine("  - Name matches directory name");
        Console.WriteLine("  - Name format (lowercase alphanumeric with hyphens)");
        Console.WriteLine("  - Semantic version format");
        Console.WriteLine("  - Description present (warning if missing)");
    }

    private static void PrintSubAgentHelp()
    {
        Console.WriteLine("Usage: skillserver lint subagent <path>");
        Console.WriteLine();
        Console.WriteLine("Validate a NetClaw-compatible sub-agent markdown file.");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <path>    Path to agent.md or another .md sub-agent definition");
        Console.WriteLine();
        Console.WriteLine("Validates:");
        Console.WriteLine("  - YAML frontmatter exists");
        Console.WriteLine("  - Required 'name' and 'description' fields are present");
        Console.WriteLine("  - Name format matches SkillServer resource names");
        Console.WriteLine("  - Prompt body is not empty");
        Console.WriteLine("  - modelRole, visibility, timeoutSeconds, and prefillTimeoutSeconds values are valid");
    }

    private static void PrintSubAgentsHelp()
    {
        Console.WriteLine("Usage: skillserver lint subagents <path>");
        Console.WriteLine();
        Console.WriteLine("Validate all NetClaw-compatible sub-agent markdown files under a directory.");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <path>    Directory containing .md sub-agent definitions");
        Console.WriteLine();
        Console.WriteLine("Validates each file and reports duplicate sub-agent names in the input set.");
    }
}
