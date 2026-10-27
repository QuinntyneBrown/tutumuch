# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- The empty catalog and an empty My listings use the copy from the mocks ("Be the first to list a dress, a pair of sneakers or anything your daughter has outgrown." and "List a dress, a leotard or anything she’s outgrown.") instead of the 12L sizes left over from the clone.
- Every page, mock and design-system page follows `docs/page-guidelines.md` (`L1-022`, `L2-096`, ADR general/0009): the brand-kit palette, Fraunces and DM Sans (self-hosted) with Titan One only in the logo, pill buttons that hover to ink, no card shadows or gradients, the two-row header with the categories in guideline order and the mark under 640px, listing cards with aligned price rows, condition badges, blush skeletons, Tilly in the marketing hero, sport headers, empty states and the "It's live" confirmation, "More filters" for Brand and Price, and the standard terms ("Shop the closet", "Sell outgrown gear"). New `tl-mark`, `tl-tilly`, `tl-condition-badge` and `tl-skeleton`; `tl-shapes` is retired. A Playwright guideline check covers every routed page.
- Simpler pitch video: the poster button reads "Play", the dialog has no visible heading, captions stay off until turned on, and the transcript link and file are gone (`L2-080` to `L2-082`). The seller band drops "10% fee, only when it sells".

### Added

- Sign in with Google: "Continue with Google" on `/sign-in` and `/join` of the full application, through a server-side OpenID Connect code flow with PKCE; a linked member or a verified member with the same email signs in and returns to `returnUrl`, a new person finishes joining at `/join/google` with a display name, the age confirmation and the terms (no password, no verification email), an unverified matching email is refused, and nothing shows without a Google client. New provider button and "or" divider in the design system and `@tutumuch/components`; `ExternalLogins` and `PendingExternalJoins` tables, nullable `PasswordHash`; owner steps in `docs/deployment.md` (`L2-088`, ADR accounts/0001).
- Launch phases chosen by `Launch__Phase` (default 1): Phase 1 lets only the owner (an `Administrator`) sell, hides the category menus and keeps fewer than 40 listings; Phase 2 shows every menu with owner-only selling; Phase 3 opens selling to every verified member. `GET /api/launch` tells the web app, which hides seller entry points accordingly; the API answers 403 `SELLING_NOT_OPEN` and 409 `PRODUCT_LIMIT_REACHED`. Switching needs only the setting and a restart (`L2-089` to `L2-095`, ADR platform/0014).
- UAT host: every push to `main` also serves the full application at `https://uat.tutumuch.kids`, from the same `tutumuch-web` image, Caddy and API as the marketing page, with no extra VM or environment; the deploy sets `SITE_ADDRESS` on the VM from `SITE_HOST` and smoke-tests the UAT host (`L2-087`, ADR platform/0013).
- Link-preview tags on the marketing page: meta description, Open Graph and Twitter card tags in the served HTML with a 1200×630 share image (`/media/tutumuch-share.jpg`), so a shared link shows the poster (`L2-083`).
- Continuous deployment: every push to `main` deploys to one low-cost Azure Linux VM once CI's `verify` passes (`workflow_run`), migrates before the restart (`docker compose run --rm -T migrator < /dev/null`, so the restart now runs), fails the run on a bad `/api/health` smoke test, and can be re-run by hand for an older SHA to roll back. The compose stack has memory limits and rotated logs for a 1 to 2 GB VM, Caddy sends `Cache-Control: no-cache` for `index.html` and SPA routes, and only the build job may write packages (`L2-084` to `L2-086`, ADR platform/0012).
- Marketing page at `/welcome` (`@tutumuch/marketing`) with a 30-second pitch video (captions, transcript; an interim silent build until the Kokoro-82M narration and the Pixabay _Upbeat Ukulele Kids_ music are added), new design-system components (marketing hero, shapes, feature band, steps, video poster, video player), an "About TuTuMuch" footer link; the copy says everything is for girls (`L2-078` to `L2-083`, ADR platform/0011, ADR general/0005).
- `TuTuMuch.Cli`, a developer tool: `reset` drops and re-migrates a local database and empties the photo store (Development or `--force` only); `seed` loads 8 verified demo members and 48 listings with real CC BY 2.0 product photos through the application commands; `seed --reset` gives the same fresh demo (`L2-072` to `L2-077`, ADR platform/0010).
- HTML mocks of every page, dialog and notification in every state, light and dark, responsive, in `docs/mocks/` (235 mocks across 56 screens), styled from the TuTuMuch logo.
- L1 high-level and L2 detailed requirements with acceptance criteria in `docs/specs/`.
- Documented the technology stack: Angular frontend, .NET backend, full-stack responsive web application.
- Repository scaffolding: README, contributing guide, code of conduct, security policy, support policy, MIT license, and issue and pull request templates.
- Agent skills in `.claude/skills` and `.agents/skills`.
- Blank `AGENTS.md`, `CLAUDE.md`, `GEMINI.md` and `.github/copilot-instructions.md`.
