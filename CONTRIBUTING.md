# Contributing to TuTuMuch

Thank you for your interest in contributing. This document explains how to propose changes and what is expected of a pull request.

## Table of contents

- [Before you start](#before-you-start)
- [Reporting bugs](#reporting-bugs)
- [Suggesting features](#suggesting-features)
- [Development setup](#development-setup)
- [Making changes](#making-changes)
- [Submitting a pull request](#submitting-a-pull-request)
- [Commit messages](#commit-messages)
- [Code of conduct](#code-of-conduct)
- [License](#license)

## Before you start

TuTuMuch is a marketplace for new and used **girls' (children's) clothing**: dresses, tops, outerwear and bottoms. Changes should serve that focus. Features that widen the marketplace to other sizes (boys' or adult) or categories are out of scope unless an issue agrees otherwise.

For anything beyond a small fix, open an issue first so the scope can be agreed before work begins.

## Reporting bugs

Use the [bug report template](.github/ISSUE_TEMPLATE/bug_report.md). Include:

- What you did, what you expected, and what happened.
- Steps to reproduce.
- Device, screen size and browser version.
- Screenshots or a recording if the problem is visual.

Please check existing issues first to avoid duplicates.

## Suggesting features

Use the [feature request template](.github/ISSUE_TEMPLATE/feature_request.md). Describe the problem the feature solves for buyers or sellers, not only the solution.

## Development setup

```bash
git clone https://github.com/QuinntyneBrown/tutumuch.git
cd tutumuch
```

TuTuMuch is a full-stack web application: an Angular frontend in `src/frontend` and a .NET (ASP.NET Core) backend in `src/backend`. See the [README](README.md#prerequisites) for prerequisites. Run instructions will be added as the code is introduced.

## Making changes

1. Fork the repository and create a branch from `main`.
2. Keep the change small and focused on one issue.
3. Follow the existing code style. Do not reformat or refactor code you are not changing.
4. Keep the frontend responsive. Every screen must work on phone, tablet and desktop widths.
5. Enforce business rules, such as the accepted sizes, in the .NET backend. The Angular app may validate too, but never as the only check.
6. Update documentation when behaviour or design changes.
7. Add an entry to [CHANGELOG.md](CHANGELOG.md) under **Unreleased**.

## Submitting a pull request

1. Make sure all checks pass.
2. Fill in the [pull request template](.github/PULL_REQUEST_TEMPLATE.md). Link the issue it resolves.
3. Keep the pull request proportional to the issue. A small fix should be a small diff.
4. Be responsive to review feedback. Maintainers may ask for changes before merging.

## Commit messages

- Use the imperative mood in the subject line ("Add listing form", not "Added listing form").
- Keep the subject line under 72 characters.
- Reference the issue in the body when applicable (`Fixes #123`).

## Code of conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md). By participating you agree to uphold it.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
