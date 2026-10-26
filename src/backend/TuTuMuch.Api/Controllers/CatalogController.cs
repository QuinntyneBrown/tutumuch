using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuTuMuch.Api.Auth;
using TuTuMuch.Api.Http;
using TuTuMuch.Application.Catalog;

namespace TuTuMuch.Api.Controllers;

/// <summary>The public catalog: anyone may browse it, signed in or not (L2-022).</summary>
[ApiController]
[AllowAnonymous]
public sealed class CatalogController(ISender sender) : ControllerBase
{
    /// <summary>
    /// A page of the catalog narrowed by keyword and filters (L2-023, L2-024). Values are the
    /// slugs of the page URL; repeat subcategory, brand and condition for more than one.
    /// </summary>
    [HttpGet("api/catalog/listings")]
    public async Task<IActionResult> Listings(
        [FromQuery] CatalogFilterParameters filters, int page = 1, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await sender.Send(new BrowseCatalogQuery(page, pageSize, filters.ToFilters()), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>How many listings the filters keep, for the filter panel's apply button.</summary>
    [HttpGet("api/catalog/listings/count")]
    public async Task<IActionResult> Count([FromQuery] CatalogFilterParameters filters, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await sender.Send(new CountCatalogListingsQuery(filters.ToFilters()), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>Brands of the visible catalog, most listed first.</summary>
    [HttpGet("api/catalog/brands")]
    public async Task<IActionResult> Brands(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCatalogBrandsQuery(), cancellationToken));

    /// <summary>
    /// One listing (L2-025). The session cookie is read when present so the seller is recognised;
    /// a missing or expired one means a visitor, never 401. The body depends on the viewer.
    /// </summary>
    [HttpGet("api/catalog/listings/{reference}")]
    public async Task<IActionResult> Listing(string reference, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        Guid? viewerId = User.Identity?.IsAuthenticated == true ? User.MemberId() : null;
        var result = await sender.Send(new GetListingDetailQuery(reference, viewerId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}

/// <summary>The filter query parameters of the catalog endpoints, bound as strings so a malformed value is a 400 with a TuTuMuch code.</summary>
public sealed class CatalogFilterParameters
{
    public string? Q { get; init; }

    public string? Category { get; init; }

    public string[]? Subcategory { get; init; }

    public string[]? Brand { get; init; }

    public string[]? Condition { get; init; }

    public string? Min { get; init; }

    public string? Max { get; init; }

    public string? Sort { get; init; }

    public CatalogFilters ToFilters() => new(Q, Category, Subcategory, Brand, Condition, Min, Max, Sort);
}
