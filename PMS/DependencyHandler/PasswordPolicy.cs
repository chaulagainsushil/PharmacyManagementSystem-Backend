namespace PMS.DependencyHandler;

/// <summary>
/// Strongly-typed representation of the <c>PasswordPolicy</c> section in appsettings.json.
/// Bound at startup and consumed by <see cref="ServiceExtensions.AddApplicationServices"/>.
/// </summary>
public sealed class PasswordPolicy
{
    public const string Section = "PasswordPolicy";

    /// <summary>Minimum number of characters. Default: 8.</summary>
    public int RequiredLength { get; init; } = 8;

    /// <summary>Require at least one uppercase letter (A–Z).</summary>
    public bool RequireUppercase { get; init; } = true;

    /// <summary>Require at least one lowercase letter (a–z).</summary>
    public bool RequireLowercase { get; init; } = true;

    /// <summary>Require at least one digit (0–9).</summary>
    public bool RequireDigit { get; init; } = true;

    /// <summary>Require at least one non-alphanumeric character (e.g. !, @, #).</summary>
    public bool RequireNonAlphanumeric { get; init; } = true;

    /// <summary>Minimum number of unique characters in the password. Default: 4.</summary>
    public int RequiredUniqueChars { get; init; } = 4;
}
