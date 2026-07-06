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

        // ZIP stores DOS date/time with no timezone, so LastWriteTime reads back with the local
        // offset. Assert on the wall-clock component, which is what the format round-trips
        // deterministically across machines regardless of their timezone.
        Assert.All(archive.Entries, entry =>
            Assert.Equal(new DateTime(1980, 1, 1, 0, 0, 0), entry.LastWriteTime.DateTime));

        Assert.Equal(0x1A4, GetUnixMode(archive.GetEntry("SKILL.md")!));
        Assert.Equal(0x1A4, GetUnixMode(archive.GetEntry("references/guide.md")!));
        Assert.Equal(0x1ED, GetUnixMode(archive.GetEntry("scripts/setup.sh")!));
    }

    [Fact]
    public void ToZipExternalAttributes_WritesRegularFileMetadataPortably()
    {
        var attributes = SkillArchiveBuilder.ToZipExternalAttributes(0x1ED);

        var rawMode = GetRawUnixMode(attributes);
        Assert.Equal(0x8000, rawMode & 0xF000);
        Assert.Equal(0x1ED, rawMode & 0x1FF);
    }

    [Fact]
    public void BuildZip_StripsSpecialUnixModeBits()
    {
        var skillMd = Encoding.UTF8.GetBytes("---\nname: test\ndescription: test\n---\n# Test");
        var script = Encoding.UTF8.GetBytes("#!/bin/sh\necho ok\n");

        var zip = SkillArchiveBuilder.BuildZip(skillMd,
        [
            new SkillArchiveResource(ResourcePath.Create("scripts/tool"), script, 0xFED)
        ]);

        using var archiveStream = new MemoryStream(zip);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read);
        var rawMode = GetRawUnixMode(archive.GetEntry("scripts/tool")!.ExternalAttributes);

        Assert.Equal(0x8000, rawMode & 0xF000);
        Assert.Equal(0x1ED, rawMode & 0x1FF);
        Assert.Equal(0, rawMode & 0x0E00);
    }

    [Fact]
    public void BuildZip_WhenExtractedOnUnix_RestoresExecutableBit()
    {
        if (OperatingSystem.IsWindows())
            return;

        var tempDir = Path.Combine(Path.GetTempPath(), "skillserver-archive-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var skillMd = Encoding.UTF8.GetBytes("---\nname: test\ndescription: test\n---\n# Test");
            var script = Encoding.UTF8.GetBytes("#!/bin/sh\necho ok\n");
            var zip = SkillArchiveBuilder.BuildZip(skillMd,
            [
                new SkillArchiveResource(ResourcePath.Create("scripts/tool"), script, 0x1ED)
            ]);

            var zipPath = Path.Combine(tempDir, "archive.zip");
            var extractDir = Path.Combine(tempDir, "extracted");
            File.WriteAllBytes(zipPath, zip);
            ZipFile.ExtractToDirectory(zipPath, extractDir);

            var mode = (int)(File.GetUnixFileMode(Path.Combine(extractDir, "scripts", "tool")) &
                             (UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                              UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                              UnixFileMode.OtherRead | UnixFileMode.OtherExecute));
            Assert.Equal(0x1ED, mode);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static int GetUnixMode(ZipArchiveEntry entry)
        => GetRawUnixMode(entry.ExternalAttributes) & 0x1FF;

    private static int GetRawUnixMode(int externalAttributes)
        => (externalAttributes >> 16) & 0xFFFF;
}
