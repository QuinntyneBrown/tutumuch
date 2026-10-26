using MediatR;
using Microsoft.Extensions.Options;
using TuTuMuch.Domain.Listings;

namespace TuTuMuch.Application.Listings;

/// <summary>Selling settings: the platform fee shown before publishing (L2-031) and suggested brands.</summary>
public sealed class SellingOptions
{
    public decimal PlatformFeePercent { get; set; } = 10m;

    public List<string> SuggestedBrands { get; set; } = [];

    /// <summary>Publishes allowed per member per UTC day (L2-051).</summary>
    public int PublishesPerDay { get; set; } = 20;
}

public sealed record CategoryDto(string Name, IReadOnlyList<string> Subcategories, string AcceptedSize, string SizeLabel);

/// <summary>The L2-011 table for the sell form and the rules page.</summary>
public sealed record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;

public sealed class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CategoryDto>>(
            CategoryCatalog.All.Select(c => new CategoryDto(c.Name, c.Subcategories, c.AcceptedSize, c.SizeLabel)).ToList());
}

public sealed record SellingOptionsDto(decimal FeePercent, IReadOnlyList<string> Conditions, IReadOnlyList<string> SuggestedBrands, int MaxPhotos);

public sealed record GetSellingOptionsQuery : IRequest<SellingOptionsDto>;

public sealed class GetSellingOptionsQueryHandler(IOptions<SellingOptions> options) : IRequestHandler<GetSellingOptionsQuery, SellingOptionsDto>
{
    public const int MaxPhotos = 8;

    public Task<SellingOptionsDto> Handle(GetSellingOptionsQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(new SellingOptionsDto(options.Value.PlatformFeePercent, ItemConditions.All, options.Value.SuggestedBrands, MaxPhotos));
}
