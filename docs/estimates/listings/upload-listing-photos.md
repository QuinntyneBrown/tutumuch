# Upload listing photos — estimate

## Basis

The estimate assumes an intermediate Angular/TypeScript and ASP.NET Core/C# developer who is productive in both stacks but new to this codebase and to image processing in .NET. It assumes the baseline outbox, `TuTuMuch Worker` host, object storage account, CDN and `Listings` table exist, and that SixLabors ImageSharp is approved (`<TO SUPPLY>`; a different library could add hours). The open configuration values (retention, schedule, quality, pixel limit) do not change the hours. Hours cover implementation, unit and acceptance tests, and review fixes. [Detailed design](../../detailed-designs/listings/upload-listing-photos/README.md)

## Full design

| Work item | Hours |
|-----------|------:|
| `ListingPhoto` entity, `ListingPhotoVersion` value object, `ListingPhotos` and `ListingPhotoVersions` tables and migration | 4 |
| `ImageSharpInspector`: magic-byte type detection, dimension read, pixel-count guard | 4 |
| `UploadListingPhotoCommandHandler` with 413, 415 and 422 rules, original storage and outbox message | 5 |
| `PhotoUploadsController` with multipart upload, request size limit and status endpoint | 3 |
| `ObjectStoragePhotoStorage` adapter for private originals and public, immutable versions | 4 |
| `ImageSharpPhotoProcessor`: orientation, metadata stripping, 3 widths in WebP and JPEG, content-addressed keys | 8 |
| `PhotoProcessingJob` with retry, back-off and `Failed` handling | 4 |
| `ListingPhotoAttacher`: 8-photo limit, ownership, ordering, detaching | 4 |
| `PhotoCleanupJob` for unattached uploads and unreferenced versions | 4 |
| `PhotoUploaderComponent`: tiles, cover badge, remove, rejected tile, add tile with "n of 8", client checks | 8 |
| Drag-and-drop and keyboard reordering with live-region announcements | 5 |
| `PhotoUploadService` with progress, parallel limit and status polling | 3 |
| Acceptance tests for `L2-017` (size, type by content, minimum size, metadata removed, ninth photo, cover order) and cache header of `L2-059` | 8 |
| **Total** | **64** |

## Minimal scope

The minimal version processes photos synchronously in `TuTuMuch API` instead of the worker. It includes content sniffing, the size and dimension checks, metadata stripping, 3 widths in WebP and JPEG under content-addressed keys with the one-year cache header, the attacher with the 8-photo limit and ordering, and an uploader with add, remove and drag reordering. It leaves out `PhotoProcessingJob` and the outbox, status polling, the keyboard reorder alternative and `PhotoCleanupJob`. It still satisfies `L2-017` and the content-addressing and caching parts of `L2-059`. It weakens keyboard access to reordering (`L2-065`) and leaves orphaned uploads in storage. Minimal scope totals **44** hours.

## Partial descoping

| Descope | What is lost | Hours saved |
|---------|--------------|------------:|
| `PhotoCleanupJob` | Unattached uploads and unreferenced versions accumulate in storage | 4 |
| Synchronous processing instead of `PhotoProcessingJob` and polling | Upload requests take longer and block an API thread during re-encoding | 5 |
| JPEG fallback versions; WebP only | Browsers without WebP support show no photo (`L2-059`) | 2 |
| Keyboard reorder alternative | Reordering needs a pointer (`L2-065` risk) | 3 |
| Parallel upload limit and progress events | Multi-file uploads run one at a time without progress | 1 |
