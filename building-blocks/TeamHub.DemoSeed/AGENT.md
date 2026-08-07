## Purpose
- Shared deterministic demo user identity generation for auth and organization seeders.

## Source of truth
- `DemoUserIdentityFactory.cs`

## Do
- Username: `Name+Surname+123` (ASCII letters only; collision suffix `2`/`3`/… before `123`).
- Password and email local-part: lowercase username (e.g. `JanWilk123` → `janwilk123` / `janwilk123@teamhub.local`).
- Locales alternate PL/EN by index (`i % 2`) so generating a prefix of N identities matches the first N from a larger run.
- Keep `RandomizerSeed` stable so auth bulk users and organization member resolution stay aligned.
- Reserved active login username (default `JanWilk123`) must be passed as `reservedUsername` when generating bulk identities.

## Don't
- Do not change the seed or naming algorithm without updating auth and organization seed docs/`AGENT.md`.
