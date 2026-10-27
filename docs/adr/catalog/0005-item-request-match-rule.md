# 0005. Item request match rule: category, highest price, lowest condition and every keyword word

Date: 2026-10-27

## Status

Accepted

## Context

An item request names a category, keywords and, optionally, a highest price and a lowest condition (`L2-097`). `L2-100` asks for matching by category, size, price and keywords, reusing the case-insensitive keyword rule of catalog search ([ADR catalog/0003](0003-keyword-search-by-case-insensitive-match.md)), and never against the member's own listings. Every listing has the one size Girls' (`L2-013`), so size cannot narrow a match. A request such as "ballet slippers, size 12" carries a shoe size that the size rule does not model.

## Decision

A request matches a listing when all of these hold:

- the listing's category equals the request's category (ignoring case);
- the request has no highest price, or the listing price is at or below it; shipping is not counted;
- the request has no lowest condition, or the listing's condition is that condition or better, in the order New with tags, New without tags, Used - Excellent, Used - Good, Used - Fair;
- every keyword word, after trimming, collapsing whitespace and lower-casing, appears inside the listing's lower-cased title, brand or description, as catalog search matches words;
- the listing's seller is not the request's member, and the member's account is not closed (`L2-106`).

The size is stored on the request from the category, like a listing's, and is not compared. A size number such as "12" is an ordinary keyword. Keywords are 3 to 100 characters, the length of a catalog search term. Matching evaluates in memory over the open requests of the listing's category, in `ItemRequest.Matches`, so the domain owns the rule and tests it without a database.

## Consequences

- A request matches exactly the listings a search for its keywords, filtered by the same category, price and condition, would show, so members can predict it; the form's help says every word must be in the listing.
- Words match inside other words ("slipper" matches "slippers") as in search; an over-specific request ("size 12 pink satin") matches little, and the member can edit it.
- No subcategory, brand or multiple-condition filter is offered on a request. They can be added later as optional fields without changing stored requests.
