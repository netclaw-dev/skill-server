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
    private static readonly DateTimeOffset FixedTimestamp = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static byte[] BuildZip(
        byte[] skillMdContent,
        IReadOnlyList<(ResourcePath Path, byte[] Content)> resources)
    {
        using var archiveStream = new MemoryStream();
        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "SKILL.md", skillMdContent);

            foreach (var resource in resources.OrderBy(r => r.Path.Value, StringComparer.Ordinal))
            {
                AddEntry(archive, resource.Path.Value, resource.Content);
            }
        }

        return archiveStream.ToArray();
    }

    private static void AddEntry(ZipArchive archive, string path, byte[] content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.NoCompression);
        entry.LastWriteTime = FixedTimestamp;
        entry.ExternalAttributes = 0;

        using var entryStream = entry.Open();
        entryStream.Write(content);
    }
}
