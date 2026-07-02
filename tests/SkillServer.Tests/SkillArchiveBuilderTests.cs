// -----------------------------------------------------------------------
// <copyright file="SkillArchiveBuilderTests.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.IO.Compression;
using System.Text;
using SkillServer.Models;
using SkillServer.Services;
using Xunit;

namespace SkillServer.Tests;

public sealed class SkillArchiveBuilderTests
{
    [Fact]
    public void BuildZip_IsDeterministicAndUsesExpectedLayout()
    {
        var skillMd = Encoding.UTF8.GetBytes("---\nname: test\ndescription: test\n---\n# Test");
        var guide = Encoding.UTF8.GetBytes("# Guide");
        var script = Encoding.UTF8.GetBytes("#!/bin/bash\necho setup");

        var resources = new List<SkillArchiveResource>
        {
            new(ResourcePath.Create("scripts/setup.sh"), script, 0x1ED),
            new(ResourcePath.Create("references/guide.md"), guide, 0x1A4)
        };

        var reversedResources = resources.AsEnumerable().Reverse().ToList();

        var first = SkillArchiveBuilder.BuildZip(skillMd, resources);
        var second = SkillArchiveBuilder.BuildZip(skillMd, reversedResources);

        Assert.Equal(first, second);

        using var archiveStream = new MemoryStream(first);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read);

        Assert.Equal([
            "SKILL.md",
            "references/guide.md",
            "scripts/setup.sh"
        ], archive.Entries.Select(e => e.FullName).ToArray());

        Assert.All(archive.Entries, entry =>
            Assert.Equal(new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero), entry.LastWriteTime));

        Assert.Equal(0x1A4, GetUnixMode(archive.GetEntry("SKILL.md")!));
        Assert.Equal(0x1A4, GetUnixMode(archive.GetEntry("references/guide.md")!));
        Assert.Equal(0x1ED, GetUnixMode(archive.GetEntry("scripts/setup.sh")!));
    }

    private static int GetUnixMode(ZipArchiveEntry entry)
        => (entry.ExternalAttributes >> 16) & 0xFFF;
}
