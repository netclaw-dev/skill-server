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
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class SkillServerJsonContext : JsonSerializerContext;
