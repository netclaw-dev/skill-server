// -----------------------------------------------------------------------
// <copyright file="TestEnvironmentInitializer.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Runtime.CompilerServices;

namespace SkillServer.Integration.Tests;

internal static class TestEnvironmentInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");
    }
}
