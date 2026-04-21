using Microsoft.AspNetCore.Mvc;
using SkillServer.Services;

namespace SkillServer.Controllers;

/// <summary>
/// Content-addressable blob downloads.
/// </summary>
[ApiController]
[Route("blobs")]
public sealed class BlobsController : ControllerBase
{
    private readonly BlobStorage _blobStorage;

    public BlobsController(BlobStorage blobStorage)
    {
        _blobStorage = blobStorage;
    }

    /// <summary>
    /// Download a blob by SHA-256 digest.
    /// </summary>
    [HttpGet("sha256/{digest}")]
    public IActionResult GetBlob(string digest)
    {
        var stream = _blobStorage.GetBlob(digest);
        if (stream is null)
            return NotFound();

        return File(stream, "application/octet-stream");
    }

    /// <summary>
    /// Check if a blob exists.
    /// </summary>
    [HttpHead("sha256/{digest}")]
    public IActionResult HeadBlob(string digest)
    {
        return _blobStorage.Exists(digest) ? Ok() : NotFound();
    }
}
