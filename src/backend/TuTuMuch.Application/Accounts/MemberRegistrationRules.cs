using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Common;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Application.Accounts;

/// <summary>
/// The display name, age and terms rules of L2-001, shared by password registration and the
/// finish-joining step of a Google sign-in (L2-088) so the two never drift.
/// </summary>
public static partial class MemberRegistrationRules
{
    /// <summary>Adds a <c>displayName</c> error when the trimmed name breaks the format or is taken ignoring case.</summary>
    public static async Task ValidateDisplayNameAsync(ITuTuMuchDbContext db, string displayName, Dictionary<string, FieldError> errors, CancellationToken cancellationToken)
    {
        if (!DisplayNamePattern().IsMatch(displayName))
        {
            errors["displayName"] = new("DISPLAY_NAME_FORMAT", "Use 3 to 30 letters, numbers, _ or -");
        }
        else if (await db.Members.AnyAsync(m => m.NormalizedDisplayName == Member.Normalize(displayName), cancellationToken))
        {
            errors["displayName"] = new("DISPLAY_NAME_TAKEN", "That display name is taken");
        }
    }

    /// <summary>Adds <c>ageConfirmed</c> and <c>termsAccepted</c> errors for unticked boxes.</summary>
    public static void ValidateConsent(bool ageConfirmed, bool termsAccepted, Dictionary<string, FieldError> errors)
    {
        if (!ageConfirmed)
        {
            errors["ageConfirmed"] = new("AGE_REQUIRED", "Confirm you're 18 or older");
        }

        if (!termsAccepted)
        {
            errors["termsAccepted"] = new("TERMS_REQUIRED", "Accept the terms to continue");
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{3,30}$")]
    private static partial Regex DisplayNamePattern();
}
