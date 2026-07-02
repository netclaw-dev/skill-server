// -----------------------------------------------------------------------
// <copyright file="ValueObjectTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using SkillServer.Models;
using Xunit;

namespace SkillServer.Tests;

public sealed class SkillNameTests
{
    [Theory]
    [InlineData("my-skill", true)]
    [InlineData("skill", true)]
    [InlineData("my-cool-skill", true)]
    [InlineData("skill123", true)]
    [InlineData("a", true)]
    [InlineData("", false)]
    [InlineData("-skill", false)]
    [InlineData("skill-", false)]
    [InlineData("my--skill", false)]
    [InlineData("My-Skill", true)] // Gets normalized to lowercase
    [InlineData("SKILL", true)] // Gets normalized to lowercase
    [InlineData("my_skill", false)]
    [InlineData("my skill", false)]
    [InlineData("my.skill", false)]
    public void TryCreate_ValidatesCorrectly(string input, bool expectedValid)
    {
        var result = SkillName.TryCreate(input, out var name);
        Assert.Equal(expectedValid, result);

        if (expectedValid)
        {
            Assert.NotNull(name);
            Assert.Equal(input.ToLowerInvariant(), name.Value.Value);
        }
    }

    [Fact]
    public void Create_ThrowsOnInvalidName()
    {
        Assert.Throws<ArgumentException>(() => SkillName.Create("-invalid"));
    }

    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        var name = SkillName.Create("my-skill");
        string value = name;
        Assert.Equal("my-skill", value);
    }
}

public sealed class SkillVersionStringTests
{
    [Theory]
    [InlineData("1.0.0", true)]
    [InlineData("1.0", true)]
    [InlineData("1", true)]
    [InlineData("1.0.0-beta.1", true)]
    [InlineData("1.0.0+build.123", true)]
    [InlineData("", false)]
    [InlineData("1.0.0/bad", false)]
    public void TryCreate_ValidatesCorrectly(string input, bool expectedValid)
    {
        var result = SkillVersionString.TryCreate(input, out var version);
        Assert.Equal(expectedValid, result);

        if (expectedValid)
        {
            Assert.NotNull(version);
            Assert.Equal(input, version.Value.Value);
        }
    }
}

public sealed class Sha256DigestTests
{
    private const string ValidHex = "9c15d0e481158f38f2714553745233b9d88e887ac70a95f7d372f5398f0b1145";

    [Theory]
    [InlineData("sha256:9c15d0e481158f38f2714553745233b9d88e887ac70a95f7d372f5398f0b1145", true)]
    [InlineData("9c15d0e481158f38f2714553745233b9d88e887ac70a95f7d372f5398f0b1145", true)] // Gets sha256: prefix added
    [InlineData("SHA256:9C15D0E481158F38F2714553745233B9D88E887AC70A95F7D372F5398F0B1145", true)] // Gets normalized
    [InlineData("sha256:invalid", false)]
    [InlineData("sha256:9c15", false)]
    [InlineData("", false)]
    public void TryCreate_ValidatesCorrectly(string input, bool expectedValid)
    {
        var result = Sha256Digest.TryCreate(input, out var digest);
        Assert.Equal(expectedValid, result);

        if (expectedValid)
        {
            Assert.NotNull(digest);
            Assert.StartsWith("sha256:", digest.Value.Value);
            Assert.Equal(64, digest.Value.HexValue.Length);
        }
    }

    [Fact]
    public void FromHex_CreatesValidDigest()
    {
        var digest = Sha256Digest.FromHex(ValidHex);
        Assert.Equal($"sha256:{ValidHex}", digest.Value);
        Assert.Equal(ValidHex, digest.HexValue);
    }
}

public sealed class ResourcePathTests
{
    [Theory]
    [InlineData("references/guide.md", true)]
    [InlineData("scripts/setup.sh", true)]
    [InlineData("assets/logo.png", true)]
    [InlineData("references/deep/nested/file.md", true)]
    [InlineData("custom/file.txt", true)]
    [InlineData("examples/demo.py", true)]
    [InlineData("references/hidden..file.md", true)]
    [InlineData("../secret.txt", false)]
    [InlineData("references/../secret.txt", false)]
    [InlineData("references/./guide.md", false)]
    [InlineData("references//guide.md", false)]
    [InlineData("/etc/passwd", false)]
    [InlineData("C:/temp/tool", false)]
    [InlineData("C:\\temp\\tool", false)]
    [InlineData("C:temp/tool", false)]
    [InlineData("references/guide:latest.md", false)]
    [InlineData("SKILL.md", false)] // Bare filenames are not resources
    [InlineData("justadirectory/", false)]
    [InlineData("", false)]
    public void TryCreate_ValidatesCorrectly(string input, bool expectedValid)
    {
        var result = ResourcePath.TryCreate(input, out var path);
        Assert.Equal(expectedValid, result);

        if (expectedValid)
        {
            Assert.NotNull(path);
        }
    }
}

public sealed class FileSizeTests
{
    [Fact]
    public void Constructor_ThrowsOnNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FileSize(-1));
    }

    [Theory]
    [InlineData(500, "500 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1572864, "1.5 MB")]
    public void ToString_FormatsCorrectly(long bytes, string expected)
    {
        var size = new FileSize(bytes);
        Assert.Equal(expected, size.ToString());
    }
}
