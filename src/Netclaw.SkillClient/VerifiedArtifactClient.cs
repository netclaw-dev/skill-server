// -----------------------------------------------------------------------
// <copyright file="VerifiedArtifactClient.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Security.Cryptography;

namespace Netclaw.SkillClient;

public sealed partial class SkillServerClient
{
    public Task<VerifiedArtifactDownload> DownloadNativeSkillArtifactAsync(
        NativeSkillArtifact artifact,
        Stream destination,
        CancellationToken ct = default)
    {
        return DownloadVerifiedArtifactAsync(artifact.Url, artifact.Digest, destination, ct);
    }

    public Task<VerifiedArtifactDownload> DownloadNativeSubAgentArtifactAsync(
        NativeSubAgentVersionDetail subAgentVersion,
        Stream destination,
        CancellationToken ct = default)
    {
        return DownloadVerifiedArtifactAsync(subAgentVersion.Url, subAgentVersion.Digest, destination, ct);
    }

    public async Task<byte[]> DownloadVerifiedArtifactBytesAsync(
        string url,
        string expectedDigest,
        CancellationToken ct = default)
    {
        using var destination = new MemoryStream();
        await DownloadVerifiedArtifactAsync(url, expectedDigest, destination, ct);
        return destination.ToArray();
    }

    public async Task<VerifiedArtifactDownload> DownloadVerifiedArtifactAsync(
        string url,
        string expectedDigest,
        Stream destination,
        CancellationToken ct = default)
    {
        if (!destination.CanWrite)
            throw new ArgumentException("Destination stream must be writable.", nameof(destination));

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(ct);
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long totalBytes = 0;

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer, ct);
            if (bytesRead == 0)
                break;

            sha256.AppendData(buffer.AsSpan(0, bytesRead));
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalBytes += bytesRead;
        }

        var actualDigest = FormatSha256Digest(sha256.GetHashAndReset());
        var expectedHex = NormalizeSha256Digest(expectedDigest);
        if (!NormalizeSha256Digest(actualDigest).Equals(expectedHex, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Downloaded artifact digest mismatch. Expected sha256:{expectedHex} but received {actualDigest}.");
        }

        return new VerifiedArtifactDownload
        {
            Url = url,
            Digest = actualDigest,
            SizeBytes = totalBytes
        };
    }

    private static string NormalizeSha256Digest(string digest)
    {
        return digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
            ? digest[7..].ToLowerInvariant()
            : digest.ToLowerInvariant();
    }

    private static string FormatSha256Digest(byte[] hashBytes)
    {
        return $"sha256:{Convert.ToHexString(hashBytes).ToLowerInvariant()}";
    }
}
