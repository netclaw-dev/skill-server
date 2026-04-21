using System.Security.Cryptography;
using SkillServer.Models;

namespace SkillServer.Services;

/// <summary>
/// Content-addressable blob storage using SHA-256 digests as filenames.
/// </summary>
public sealed class BlobStorage
{
    private readonly string _blobsPath;
    private readonly ILogger<BlobStorage> _logger;

    public BlobStorage(IConfiguration configuration, ILogger<BlobStorage> logger)
    {
        var dataPath = configuration["SkillServer:DataPath"] ?? "./data";
        _blobsPath = Path.Combine(dataPath, "blobs", "sha256");
        Directory.CreateDirectory(_blobsPath);
        _logger = logger;
    }

    /// <summary>
    /// Stores content and returns the SHA-256 digest.
    /// </summary>
    public async Task<(string Digest, long SizeBytes)> StoreAsync(Stream content, CancellationToken ct = default)
    {
        // First, compute hash and copy to temp file
        var tempPath = Path.GetTempFileName();
        string hashHex;
        long sizeBytes;

        try
        {
            await using (var tempStream = File.Create(tempPath))
            {
                using var sha256 = SHA256.Create();
                var buffer = new byte[81920];
                int bytesRead;
                sizeBytes = 0;

                while ((bytesRead = await content.ReadAsync(buffer, ct)) > 0)
                {
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                    await tempStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    sizeBytes += bytesRead;
                }

                sha256.TransformFinalBlock([], 0, 0);
                hashHex = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
            }

            // Move to content-addressable location
            var blobPath = GetBlobPath(hashHex);

            if (!File.Exists(blobPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(blobPath)!);
                File.Move(tempPath, blobPath);
                _logger.LogDebug("Stored blob {Digest} ({Size} bytes)", hashHex, sizeBytes);
            }
            else
            {
                // Already exists (deduplication)
                File.Delete(tempPath);
                _logger.LogDebug("Blob {Digest} already exists (deduplicated)", hashHex);
            }

            return ($"sha256:{hashHex}", sizeBytes);
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    /// <summary>
    /// Stores content from a byte array and returns the SHA-256 digest.
    /// </summary>
    public async Task<(string Digest, long SizeBytes)> StoreAsync(byte[] content, CancellationToken ct = default)
    {
        using var stream = new MemoryStream(content);
        return await StoreAsync(stream, ct);
    }

    /// <summary>
    /// Gets a stream for reading blob content.
    /// </summary>
    public Stream? GetBlob(string digest)
    {
        var hashHex = Sha256Digest.Create(digest).HexValue;
        var blobPath = GetBlobPath(hashHex);

        if (!File.Exists(blobPath))
            return null;

        return File.OpenRead(blobPath);
    }

    /// <summary>
    /// Checks if a blob exists.
    /// </summary>
    public bool Exists(string digest)
    {
        var hashHex = Sha256Digest.Create(digest).HexValue;
        return File.Exists(GetBlobPath(hashHex));
    }

    /// <summary>
    /// Gets the full path for a blob (for static file serving).
    /// </summary>
    public string? GetBlobFilePath(string digest)
    {
        var hashHex = Sha256Digest.Create(digest).HexValue;
        var path = GetBlobPath(hashHex);
        return File.Exists(path) ? path : null;
    }

    private string GetBlobPath(string hashHex)
    {
        var prefix = hashHex[..8];
        return Path.Combine(_blobsPath, prefix, hashHex);
    }
}
