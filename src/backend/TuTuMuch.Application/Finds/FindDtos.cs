namespace TuTuMuch.Application.Finds;

/// <summary>One card on Shelf finds (L2-114). Amounts are "0.00" strings; GrabAvailable means a grab is offered and none is open.</summary>
public sealed record FindCardDto(
    string Reference,
    string Title,
    string Category,
    string ShelfPrice,
    string StoreName,
    DateOnly SeenOn,
    bool GrabAvailable);

public sealed record FindPageDto(IReadOnlyList<FindCardDto> Items, int Page, int PageSize, int TotalCount);

/// <summary>
/// One find (L2-115). Status is Active, Gone or Expired; GrabOpen says only that a grab is open,
/// never who asked (L2-121). Mine is true for the poster.
/// </summary>
public sealed record FindDetailDto(
    string Reference,
    string Title,
    string Category,
    string ShelfPrice,
    string StoreName,
    string StoreAddress,
    DateOnly SeenOn,
    DateOnly ExpiresOn,
    string? Note,
    string Status,
    string PosterDisplayName,
    string? GrabFee,
    bool GrabOpen,
    bool Mine);
