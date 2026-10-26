using System.Text.RegularExpressions;

namespace TutuMuch.Application.Notifications;

/// <summary>
/// The last check before an email leaves Tutu Much (L2-042 AC3): refuses content with a 13- to 19-digit
/// number that passes the Luhn check, or a password or security code label followed by a value.
/// IDs (GUIDs) and the identifiers the template names, such as a tracking number, are skipped, so
/// their digits cannot pass for a card number.
/// </summary>
public static partial class EmailContentGuard
{
    /// <summary>Why the content is refused, or null when it is safe to send.</summary>
    public static string? Check(string content, IEnumerable<string>? identifiers = null)
    {
        foreach (var identifier in identifiers ?? [])
        {
            if (identifier.Length > 0)
            {
                content = content.Replace(identifier, " ", StringComparison.Ordinal);
            }
        }

        content = Guid().Replace(content, " ");
        foreach (Match match in Digits().Matches(content))
        {
            var digits = match.Value.Where(char.IsAsciiDigit).ToArray();
            if (digits.Length is >= 13 and <= 19 && PassesLuhn(digits))
            {
                return "card_number";
            }
        }

        if (Password().IsMatch(content))
        {
            return "password";
        }

        return SecurityCode().IsMatch(content) ? "security_code" : null;
    }

    private static bool PassesLuhn(char[] digits)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            var digit = digits[digits.Length - 1 - i] - '0';
            if (i % 2 == 1)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
        }

        return sum % 10 == 0;
    }

    // Runs of digits that may be split by single spaces or dashes, as card numbers are written.
    [GeneratedRegex(@"(?<![\d])\d(?:[ -]?\d){12,18}(?![\d])")]
    private static partial Regex Digits();

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex Guid();

    [GeneratedRegex(@"password\s*[:=]\s*\S", RegexOptions.IgnoreCase)]
    private static partial Regex Password();

    [GeneratedRegex(@"\b(?:CVV|CVC|CVV2|CVC2|security code)\b\s*[:=]?\s*\d{3,4}\b", RegexOptions.IgnoreCase)]
    private static partial Regex SecurityCode();
}
