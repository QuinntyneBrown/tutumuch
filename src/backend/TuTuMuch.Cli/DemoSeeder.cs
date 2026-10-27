using System.Text.Json;
using System.Web;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TuTuMuch.Application.Accounts;
using TuTuMuch.Application.Common;
using TuTuMuch.Application.Listings;
using TuTuMuch.Domain.Listings;
using TuTuMuch.Domain.Members;
using TuTuMuch.Domain.Notifications;
using TuTuMuch.Infrastructure.Persistence;

namespace TuTuMuch.Cli;

/// <summary>
/// Loads the demo catalogue through the same MediatR commands as the API, so every account,
/// listing and photo rule applies (L2-074, L2-075). Runs once: an existing demo is left alone (L2-076).
/// </summary>
public sealed class DemoSeeder(ISender sender, TuTuMuchDbContext db, SeedClock clock, IConfiguration configuration)
{
    /// <summary>The demo members register this long before the seed, before their first listing.</summary>
    public static readonly TimeSpan MembersJoinedBefore = TimeSpan.FromDays(31);

    /// <summary>The listings are published over this many days before the seed, oldest first.</summary>
    public static readonly TimeSpan ListingsSpread = TimeSpan.FromDays(30);

    /// <summary>The launch phase the demo targets: every member sells (L2-095).</summary>
    public const Domain.Launch.LaunchPhase LaunchPhase = Domain.Launch.LaunchPhase.Three;

    private const string SuppressionReason = "Demo member verified by TuTuMuch.Cli";

    /// <summary>Where the seed photos are: Seed:PhotosPath, or SeedPhotos next to the tool.</summary>
    public string PhotosPath => configuration["Seed:PhotosPath"] is { Length: > 0 } path ? path : Path.Combine(AppContext.BaseDirectory, "SeedPhotos");

    /// <summary>The first seed photo that is not on disk, or null when every one is (L2-077).</summary>
    public string? FindMissingPhoto() =>
        DemoCatalogue.PhotoFiles.Select(f => Path.Combine(PhotosPath, f)).FirstOrDefault(p => !File.Exists(p));

    public async Task SeedAsync(TextWriter output, CancellationToken cancellationToken)
    {
        var marker = Member.Normalize(DemoCatalogue.Members[0].Email);
        if (await db.Members.AnyAsync(m => m.NormalizedEmail == marker, cancellationToken))
        {
            output.WriteLine("Demo data already exists. Run seed --reset for a fresh demo.");
            return;
        }

        var start = clock.GetUtcNow();
        try
        {
            clock.Set(start - MembersJoinedBefore);
            var memberIds = new Dictionary<DemoMember, Guid>();
            foreach (var member in DemoCatalogue.Members)
            {
                memberIds[member] = await AddMemberAsync(member, cancellationToken);
            }

            // Spread the publishes so no seller publishes twice on one day: the daily limit
            // (Selling:PublishesPerDay, L2-051) stays in force and never blocks the seed.
            var spacing = ListingsSpread / DemoCatalogue.Listings.Count;
            for (var i = 0; i < DemoCatalogue.Listings.Count; i++)
            {
                clock.Set(start - ListingsSpread + (spacing * i));
                await AddListingAsync(memberIds[DemoCatalogue.SellerOf(i)], DemoCatalogue.Listings[i], cancellationToken);
            }
        }
        finally
        {
            clock.Release();
        }

        output.WriteLine($"Seeded {DemoCatalogue.Members.Count} members and {DemoCatalogue.Listings.Count} listings.");
        output.WriteLine($"Demo data targets launch phase {LaunchPhase:D}.");
    }

    /// <summary>Registers the member, verifies them with the link from their queued email, then suppresses that email.</summary>
    private async Task<Guid> AddMemberAsync(DemoMember member, CancellationToken cancellationToken)
    {
        var memberId = Expect(
            await sender.Send(new RegisterMemberCommand(member.DisplayName, member.Email, DemoCatalogue.Password, AgeConfirmed: true, TermsAccepted: true), cancellationToken),
            $"Registering {member.Email}");
        var email = await db.OutboxMessages.SingleAsync(
            m => m.RecipientMemberId == memberId && m.NotificationType == NotificationType.EmailVerification && m.Status == OutboxMessageStatus.Pending,
            cancellationToken);
        Expect(await sender.Send(new VerifyEmailCommand(TokenFrom(email.Payload)), cancellationToken), $"Verifying {member.Email}");

        var now = clock.GetUtcNow().UtcDateTime;
        email.Claim(now);
        email.Suppress(SuppressionReason);
        await db.SaveChangesAsync(cancellationToken);
        return memberId;
    }

    /// <summary>
    /// Uploads the listing's photos through the photo pipeline (L2-017), creates the draft with
    /// them and publishes it with the size confirmed, as a seller would on the sell form.
    /// </summary>
    private async Task AddListingAsync(Guid sellerId, DemoListing listing, CancellationToken cancellationToken)
    {
        var photoIds = new List<Guid>();
        foreach (var file in listing.Photos)
        {
            var content = await File.ReadAllBytesAsync(Path.Combine(PhotosPath, file), cancellationToken);
            photoIds.Add(Expect(await sender.Send(new UploadListingPhotoCommand(sellerId, content), cancellationToken), $"Uploading {file}").Id);
        }

        var details = new ListingDetails(
            listing.Category, listing.Subcategory, Size: null, listing.Title, listing.Description, listing.Brand,
            listing.Condition, listing.Flaws, listing.Price, listing.ShippingCost);
        var draft = Expect(await sender.Send(new CreateListingDraftCommand(sellerId, details, photoIds), cancellationToken), $"Creating '{listing.Title}'");
        Expect(await sender.Send(new PublishListingCommand(sellerId, draft.Reference), cancellationToken), $"Publishing '{listing.Title}'");
    }

    /// <summary>The raw token in the verification link of an EmailVerificationEmail payload.</summary>
    private static string TokenFrom(string payload)
    {
        using var json = JsonDocument.Parse(payload);
        var link = new Uri(json.RootElement.GetProperty("link").GetString()!);
        return HttpUtility.ParseQueryString(link.Query)["token"]
            ?? throw new InvalidOperationException("The verification email has no token.");
    }

    /// <summary>The value of a successful result; a refused command stops the seed with its code.</summary>
    private static T Expect<T>(Result<T> result, string step) =>
        result.IsSuccess ? result.Value! : throw new InvalidOperationException($"{step}: {result.Error!.Code} {result.Error.Title}");
}
