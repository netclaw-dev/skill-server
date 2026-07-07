// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
var builder = DistributedApplication.CreateBuilder(args);
var skillServerProjectDirectory = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "SkillServer"));

builder.AddProject<Projects.SkillServer>("skillserver")
    .WithHttpEndpoint(port: 0, name: "http")
    .WithEnvironment("SkillServer__SeedData", builder.Configuration["SkillServer:SeedData"] ?? "true")
    .WithEnvironment("SkillServer__DataPath", Path.Combine(skillServerProjectDirectory, "data"));

builder.Build().Run();
