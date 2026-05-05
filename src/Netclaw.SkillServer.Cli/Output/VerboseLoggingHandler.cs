// -----------------------------------------------------------------------
// <copyright file="VerboseLoggingHandler.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace Netclaw.SkillServer.Cli.Output;

internal sealed class VerboseLoggingHandler : DelegatingHandler
{
    public VerboseLoggingHandler() : base(new HttpClientHandler())
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ConsoleOutput.WriteDim($"  > {request.Method} {request.RequestUri}");

        var sw = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        sw.Stop();

        ConsoleOutput.WriteDim($"  < {(int)response.StatusCode} {response.StatusCode} ({sw.ElapsedMilliseconds}ms)");

        return response;
    }
}
