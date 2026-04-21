using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SkillServer.Models;
using SkillServer.Services;

namespace SkillServer.Controllers;

/// <summary>
/// Serves discovery indexes for RFC and NetClaw formats.
/// </summary>
[ApiController]
public sealed class DiscoveryController : ControllerBase
{
    private readonly IndexGenerator _indexGenerator;

    public DiscoveryController(IndexGenerator indexGenerator)
    {
        _indexGenerator = indexGenerator;
    }

    /// <summary>
    /// RFC-compliant discovery index.
    /// </summary>
    [HttpGet("/.well-known/agent-skills/index.json")]
    [Produces("application/json")]
    public async Task<IActionResult> GetRfcIndex(CancellationToken ct)
    {
        var index = await _indexGenerator.GenerateRfcIndexAsync(ct);
        return new JsonResult(index, SkillServerJsonContext.Default.RfcSkillIndex);
    }

    /// <summary>
    /// NetClaw-compatible manifest.
    /// </summary>
    [HttpGet("/manifest.json")]
    [Produces("application/json")]
    public async Task<IActionResult> GetNetclawManifest(CancellationToken ct)
    {
        var manifest = await _indexGenerator.GenerateNetclawManifestAsync(ct);
        return new JsonResult(manifest, SkillServerJsonContext.Default.NetclawManifest);
    }
}
