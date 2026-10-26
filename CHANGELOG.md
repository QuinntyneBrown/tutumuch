# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Marketing page at `/welcome` (`@tutumuch/marketing`) with a 30-second pitch video (captions, transcript; an interim silent build until the Kokoro-82M narration and the Pixabay _Upbeat Ukulele Kids_ music are added), new design-system components (marketing hero, shapes, feature band, steps, video poster, video player), an "About TuTuMuch" footer link and the manual-only deploy workflow described in `docs/deployment.md`; the copy says everything is for girls (`L2-078` to `L2-083`, ADR platform/0011, ADR general/0005).
- `TuTuMuch.Cli`, a developer tool: `reset` drops and re-migrates a local database and empties the photo store (Development or `--force` only); `seed` loads 8 verified demo members and 48 listings with real CC BY 2.0 product photos through the application commands; `seed --reset` gives the same fresh demo (`L2-072` to `L2-077`, ADR platform/0010).
- HTML mocks of every page, dialog and notification in every state, light and dark, responsive, in `docs/mocks/` (235 mocks across 56 screens), styled from the TuTuMuch logo.
- L1 high-level and L2 detailed requirements with acceptance criteria in `docs/specs/`.
- Documented the technology stack: Angular frontend, .NET backend, full-stack responsive web application.
- Repository scaffolding: README, contributing guide, code of conduct, security policy, support policy, MIT license, and issue and pull request templates.
- Agent skills in `.claude/skills` and `.agents/skills`.
- Blank `AGENTS.md`, `CLAUDE.md`, `GEMINI.md` and `.github/copilot-instructions.md`.
