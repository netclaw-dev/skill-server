// -----------------------------------------------------------------------
// <copyright file="LintCommandTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Netclaw.SkillServer.Cli.Commands;
using Xunit;

namespace Netclaw.SkillServer.Cli.Tests;

public sealed class LintCommandTests : IDisposable
{
    private readonly string _tempDir;

    public LintCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "skillserver-lint-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateSkillDirectory(string name, string skillMdContent)
    {
        var dir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), skillMdContent);
        return dir;
    }

    #region Valid skills

    [Fact]
    public void ValidateSkill_ValidSkill_NoIssuesOrWarnings()
    {
        var content = """
            ---
            name: my-skill
            version: 1.0.0
            description: A test skill
            ---
            # My Skill
            
            Content here.
            """;

        var result = LintCommand.ValidateSkill("my-skill", "skills/my-skill/SKILL.md", content);

        Assert.Empty(result.Issues);
        Assert.Empty(result.Warnings);
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("0.1.0")]
    [InlineData("2.13.7")]
    [InlineData("10.0.0-alpha")]
    public void ValidateSkill_ValidSemVer_NoWarning(string version)
    {
        var content = $"""
            ---
            name: my-skill
            version: {version}
            description: A test skill
            ---
            # My Skill
            """;

        var result = LintCommand.ValidateSkill("my-skill", "skills/my-skill/SKILL.md", content);

        Assert.DoesNotContain(result.Warnings, w => w.Contains("semantic versioning"));
    }

    [Theory]
    [InlineData("my-skill")]
    [InlineData("a")]
    [InlineData("my-skill-123")]
    [InlineData("123skill")]
    public void ValidateSkill_ValidNameFormat_NoIssue(string name)
    {
        var content = $"""
            ---
            name: {name}
            version: 1.0.0
            description: A test skill
            ---
            # My Skill
            """;

        var result = LintCommand.ValidateSkill(name, $"skills/{name}/SKILL.md", content);

        Assert.DoesNotContain(result.Issues, i => i.Contains("Invalid 'name' format"));
    }

    #endregion

    #region Missing required fields

    [Theory]
    [InlineData("""
        ---
        version: 1.0.0
        description: Missing name
        ---
        # Content
        """, "Missing required 'name'")]
    [InlineData("""
        ---
        name: no-version
        description: Missing version
        ---
        # Content
        """, "Missing required 'version'")]
    [InlineData("""
        ---
        description: Missing both
        ---
        # Content
        """, "Missing required 'name'")]
    public void ValidateSkill_MissingRequiredFields_ReportsIssues(string content, string expectedError)
    {
        var result = LintCommand.ValidateSkill("test-skill", "skills/test-skill/SKILL.md", content);

        Assert.NotEmpty(result.Issues);
        Assert.Contains(result.Issues, i => i.Contains(expectedError));
    }

    [Fact]
    public void ValidateSkill_MissingBothNameAndVersion_TwoIssues()
    {
        var content = """
            ---
            description: Missing both
            ---
            # Content
            """;

        var result = LintCommand.ValidateSkill("test-skill", "skills/test-skill/SKILL.md", content);

        Assert.Equal(2, result.Issues.Count);
        Assert.Contains(result.Issues, i => i.Contains("Missing required 'name'"));
        Assert.Contains(result.Issues, i => i.Contains("Missing required 'version'"));
    }

    #endregion

    #region Frontmatter

    [Fact]
    public void ValidateSkill_NoFrontmatter_ReportIssue()
    {
        var content = "# Just content, no frontmatter";

        var result = LintCommand.ValidateSkill("test-skill", "skills/test-skill/SKILL.md", content);

        Assert.Single(result.Issues);
        Assert.Contains("No YAML frontmatter", result.Issues[0]);
    }

    [Fact]
    public void ValidateSkill_EmptyBody_HasWarning()
    {
        // Must start with --- at position 0 for the frontmatter regex to match
        var content = "---\nname: test-skill\nversion: 1.0.0\ndescription: A test skill\n---\n";

        var result = LintCommand.ValidateSkill("test-skill", "skills/test-skill/SKILL.md", content);

        Assert.Empty(result.Issues);
        Assert.Contains(result.Warnings, w => w.Contains("no body content"));
    }

    #endregion

    #region Warnings

    [Theory]
    [InlineData("MySkill", "uppercase")]
    [InlineData("test-skill", "camelCase")]
    public void ValidateSkill_NameMismatch_Warning(string dirName, string frontmatterName)
    {
        var content = $"""
            ---
            name: {frontmatterName}
            version: 1.0.0
            description: A test skill
            ---
            # My Skill
            """;

        var result = LintCommand.ValidateSkill(dirName, $"skills/{dirName}/SKILL.md", content);

        Assert.Contains(result.Warnings, w => w.Contains("does not match directory name"));
    }

    [Fact]
    public void ValidateSkill_MissingDescription_Warning()
    {
        var content = """
            ---
            name: test-skill
            version: 1.0.0
            ---
            # My Skill
            """;

        var result = LintCommand.ValidateSkill("test-skill", "skills/test-skill/SKILL.md", content);

        Assert.Empty(result.Issues);
        Assert.Contains(result.Warnings, w => w.Contains("Missing recommended 'description'"));
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("v1.0.0")]
    [InlineData("latest")]
    public void ValidateSkill_NonStandardSemVer_Warning(string version)
    {
        var content = $"""
            ---
            name: test-skill
            version: {version}
            description: A test skill
            ---
            # My Skill
            """;

        var result = LintCommand.ValidateSkill("test-skill", "skills/test-skill/SKILL.md", content);

        Assert.Contains(result.Warnings, w => w.Contains("does not follow semantic versioning"));
    }

    #endregion

    #region Invalid name format

    [Theory]
    [InlineData("MySkill")]
    [InlineData("test_skill")]
    [InlineData("test skill")]
    [InlineData("-test-skill")]
    [InlineData("test.skill")]
    public void ValidateSkill_InvalidNameFormat_Issue(string name)
    {
        var content = $"""
            ---
            name: {name}
            version: 1.0.0
            description: A test skill
            ---
            # My Skill
            """;

        var result = LintCommand.ValidateSkill(name, $"skills/{name}/SKILL.md", content);

        Assert.Contains(result.Issues, i => i.Contains("Invalid 'name' format"));
    }

    #endregion

    #region File system tests

    [Fact]
    public async Task ExecuteAsync_NonexistentDirectory_Returns1()
    {
        var args = new ParsedArgs { Positional = ["/nonexistent/path"] };

        var result = await LintCommand.ExecuteAsync(args);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_NoArguments_Returns1()
    {
        var args = new ParsedArgs { Positional = [] };

        var result = await LintCommand.ExecuteAsync(args);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Help_Returns0()
    {
        var args = new ParsedArgs { Help = true };

        var result = await LintCommand.ExecuteAsync(args);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_ValidSkills_Returns0()
    {
        CreateSkillDirectory("good-skill", """
            ---
            name: good-skill
            version: 1.0.0
            description: A good skill
            ---
            # Good Skill
            Content here.
            """);

        var args = new ParsedArgs { Positional = [_tempDir] };

        var result = await LintCommand.ExecuteAsync(args);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_BrokenSkills_Returns1()
    {
        CreateSkillDirectory("broken-skill", """
            ---
            name: broken-skill
            description: Missing version
            ---
            # Broken Skill
            """);

        var args = new ParsedArgs { Positional = [_tempDir] };

        var result = await LintCommand.ExecuteAsync(args);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_MixedSkills_Returns1()
    {
        CreateSkillDirectory("good-skill", """
            ---
            name: good-skill
            version: 1.0.0
            description: A good skill
            ---
            # Good Skill
            Content.
            """);

        CreateSkillDirectory("broken-skill", """
            ---
            name: broken-skill
            description: Missing version
            ---
            # Broken Skill
            """);

        var args = new ParsedArgs { Positional = [_tempDir] };

        var result = await LintCommand.ExecuteAsync(args);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_WarningsOnly_Returns0()
    {
        CreateSkillDirectory("warning-skill", """
            ---
            name: warning-skill
            version: 1.0.0
            ---
            # Warning Skill
            """);

        var args = new ParsedArgs { Positional = [_tempDir] };

        var result = await LintCommand.ExecuteAsync(args);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_NoSkillMd_DirectoryWithFiles_Warning()
    {
        var dir = Path.Combine(_tempDir, "incomplete-skill");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "README.md"), "# Readme");

        var args = new ParsedArgs { Positional = [_tempDir] };

        var result = await LintCommand.ExecuteAsync(args);

        Assert.Equal(0, result); // warnings don't fail
    }

    [Fact]
    public async Task ExecuteAsync_SubAgentsDirectoryWithDuplicateNames_Returns1()
    {
        CreateSubAgentFile("one.md", "support-agent");
        CreateSubAgentFile("two.md", "support-agent");

        var args = new ParsedArgs { Positional = ["subagents", _tempDir] };

        var result = await LintCommand.ExecuteAsync(args);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_ValidSubAgentsDirectory_Returns0()
    {
        CreateSubAgentFile("support.md", "support-agent");
        CreateSubAgentFile("review.md", "review-agent");

        var args = new ParsedArgs { Positional = ["subagents", _tempDir] };

        var result = await LintCommand.ExecuteAsync(args);

        Assert.Equal(0, result);
    }

    #endregion

    private void CreateSubAgentFile(string fileName, string name)
    {
        File.WriteAllText(Path.Combine(_tempDir, fileName), $"""
            ---
            name: {name}
            description: Diagnose support issues.
            ---

            You are a support diagnostician.
            """);
    }
}
