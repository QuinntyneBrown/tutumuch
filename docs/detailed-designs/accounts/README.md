# Accounts subsystem

The accounts subsystem covers how a visitor becomes a member, proves their email, signs in (with a password or with Google) and stays signed in, and how `TuTuMuch API` decides what a caller may do (`L1-001`, `L1-002`). Pages live in the `@tutumuch/accounts` library (`src/frontend/projects/accounts/`); session state, the session interceptor and the typed clients live in `@tutumuch/api`. The slices are built on the [architecture baseline](../README.md).

| Feature | Design | Estimate | L2 requirements |
|---------|--------|----------|-----------------|
| Sign up and sign in | [sign-up-and-sign-in](sign-up-and-sign-in/README.md) | [estimate](../../estimates/accounts.md) | `L2-001`, `L2-002`, `L2-006`, `L2-008`, `L2-010` |
| Sign in with Google | [sign-in-with-google](sign-in-with-google/README.md) | [estimate](../../estimates/accounts.md) | `L2-088` |

Password reset (`L2-009`), lockout (`L2-007`), managing sessions, editing the profile and closing the account are listed as open questions in sign-up-and-sign-in and get their own designs when their journeys are built.
