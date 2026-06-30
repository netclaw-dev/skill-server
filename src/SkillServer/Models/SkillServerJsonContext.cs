// -----------------------------------------------------------------------
// <copyright file="SkillServerJsonContext.cs" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using System.Text.Json.Serialization;

namespace SkillServer.Models;

/// <summary>
/// JSON serialization context for AOT compilation support.
/// </summary>
[JsonSerializable(typeof(RfcSkillIndex))]
[JsonSerializable(typeof(RfcSkillEntry))]
[JsonSerializable(typeof(RfcResourceEntry))]
[JsonSerializable(typeof(SkillUploadRequest))]
[JsonSerializable(typeof(SkillUploadResponse))]
[JsonSerializable(typeof(SubAgentUploadResponse))]
[JsonSerializable(typeof(SkillSummary))]
[JsonSerializable(typeof(SkillVersionSummary))]
[JsonSerializable(typeof(SubAgentSummary))]
[JsonSerializable(typeof(SubAgentVersionSummary))]
[JsonSerializable(typeof(IReadOnlyList<SkillSummary>))]
[JsonSerializable(typeof(IReadOnlyList<SkillVersionSummary>))]
[JsonSerializable(typeof(IReadOnlyList<SubAgentSummary>))]
[JsonSerializable(typeof(IReadOnlyList<SubAgentVersionSummary>))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(CreateApiKeyRequest))]
[JsonSerializable(typeof(CreateApiKeyResponse))]
[JsonSerializable(typeof(ApiKeySummary))]
[JsonSerializable(typeof(IReadOnlyList<ApiKeySummary>))]
[JsonSerializable(typeof(IReadOnlyList<CheckUpdateRequestItem>))]
[JsonSerializable(typeof(IReadOnlyList<CheckUpdateResponseItem>))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(NativeRootManifest))]
[JsonSerializable(typeof(NativeSkillCollectionIndex))]
[JsonSerializable(typeof(NativeSkillCollectionPage))]
[JsonSerializable(typeof(NativeSkillIdentityIndex))]
[JsonSerializable(typeof(NativeSkillVersionDetail))]
[JsonSerializable(typeof(NativeSubAgentCollectionIndex))]
[JsonSerializable(typeof(NativeSubAgentCollectionPage))]
[JsonSerializable(typeof(NativeSubAgentIdentityIndex))]
[JsonSerializable(typeof(NativeSubAgentVersionDetail))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class SkillServerJsonContext : JsonSerializerContext;
