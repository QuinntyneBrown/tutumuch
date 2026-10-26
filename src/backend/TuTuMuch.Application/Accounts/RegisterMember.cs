using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Common;
using TuTuMuch.Domain.Members;

namespace TuTuMuch.Application.Accounts;

/// <summary>Registers an unverified member and emails a verification link (L2-001, L2-002).</summary>
public sealed record RegisterMemberCommand(string? DisplayName, string? Email, string? Password, bool AgeConfirmed, bool TermsAccepted)
    : IRequest<Result<Guid>>;

public sealed partial class RegisterMemberCommandHandler(
    ITuTuMuchDbContext db,
    IPasswordHasher hasher,
    IBreachedPasswords breached,
    VerificationEmails verificationEmails,
    TimeProvider clock) : IRequestHandler<RegisterMemberCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterMemberCommand command, CancellationToken cancellationToken)
    {
        var displayName = command.DisplayName?.Trim() ?? string.Empty;
        var email = command.Email?.Trim() ?? string.Empty;
        var password = command.Password ?? string.Empty;
        var errors = new Dictionary<string, FieldError>();

        if (!DisplayNamePattern().IsMatch(displayName))
        {
            errors["displayName"] = new("DISPLAY_NAME_FORMAT", "Use 3 to 30 letters, numbers, _ or -");
        }
        else if (await db.Members.AnyAsync(m => m.NormalizedDisplayName == Member.Normalize(displayName), cancellationToken))
        {
            errors["displayName"] = new("DISPLAY_NAME_TAKEN", "That display name is taken");
        }

        if (email.Length > 254 || !EmailPattern().IsMatch(email))
        {
            errors["email"] = new("EMAIL_FORMAT", "Enter an email address like name@example.com");
        }
        else if (await db.Members.AnyAsync(m => m.NormalizedEmail == Member.Normalize(email), cancellationToken))
        {
            errors["email"] = new("EMAIL_TAKEN", "An account with this email already exists");
        }

        if (password.Length < 12)
        {
            errors["password"] = new("PASSWORD_TOO_SHORT", "Password must be at least 12 characters");
        }
        else if (password.Length > 128)
        {
            errors["password"] = new("PASSWORD_TOO_LONG", "Password must be 128 characters or fewer");
        }
        else if (breached.Contains(password))
        {
            errors["password"] = new("PASSWORD_BREACHED", "This password has appeared in a data breach. Choose another.");
        }

        if (!command.AgeConfirmed)
        {
            errors["ageConfirmed"] = new("AGE_REQUIRED", "Confirm you're 18 or older");
        }

        if (!command.TermsAccepted)
        {
            errors["termsAccepted"] = new("TERMS_REQUIRED", "Accept the terms to continue");
        }

        if (errors.Count > 0)
        {
            return Error.Invalid("REGISTRATION_INVALID", errors);
        }

        var member = Member.Register(displayName, email, hasher.Hash(password), clock.GetUtcNow().UtcDateTime);
        db.Members.Add(member);
        await db.SaveChangesAsync(cancellationToken);
        await verificationEmails.SendAsync(member, cancellationToken);
        return member.Id;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{3,30}$")]
    private static partial Regex DisplayNamePattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
