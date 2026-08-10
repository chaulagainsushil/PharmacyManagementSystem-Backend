using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;

namespace PMS.Application.Application.Validators;

/// <summary>
/// Custom ASP.NET Identity password validator that rejects commonly used or trivially weak passwords.
/// Works alongside the structural rules (length, uppercase, digit, etc.) already configured in
/// Identity options — this validator adds a semantic "dictionary" check on top.
///
/// Detection strategy (NIST SP 800-63B):
///   1. Direct match against the common-password set (case-insensitive).
///   2. Leet-speak / symbol substitution normalisation, then re-check.
///   3. Root-word extraction (strip all digits and symbols) — catches "Admin@1234", "P@ssw0rd!99".
///   4. Email-prefix containment check.
/// </summary>
public sealed class WeakPasswordValidator : IPasswordValidator<IdentityUser>
{
    // ── Common password dictionary ────────────────────────────────────────────
    // Case-insensitive comparison is used at every check.
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Variants of "password"
        "password", "password1", "password12", "password123", "password1234",
        "password1!", "password!1", "password@1", "password@123", "password@1234",
        // Variants of "pass"
        "pass1234", "pass@1234", "pass@123", "passw0rd",
        // Variants of "admin"
        "admin", "admin1", "admin12", "admin123", "admin1234", "admin12345",
        "admin@1", "admin@12", "admin@123", "admin@1234", "admin@12345",
        "admin!1", "admin!123",
        // Variants of "p@ssword" / "p@ss"
        "p@ssword", "p@ssw0rd", "pa$$word", "pa$$w0rd", "p@ss123", "p@ss@123",
        // Sequential numbers
        "123456", "1234567", "12345678", "123456789", "1234567890",
        "654321", "987654321", "111111", "111111!", "000000", "123123", "321321",
        // Keyboard patterns
        "qwerty", "qwerty1", "qwerty12", "qwerty123", "qwerty@123", "qwerty1234",
        "1q2w3e4r", "1qaz2wsx", "1qaz@wsx",
        "zxcvbnm", "asdfgh", "asdfghjkl",
        // Common words
        "abc123", "abc@123", "abcdef", "abcdefg",
        "letmein", "letmein1", "letmein!",
        "welcome", "welcome1", "welcome123", "welcome@1",
        "monkey", "monkey1", "monkey123",
        "dragon", "dragon1",
        "master", "master1", "master123",
        "login", "login123",
        "superman", "batman",
        "iloveyou", "iloveyou1",
        "sunshine", "sunshine1",
        "princess", "princess1",
        "football", "baseball", "soccer", "hockey",
        "shadow", "shadow1", "shadow123",
        "trustno1", "whatever",
        "changeme", "changeme1", "changeit",
        "test", "test1", "test12", "test123", "test@123", "testing", "testing123",
        "hello", "hello1", "hello123", "hello@123",
        "user", "user1", "user123", "user@123",
        // App-specific weak passwords
        "pharma", "pharma1", "pharma123", "pharma@123",
        "pharmacy", "pharmacy1", "pharmacy@1",
        "pms", "pms1", "pms123", "pms@123",
    };

    // ── Root words to reject regardless of appended digits/symbols ────────────
    // If stripping all non-alpha characters from the password yields one of these words
    // the password is considered weak (catches "Admin@1234", "Welcome@99!", "Test@2026").
    private static readonly HashSet<string> WeakRootWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "pass", "admin", "user", "login", "welcome", "letmein",
        "master", "dragon", "monkey", "shadow", "sunshine", "princess",
        "football", "baseball", "soccer", "hockey", "superman", "batman",
        "iloveyou", "trustno", "qwerty", "test", "hello", "changeme",
        "pharma", "pharmacy", "pms",
    };

    // ── Leet-speak substitution map ───────────────────────────────────────────
    private static readonly (string From, string To)[] LeetMap =
    [
        ("@", "a"), ("$", "s"), ("0", "o"), ("1", "i"),
        ("3", "e"), ("5", "s"), ("7", "t"), ("!", "i"),
        ("+", "t"), ("#", "h"),
    ];

    public Task<IdentityResult> ValidateAsync(
        UserManager<IdentityUser> manager,
        IdentityUser user,
        string? password)
    {
        if (string.IsNullOrEmpty(password))
            return Task.FromResult(IdentityResult.Success);

        // ── Check 1: direct dictionary hit (case-insensitive) ─────────────────
        if (CommonPasswords.Contains(password))
            return Fail();

        // ── Check 2: leet-speak normalisation ─────────────────────────────────
        var leetNormalised = ApplyLeetMap(password).ToLowerInvariant();
        if (CommonPasswords.Contains(leetNormalised))
            return Fail();

        // ── Check 3: root-word extraction ─────────────────────────────────────
        // Strip everything that isn't a letter, then check against weak root words.
        // "Admin@1234"  → root "Admin"  → weak
        // "Welcome@99!" → root "Welcome" → weak
        // "Secure@Pass1" → root "SecurePass" → not in list → allowed
        var root = Regex.Replace(password, @"[^a-zA-Z]", string.Empty);
        if (!string.IsNullOrEmpty(root) && WeakRootWords.Contains(root))
            return Fail();

        // Also apply leet normalisation to the root
        var rootLeet = ApplyLeetMap(root).ToLowerInvariant();
        if (!string.IsNullOrEmpty(rootLeet) && WeakRootWords.Contains(rootLeet))
            return Fail();

        // ── Check 4: email-prefix containment ────────────────────────────────
        var emailLocal = user.Email?.Split('@').FirstOrDefault()?.ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(emailLocal) && emailLocal.Length >= 3
            && password.Contains(emailLocal, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code        = "PasswordContainsUsername",
                Description = "Password must not contain your email address or username."
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ApplyLeetMap(string input)
    {
        foreach (var (from, to) in LeetMap)
            input = input.Replace(from, to, StringComparison.Ordinal);
        return input;
    }

    private static Task<IdentityResult> Fail() =>
        Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code        = "WeakPassword",
            Description = "This password is too common or easily guessable. Please choose a stronger password."
        }));
}
