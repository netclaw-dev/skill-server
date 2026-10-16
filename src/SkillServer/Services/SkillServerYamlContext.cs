// -----------------------------------------------------------------------
// <copyright file="SkillServerYamlContext.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using YamlDotNet.Serialization;

namespace SkillServer.Services;

/// <summary>
/// Static YAML serialization context for AOT compilation support.
/// Registers the frontmatter types deserialized from uploaded SKILL.md and agent.md
/// files so YamlDotNet generates reflection-free object accessors at compile time.
/// </summary>
[YamlStaticContext]
[YamlSerializable(typeof(SkillFrontmatter))]
[YamlSerializable(typeof(SubAgentFrontmatter))]
public partial class SkillServerYamlContext : StaticContext;
