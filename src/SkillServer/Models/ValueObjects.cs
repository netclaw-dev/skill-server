using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SkillServer.Models;

/// <summary>
/// A valid skill name (1-64 lowercase alphanumeric + hyphens, no leading/trailing/consecutive hyphens).
/// </summary>
public readonly partial record struct SkillName
{
    public string Value { get; }

    private SkillName(string value) => Value = value;

    public static bool TryCreate(string? value, [NotNullWhen(true)] out SkillName? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
            return false;

        var normalized = value.ToLowerInvariant();

        if (!ValidNameRegex().IsMatch(normalized))
            return false;

        result = new SkillName(normalized);
        return true;
    }

    public static SkillName Create(string value)
    {
        if (!TryCreate(value, out var result))
            throw new ArgumentException($"Invalid skill name: '{value}'. Must be 1-64 lowercase alphanumeric characters and hyphens, no leading/trailing/consecutive hyphens.", nameof(value));
        return result.Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(SkillName name) => name.Value;

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex ValidNameRegex();
}

/// <summary>
/// A semantic version string.
/// </summary>
public readonly record struct SkillVersionString
{
    public string Value { get; }

    private SkillVersionString(string value) => Value = value;

    public static bool TryCreate(string? value, [NotNullWhen(true)] out SkillVersionString? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
            return false;

        // Basic semver-ish validation (allows 1.0.0, 1.0, 1.0.0-beta.1, etc.)
        if (!value.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '+'))
            return false;

        result = new SkillVersionString(value);
        return true;
    }

    public static SkillVersionString Create(string value)
    {
        if (!TryCreate(value, out var result))
            throw new ArgumentException($"Invalid version string: '{value}'.", nameof(value));
        return result.Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(SkillVersionString version) => version.Value;
}

/// <summary>
/// A SHA-256 digest in the format "sha256:{64 hex chars}".
/// </summary>
public readonly partial record struct Sha256Digest
{
    public string Value { get; }

    /// <summary>
    /// Returns just the hex portion without the "sha256:" prefix.
    /// </summary>
    public string HexValue => Value.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
        ? Value[7..]
        : Value;

    private Sha256Digest(string value) => Value = value;

    public static bool TryCreate(string? value, [NotNullWhen(true)] out Sha256Digest? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
            ? value
            : $"sha256:{value}";

        if (!ValidDigestRegex().IsMatch(normalized))
            return false;

        result = new Sha256Digest(normalized.ToLowerInvariant());
        return true;
    }

    public static Sha256Digest Create(string value)
    {
        if (!TryCreate(value, out var result))
            throw new ArgumentException($"Invalid SHA-256 digest: '{value}'. Expected format: sha256:{{64 hex chars}}.", nameof(value));
        return result.Value;
    }

    public static Sha256Digest FromHex(string hexValue)
    {
        return Create($"sha256:{hexValue}");
    }

    public override string ToString() => Value;

    public static implicit operator string(Sha256Digest digest) => digest.Value;

    [GeneratedRegex(@"^sha256:[a-f0-9]{64}$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ValidDigestRegex();
}

/// <summary>
/// A relative file path within a skill (e.g., "references/guide.md").
/// </summary>
public readonly record struct ResourcePath
{
    private static readonly string[] AllowedPrefixes = ["references/", "scripts/", "assets/"];

    public string Value { get; }

    private ResourcePath(string value) => Value = value;

    public static bool TryCreate(string? value, [NotNullWhen(true)] out ResourcePath? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        // No path traversal
        if (value.Contains("..") || value.StartsWith('/') || value.StartsWith('\\'))
            return false;

        // Normalize separators
        var normalized = value.Replace('\\', '/');

        // Must be in allowed directories
        if (!AllowedPrefixes.Any(p => normalized.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return false;

        result = new ResourcePath(normalized);
        return true;
    }

    public static ResourcePath Create(string value)
    {
        if (!TryCreate(value, out var result))
            throw new ArgumentException($"Invalid resource path: '{value}'. Must be in references/, scripts/, or assets/ directories with no path traversal.", nameof(value));
        return result.Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(ResourcePath path) => path.Value;
}

/// <summary>
/// A positive file size in bytes.
/// </summary>
public readonly record struct FileSize
{
    public long Bytes { get; }

    public FileSize(long bytes)
    {
        if (bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes), "File size cannot be negative.");
        Bytes = bytes;
    }

    public double Kilobytes => Bytes / 1024.0;
    public double Megabytes => Bytes / (1024.0 * 1024.0);

    public override string ToString() => Bytes switch
    {
        < 1024 => $"{Bytes} B",
        < 1024 * 1024 => $"{Kilobytes:F1} KB",
        _ => $"{Megabytes:F1} MB"
    };

    public static implicit operator long(FileSize size) => size.Bytes;
    public static explicit operator FileSize(long bytes) => new(bytes);
}

/// <summary>
/// JSON converters for value objects (AOT-compatible).
/// </summary>
[JsonSerializable(typeof(SkillName))]
[JsonSerializable(typeof(SkillVersionString))]
[JsonSerializable(typeof(Sha256Digest))]
[JsonSerializable(typeof(ResourcePath))]
[JsonSerializable(typeof(FileSize))]
public partial class ValueObjectsJsonContext : JsonSerializerContext;
