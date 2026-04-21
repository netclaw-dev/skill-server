var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
{
    Args = args,
    DisableDashboard = true
});

builder.AddProject<Projects.SkillServer>("skillserver")
    .WithHttpEndpoint(port: 0, name: "http");

builder.Build().Run();
