// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
{
    Args = args,
    DisableDashboard = true
});

builder.AddProject<Projects.SkillServer>("skillserver")
    .WithHttpEndpoint(port: 0, name: "http");

builder.Build().Run();
