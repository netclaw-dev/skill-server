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
[JsonSerializable(typeof(SkillSummary))]
[JsonSerializable(typeof(SkillVersionSummary))]
[JsonSerializable(typeof(IReadOnlyList<SkillSummary>))]
[JsonSerializable(typeof(IReadOnlyList<SkillVersionSummary>))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(CreateApiKeyRequest))]
[JsonSerializable(typeof(CreateApiKeyResponse))]
[JsonSerializable(typeof(ApiKeySummary))]
[JsonSerializable(typeof(IReadOnlyList<ApiKeySummary>))]
[JsonSerializable(typeof(IReadOnlyList<CheckUpdateRequestItem>))]
[JsonSerializable(typeof(IReadOnlyList<CheckUpdateResponseItem>))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class SkillServerJsonContext : JsonSerializerContext;
