using System.Globalization;
using System.Text;
using Bogus;

namespace TeamHub.DemoSeed;

/// <summary>
/// Deterministic demo identities shared by auth and organization seeders.
/// Username = Name+Surname+123; password and email local-part = lowercase(username).
/// </summary>
public static class DemoUserIdentityFactory
{
    public const int RandomizerSeed = 424242;
    public const string LoginSuffix = "123";
    public const int MaxUsernameLength = 30;

    public sealed record Identity(
        string Name,
        string Surname,
        string Username,
        string Password,
        string Email);

    public static IReadOnlyList<Identity> CreateMany(
        int count,
        string emailDomain,
        string? reservedUsername = null)
    {
        if (count <= 0)
            return [];

        Randomizer.Seed = new Random(RandomizerSeed);
        var polishFaker = new Faker("pl");
        var englishFaker = new Faker("en");
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(reservedUsername))
            used.Add(reservedUsername);

        var result = new List<Identity>(count);
        for (var i = 0; i < count; i++)
        {
            // Locale must not depend on total count so subsets stay aligned across services.
            var faker = i % 2 == 0 ? polishFaker : englishFaker;
            var name = faker.Name.FirstName();
            var surname = faker.Name.LastName();
            var username = AllocateUsername(ToAsciiAlpha(name) + ToAsciiAlpha(surname), used);
            var password = username.ToLowerInvariant();

            result.Add(new Identity(
                name,
                surname,
                username,
                password,
                $"{password}{emailDomain}"));
        }

        return result;
    }

    public static IReadOnlyList<string> CreateUsernames(
        int count,
        string? reservedUsername = null) =>
        CreateMany(count, emailDomain: "@unused.local", reservedUsername)
            .Select(i => i.Username)
            .ToList();

    static string AllocateUsername(string basePart, HashSet<string> used)
    {
        if (string.IsNullOrEmpty(basePart))
            basePart = "User";

        for (var collision = 0; ; collision++)
        {
            var suffix = collision == 0 ? LoginSuffix : $"{collision}{LoginSuffix}";
            var maxBase = MaxUsernameLength - suffix.Length;
            if (maxBase < 1)
                throw new InvalidOperationException("Demo username suffix exceeds max length.");

            var truncated = basePart.Length <= maxBase ? basePart : basePart[..maxBase];
            var username = truncated + suffix;
            if (used.Add(username))
                return username;
        }
    }

    static string ToAsciiAlpha(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(value.Length);
        foreach (var c in normalized)
        {
            var mapped = c switch
            {
                'ł' or 'Ł' => 'l',
                'ø' or 'Ø' => 'o',
                'đ' or 'Đ' => 'd',
                _ => c
            };

            if (CharUnicodeInfo.GetUnicodeCategory(mapped) == UnicodeCategory.NonSpacingMark)
                continue;

            if (mapped is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z'))
                sb.Append(mapped);
        }

        return sb.ToString();
    }
}
