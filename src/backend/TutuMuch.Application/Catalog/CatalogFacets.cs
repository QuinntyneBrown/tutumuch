using MediatR;
using Microsoft.EntityFrameworkCore;
using TutuMuch.Application.Abstractions;
using TutuMuch.Application.Common;
using TutuMuch.Domain.Listings;

namespace TutuMuch.Application.Catalog;

public sealed record CatalogCountDto(int TotalCount);

/// <summary>One brand of the visible catalog and how many listings carry it.</summary>
public sealed record CatalogBrandDto(string Name, int ListingCount);

/// <summary>How many listings the filters keep; the filter panel's "Show {n} items" (L2-024).</summary>
public sealed record CountCatalogListingsQuery(CatalogFilters Filters) : IRequest<Result<CatalogCountDto>>;

public sealed class CountCatalogListingsQueryHandler(ITutuMuchDbContext db) :
    IRequestHandler<CountCatalogListingsQuery, Result<CatalogCountDto>>
{
    public async Task<Result<CatalogCountDto>> Handle(CountCatalogListingsQuery query, CancellationToken cancellationToken)
    {
        var criteria = CatalogCriteria.From(query.Filters);
        if (!criteria.IsSuccess)
        {
            return criteria.Error!;
        }

        return new CatalogCountDto(await criteria.Value!.Apply(db.Listings.AsNoTracking()).CountAsync(cancellationToken));
    }
}

/// <summary>Brands of Active and Reserved listings, most listed first, then by name (L2-024).</summary>
public sealed record GetCatalogBrandsQuery : IRequest<IReadOnlyList<CatalogBrandDto>>;

public sealed class GetCatalogBrandsQueryHandler(ITutuMuchDbContext db) :
    IRequestHandler<GetCatalogBrandsQuery, IReadOnlyList<CatalogBrandDto>>
{
    public async Task<IReadOnlyList<CatalogBrandDto>> Handle(GetCatalogBrandsQuery query, CancellationToken cancellationToken) =>
        await db.Listings.AsNoTracking()
            .Where(CatalogVisibility.IsVisible)
            .Where(l => l.Brand != null)
            .GroupBy(l => l.Brand!)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(b => b.Count).ThenBy(b => b.Name)
            .Select(b => new CatalogBrandDto(b.Name, b.Count))
            .ToListAsync(cancellationToken);
}
