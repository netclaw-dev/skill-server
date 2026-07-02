// -----------------------------------------------------------------------
// <copyright file="SkillArchiveBuilder.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.IO.Compression;
using SkillServer.Models;

namespace SkillServer.Services;

internal static class SkillArchiveBuilder
{
    private const int RegularFileType = 0x8000;
    private const int DefaultFileMode = 0x1A4; // 0644
    internal const int PermissionBitsMask = 0x1FF;
    private static readonly DateTimeOffset FixedTimestamp = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static byte[] BuildZip(
        byte[] skillMdContent,
        IReadOnlyList<SkillArchiveResource> resources)
    {
        using var archiveStream = new MemoryStream();
        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "SKILL.md", skillMdContent, unixMode: null);

            foreach (var resource in resources.OrderBy(r => r.Path.Value, StringComparer.Ordinal))
            {
                AddEntry(archive, resource.Path.Value, resource.Content, resource.UnixMode);
            }
        }

        return archiveStream.ToArray();
    }

    private static void AddEntry(ZipArchive archive, string path, byte[] content, int? unixMode)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.NoCompression);
        entry.LastWriteTime = FixedTimestamp;
        entry.ExternalAttributes = ToZipExternalAttributes(unixMode);

        using var entryStream = entry.Open();
        entryStream.Write(content);
    }

    internal static int ToZipExternalAttributes(int? unixMode)
    {
        var mode = NormalizeUnixMode(unixMode);
        return unchecked((int)((RegularFileType | mode) << 16));
    }

    internal static bool IsSafeUnixMode(int unixMode)
        => unixMode >= 0 && (unixMode & ~PermissionBitsMask) == 0;

    internal static int NormalizeUnixMode(int? unixMode)
        => (unixMode ?? DefaultFileMode) & PermissionBitsMask;
}

internal readonly record struct SkillArchiveResource(ResourcePath Path, byte[] Content, int? UnixMode);
