# ADR-0009: SkiaSharp for Photo Processing

**Date:** 2026-10-25
**Category:** platform
**Status:** Accepted
**Deciders:** Quinntyne Brown (product owner)

## Context

Listing photos must be decoded, turned upright, stripped of every metadata block and
re-encoded as WebP and JPEG at 400, 800 and 1600 px (`L2-017`, `L2-059`). The detailed
design proposed SixLabors ImageSharp and left the library and its licence `<TO SUPPLY>`.
Checked on 2026-10-25: ImageSharp 3.1.12, the last 3.x release, restores with known high
severity advisories, which fail the build under `TreatWarningsAsErrors`. ImageSharp 4.x runs a
licence-key validation task at build time and is under the Six Labors Split License.

## Decision

Use SkiaSharp 4.153.1 (MIT) in `TutuMuch.Infrastructure`, with
`SkiaSharp.NativeAssets.Linux.NoDependencies` so CI on ubuntu-latest needs no system
packages. `SkiaImageInspector` sniffs magic bytes and reads dimensions from the header;
`SkiaPhotoProcessor` decodes to sRGB, applies the EXIF orientation, draws onto an opaque
bitmap without a colour space and encodes WebP at quality 80 and JPEG at quality 82. Only
pixels are encoded, so no EXIF, XMP, IPTC, GPS or ICC block reaches a stored version.

## Options Considered

### Option 1: SkiaSharp (chosen)
- **Pros:** MIT licence; no advisories; WebP and JPEG encoders built in; native assets for
  Windows, macOS and Linux as packages.
- **Cons:** Native binaries in the deployment; lower-level API.

### Option 2: ImageSharp 3.1.12
- **Pros:** Fully managed.
- **Cons:** Known high severity advisories; restore fails the build.

### Option 3: ImageSharp 4.x
- **Pros:** Fully managed, maintained.
- **Cons:** Build-time licence key; commercial licence above 1M USD revenue.

## Consequences

### Positive
- Photo processing builds and runs on every developer machine and in CI without keys.

### Negative
- A SkiaSharp upgrade must keep its native assets package on the same version.

### Risks
- A decoder vulnerability in Skia. Mitigation: NuGet audit stays on as an error, and the
  10 MB and 50 megapixel limits run before decoding.

## References

- [upload-listing-photos](../../detailed-designs/listings/upload-listing-photos/README.md)
- [ADR listings/0001](../listings/0001-process-photos-in-the-upload-request.md)
