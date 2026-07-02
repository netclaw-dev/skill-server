// -----------------------------------------------------------------------
// <copyright file="SkillDirectoryScannerTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Netclaw.SkillServer.Cli.Publishing;
using Xunit;

namespace Netclaw.SkillServer.Cli.Tests;

public sealed class SkillDirectoryScannerTests : IDisposable
{
    private readonly string _tempDir;

    public SkillDirectoryScannerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "skillserver-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void ScanDirectory_WithValidSkill_ReturnsScannedSkill()
    {
        var skillDir = Path.Combine(_tempDir, "my-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), """
            ---
            name: my-skill
            version: 1.0.0
            description: A test skill
            category: testing
            ---
            # My Skill
            Content here.
            """);

        var result = SkillDirectoryScanner.ScanDirectory(skillDir);

        Assert.NotNull(result);
        Assert.Equal("my-skill", result.Name);
        Assert.Equal("1.0.0", result.Version);
        Assert.Equal("testing", result.Category);
        Assert.Equal("A test skill", result.Description);
        Assert.Empty(result.Resources);
    }

    [Fact]
    public void ScanDirectory_WithResources_FindsResourceFiles()
    {
        var skillDir = Path.Combine(_tempDir, "skill-with-resources");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), """
            ---
            name: skill-with-resources
            version: 2.0.0
            description: Has resources
            ---
            # Skill
            """);

        var refDir = Path.Combine(skillDir, "references");
        Directory.CreateDirectory(refDir);
        File.WriteAllText(Path.Combine(refDir, "guide.md"), "# Guide");
        File.WriteAllText(Path.Combine(refDir, "config.md"), "# Config");

        var result = SkillDirectoryScanner.ScanDirectory(skillDir);

        Assert.NotNull(result);
        Assert.Equal(2, result.Resources.Count);
        Assert.Contains(result.Resources, r => r.RelativePath == "references/guide.md");
        Assert.Contains(result.Resources, r => r.RelativePath == "references/config.md");
    }

    [Fact]
    public void ScanDirectory_CapturesUnixResourceMode()
    {
        if (OperatingSystem.IsWindows())
            return;

        var skillDir = Path.Combine(_tempDir, "skill-with-executable");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), """
            ---
            name: skill-with-executable
            version: 1.0.0
            description: Has executable resource
            ---
            # Skill
            """);

        var binDir = Path.Combine(skillDir, "bin");
        Directory.CreateDirectory(binDir);
        var toolPath = Path.Combine(binDir, "tool");
        File.WriteAllText(toolPath, "#!/bin/sh\necho ok\n");
        File.SetUnixFileMode(toolPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

        var result = SkillDirectoryScanner.ScanDirectory(skillDir);

        Assert.NotNull(result);
        var resource = Assert.Single(result.Resources);
        Assert.Equal("bin/tool", resource.RelativePath);
        Assert.Equal(0x1ED, resource.UnixMode);
    }

    [Fact]
    public void ScanDirectory_StripsSpecialUnixModeBits()
    {
        if (OperatingSystem.IsWindows())
            return;

        var skillDir = Path.Combine(_tempDir, "skill-with-special-mode");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), """
            ---
            name: skill-with-special-mode
            version: 1.0.0
            description: Has special mode bits
            ---
            # Skill
            """);

        var binDir = Path.Combine(skillDir, "bin");
        Directory.CreateDirectory(binDir);
        var toolPath = Path.Combine(binDir, "tool");
        File.WriteAllText(toolPath, "#!/bin/sh\necho ok\n");
        File.SetUnixFileMode(toolPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherExecute |
            UnixFileMode.SetUser | UnixFileMode.SetGroup | UnixFileMode.StickyBit);

        var result = SkillDirectoryScanner.ScanDirectory(skillDir);

        Assert.NotNull(result);
        var resource = Assert.Single(result.Resources);
        Assert.Equal("bin/tool", resource.RelativePath);
        Assert.Equal(0x1ED, resource.UnixMode);
    }

    [Fact]
    public void ScanDirectory_RejectsSymlinkResourceFiles()
    {
        if (OperatingSystem.IsWindows())
            return;

        var skillDir = Path.Combine(_tempDir, "skill-with-symlink-file");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), """
            ---
            name: skill-with-symlink-file
            version: 1.0.0
            description: Has symlink resource
            ---
            # Skill
            """);

        var refDir = Path.Combine(skillDir, "references");
        Directory.CreateDirectory(refDir);
        var targetPath = Path.Combine(_tempDir, "outside.txt");
        File.WriteAllText(targetPath, "outside");
        File.CreateSymbolicLink(Path.Combine(refDir, "outside.txt"), targetPath);

        var ex = Assert.Throws<InvalidOperationException>(() => SkillDirectoryScanner.ScanDirectory(skillDir));
        Assert.Contains("symbolic links or reparse points", ex.Message);
    }

    [Fact]
    public void ScanDirectory_RejectsSymlinkResourceDirectories()
    {
        if (OperatingSystem.IsWindows())
            return;

        var skillDir = Path.Combine(_tempDir, "skill-with-symlink-dir");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), """
            ---
            name: skill-with-symlink-dir
            version: 1.0.0
            description: Has symlink directory
            ---
            # Skill
            """);

        var outsideDir = Path.Combine(_tempDir, "outside-dir");
        Directory.CreateDirectory(outsideDir);
        File.WriteAllText(Path.Combine(outsideDir, "outside.txt"), "outside");
        Directory.CreateSymbolicLink(Path.Combine(skillDir, "references"), outsideDir);

        var ex = Assert.Throws<InvalidOperationException>(() => SkillDirectoryScanner.ScanDirectory(skillDir));
        Assert.Contains("symbolic links or reparse points", ex.Message);
    }

    [Fact]
    public void ScanDirectory_MissingSkillMd_ReturnsNull()
    {
        var skillDir = Path.Combine(_tempDir, "empty-skill");
        Directory.CreateDirectory(skillDir);

        var result = SkillDirectoryScanner.ScanDirectory(skillDir);

        Assert.Null(result);
    }

    [Fact]
    public void ScanDirectory_MissingFrontmatter_ReturnsNull()
    {
        var skillDir = Path.Combine(_tempDir, "no-frontmatter");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), "# Just content, no frontmatter");

        var result = SkillDirectoryScanner.ScanDirectory(skillDir);

        Assert.Null(result);
    }

    [Fact]
    public void ScanDirectory_MissingName_ReturnsNull()
    {
        var skillDir = Path.Combine(_tempDir, "no-name");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), """
            ---
            version: 1.0.0
            description: Missing name
            ---
            # Content
            """);

        var result = SkillDirectoryScanner.ScanDirectory(skillDir);

        Assert.Null(result);
    }

    [Fact]
    public void ScanDirectory_MissingVersion_ReturnsNull()
    {
        var skillDir = Path.Combine(_tempDir, "no-version");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), """
            ---
            name: no-version
            description: Missing version
            ---
            # Content
            """);

        var result = SkillDirectoryScanner.ScanDirectory(skillDir);

        Assert.Null(result);
    }

    [Fact]
    public void ScanDirectory_NonexistentPath_ReturnsNull()
    {
        var result = SkillDirectoryScanner.ScanDirectory("/nonexistent/path");

        Assert.Null(result);
    }

    [Fact]
    public void ScanAll_FindsMultipleSkills()
    {
        CreateSkill("alpha", "1.0.0");
        CreateSkill("beta", "2.0.0");
        CreateSkill("gamma", "0.1.0");

        // Create a non-skill directory
        Directory.CreateDirectory(Path.Combine(_tempDir, "not-a-skill"));

        var results = SkillDirectoryScanner.ScanAll(_tempDir);

        Assert.Equal(3, results.Count);
        Assert.Equal("alpha", results[0].Name);
        Assert.Equal("beta", results[1].Name);
        Assert.Equal("gamma", results[2].Name);
    }

    [Fact]
    public void ScanAll_EmptyDirectory_ReturnsEmpty()
    {
        var results = SkillDirectoryScanner.ScanAll(_tempDir);

        Assert.Empty(results);
    }

    [Fact]
    public void ScanAll_NonexistentDirectory_ReturnsEmpty()
    {
        var results = SkillDirectoryScanner.ScanAll("/nonexistent/path");

        Assert.Empty(results);
    }

    [Fact]
    public void ParseFrontmatter_ValidYaml_ReturnsDictionary()
    {
        var content = """
            ---
            name: test-skill
            version: 1.0.0
            description: A test
            ---
            # Content
            """;

        var result = SkillDirectoryScanner.ParseFrontmatter(content);

        Assert.NotNull(result);
        Assert.Equal("test-skill", result["name"]);
        Assert.Equal("1.0.0", result["version"]);
        Assert.Equal("A test", result["description"]);
    }

    [Fact]
    public void ParseFrontmatter_NoFrontmatter_ReturnsNull()
    {
        var result = SkillDirectoryScanner.ParseFrontmatter("# Just content");

        Assert.Null(result);
    }

    [Fact]
    public void ScanDirectory_NestedResources_FindsAll()
    {
        var skillDir = Path.Combine(_tempDir, "nested-resources");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), """
            ---
            name: nested-resources
            version: 1.0.0
            description: Has nested resources
            ---
            # Skill
            """);

        var deepDir = Path.Combine(skillDir, "assets", "images");
        Directory.CreateDirectory(deepDir);
        File.WriteAllText(Path.Combine(deepDir, "diagram.md"), "# Diagram");

        var result = SkillDirectoryScanner.ScanDirectory(skillDir);

        Assert.NotNull(result);
        Assert.Single(result.Resources);
        Assert.Equal("assets/images/diagram.md", result.Resources[0].RelativePath);
    }

    private void CreateSkill(string name, string version)
    {
        var dir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), $"""
            ---
            name: {name}
            version: {version}
            description: Skill {name}
            ---
            # {name}
            """);
    }
}
