namespace TutuMuch.Domain.Orders;

/// <summary>What the buyer says is wrong with a Shipped or Delivered order (L2-036).</summary>
public enum ProblemReason
{
    NotReceived,
    NotAsDescribed,
    WrongSize,
    Counterfeit,
}

public enum ProblemStatus
{
    Open,
    Resolved,
}

/// <summary>
/// An administrator's decision. A partial refund is deferred: L2-032 has no status for it and the
/// mocks have no control for it (ADR orders/0007).
/// </summary>
public enum ProblemOutcome
{
    FullRefund,
    ReleaseToSeller,
}

public static class ProblemReasons
{
    public static string ToLabel(this ProblemReason reason) => reason switch
    {
        ProblemReason.NotReceived => "Not received",
        ProblemReason.NotAsDescribed => "Not as described",
        ProblemReason.WrongSize => "Wrong size",
        _ => "Counterfeit",
    };
}

/// <summary>One private evidence photo of a problem, stored under problems/{problemId}/{photoId}.jpg.</summary>
public sealed class ProblemPhoto
{
    private ProblemPhoto()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProblemId { get; private set; }

    public int Position { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public static string KeyFor(Guid problemId, Guid photoId) => $"problems/{problemId}/{photoId}.jpg";

    internal static ProblemPhoto Create(Guid id, Guid problemId, int position) => new()
    {
        Id = id,
        ProblemId = problemId,
        Position = position,
        StorageKey = KeyFor(problemId, id),
        ContentType = "image/jpeg",
    };
}

/// <summary>
/// The buyer's claim on one order (L2-036), referenced as P-1001 and up. The seller replies once
/// while it is Open; an administrator resolves it once. An order has at most one problem.
/// </summary>
public sealed class OrderProblem
{
    /// <summary>The first problem number; references start at P-1001.</summary>
    public const long FirstNumber = 1001;

    public const int MaxPhotos = 4;

    /// <summary>The longest description, seller reply or resolution reason.</summary>
    public const int MaxText = 1000;

    private readonly List<ProblemPhoto> _photos = [];

    private OrderProblem()
    {
    }

    public Guid Id { get; private set; }

    public long Number { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public Guid OrderId { get; private set; }

    public ProblemReason Reason { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public IReadOnlyList<ProblemPhoto> Photos => _photos;

    public ProblemStatus Status { get; private set; }

    public DateTime OpenedAt { get; private set; }

    public string? SellerResponse { get; private set; }

    public DateTime? SellerRespondedAt { get; private set; }

    public ProblemOutcome? Outcome { get; private set; }

    public decimal? RefundAmount { get; private set; }

    public string? ResolutionReason { get; private set; }

    /// <summary>The administrator who decided.</summary>
    public Guid? ResolvedBy { get; private set; }

    public DateTime? ResolvedAt { get; private set; }

    public static string ReferenceFor(long number) => $"P-{number}";

    /// <summary>Opens a problem on an order the buyer may still report on, with photo IDs in upload order.</summary>
    public static OrderProblem Open(long number, Order order, ProblemReason reason, string description, IReadOnlyList<Guid> photoIds, DateTime now)
    {
        if (!order.CanReportProblem(now))
        {
            throw new InvalidOperationException("The problem window of this order is closed.");
        }

        if (photoIds.Count > MaxPhotos)
        {
            throw new ArgumentException($"A problem has at most {MaxPhotos} photos.", nameof(photoIds));
        }

        var problem = new OrderProblem
        {
            Id = Guid.NewGuid(),
            Number = number,
            Reference = ReferenceFor(number),
            OrderId = order.Id,
            Reason = reason,
            Description = description,
            Status = ProblemStatus.Open,
            OpenedAt = now,
        };
        problem._photos.AddRange(photoIds.Select((id, position) => ProblemPhoto.Create(id, problem.Id, position)));
        return problem;
    }

    public bool CanSellerRespond => Status == ProblemStatus.Open && SellerResponse is null;

    /// <summary>The seller's one reply, while the problem is Open (L2-036 AC5).</summary>
    public void RecordSellerResponse(string message, DateTime now)
    {
        if (!CanSellerRespond)
        {
            throw new InvalidOperationException("The seller cannot reply to this problem.");
        }

        SellerResponse = message;
        SellerRespondedAt = now;
    }

    /// <summary>The administrator's one decision (L2-036 AC3, AC4).</summary>
    public void Resolve(ProblemOutcome outcome, decimal? refundAmount, string reason, Guid administratorId, DateTime now)
    {
        if (Status != ProblemStatus.Open)
        {
            throw new InvalidOperationException("The problem is already resolved.");
        }

        Status = ProblemStatus.Resolved;
        Outcome = outcome;
        RefundAmount = refundAmount;
        ResolutionReason = reason;
        ResolvedBy = administratorId;
        ResolvedAt = now;
    }
}
