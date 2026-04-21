using Microsoft.AspNetCore.Mvc;
using SkillServer.Models;
using SkillServer.Services;

namespace SkillServer.Controllers;

/// <summary>
/// Serves the RFC-compliant skill discovery index.
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
    /// RFC-compliant discovery index per Cloudflare Agent Skills Discovery RFC v0.2.0.
    /// </summary>
    [HttpGet("/.well-known/agent-skills/index.json")]
    [Produces("application/json")]
    public async Task<IActionResult> GetRfcIndex(CancellationToken ct)
    {
        var index = await _indexGenerator.GenerateRfcIndexAsync(ct);
        return new JsonResult(index, SkillServerJsonContext.Default.RfcSkillIndex);
    }
}
