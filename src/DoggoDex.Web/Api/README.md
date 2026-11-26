# `DoggoDex.Web.Api`

The web app talks to the profile/search endpoints through one interface, `IProfileApi`, and picks an
implementation from configuration. Both implementations return the real `DoggoDex.Shared` DTOs, so
switching between them never touches component code:

| | `MockProfileApi` | `HttpProfileApi` |
| --- | --- | --- |
| Data | 7 seeded profiles in memory (`MockProfileData`) | `GET /api/search`, `GET /api/profiles/{slug}` |
| Needs the API running | no | yes |
| Needs a JWT | no | yes, and none is available yet (see below) |

## The switch

`Program.cs` reads `Api:UseMocks`:

```csharp
if (builder.Configuration.GetValue("Api:UseMocks", false))
    builder.Services.AddSingleton<IProfileApi, MockProfileApi>();
else
    builder.Services.AddHttpClient<IProfileApi, HttpProfileApi>(client =>
        client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5099/"));
```

- `appsettings.json` — `Api:UseMocks = false`. **Staging and production read this file, so leave it
  false.** The hard-coded fallback in `Program.cs` is false too, so a deployment that forgets to set
  the value talks to the real API instead of silently rendering seeded profiles.
- `appsettings.Development.json` — `Api:UseMocks = true`, so `dotnet run` gives you a working UI with
  no API, no database and no login. `Properties/launchSettings.json` sets
  `ASPNETCORE_ENVIRONMENT=Development` on both profiles, which is what turns this on.

To develop against the real API instead:

```bash
dotnet run --project src/DoggoDex.Api          # listens on the port in its launchSettings
# then set Api:UseMocks=false for Development, or override per-run:
Api__UseMocks=false Api__BaseUrl=http://localhost:5000/ dotnet run --project src/DoggoDex.Web
```

Until sign-in exists, requests from `HttpProfileApi` go out anonymous and the API answers `401`, which
is expected — see the `TODO(auth)` in `HttpProfileApi.SearchAsync`.

## Files

- `IProfileApi.cs` — `SearchAsync(q, type, page, pageSize)` and `GetProfileAsync(slug)`. `null` from
  `GetProfileAsync` is the API's 404.
- `MockProfileApi.cs` — in-memory implementation that copies the API's behaviour: browse mixes owners
  and dogs ordered by name then id; free text is an AND over name tokens (breed is *not* searched);
  a pasted link, slug, GUID or legacy shareable id is classified into `matchType` `Slug` / `Id`
  (a slug match outranks a shareable-id match); `page < 1` becomes 1, `pageSize` 0 becomes 20 and
  anything over 50 is clamped to 50 — the clamped value is what comes back in `PageSize`, so read it
  from the response.
- `MockProfileData.cs` — the seeded rows. Jared, Camila, Sky and Luna use the ids and slugs from the
  real seeded database so existing links keep working; Priya Nair and Mochi are mock-only rows that
  exist to exercise the "no reviews" state (`AverageRating = null`, `ReviewCount = 0`).
- `HttpProfileApi.cs` — the real client, both calls mapped to the shared DTOs.

Two behaviours are easy to get wrong when writing pages against the mock:

1. `AverageRating` is `null` (not `0`) whenever `ReviewCount == 0`. Render `StarRating` with the
   nullable value and let it show "No reviews yet".
2. Digit-only input that is not a GUID is classified as `Text`, not `Id` — that is what the API does,
   so a bare `12345` is a text search with no results.

## The Shared DTOs in this branch are temporary

`IProfileApi` is typed against `DoggoDex.Shared`, but those types live on the two open API PRs, not
here. This branch carries copies so it compiles on its own:

| File | Comes from |
| --- | --- |
| `src/DoggoDex.Shared/Profiles/ProfileLookupResponse.cs` | PR **#11** (DOG-24, slug resolver) |
| `src/DoggoDex.Shared/Common/PagedResult.cs` | PR **#12** (DOG-23, paged search) |
| `src/DoggoDex.Shared/Search/SearchDtos.cs` | PR **#12** (DOG-23, paged search) |

All three are **byte-identical** to the versions on `DOG-24-profile-slugs` / `DOG-23-search-api`
(checked with `git diff` against both branches). Rules:

- **Do not edit them here.** Any contract change belongs in #11 / #12, then gets picked up on rebase.
- **Delete them when #11 and #12 merge.** Merge those first and rebase this branch and the files come
  out identical, so there is nothing to resolve. If this branch lands first, #11 / #12 will need a
  trivial rebase for the same reason.
- Once they are gone, `MockProfileApi` and `HttpProfileApi` keep compiling untouched — they only
  reference the types, never the files.

The one thing that stays after those merges is the private `Classify` copy inside `MockProfileApi`,
which duplicates `DoggoDex.Api.Common.ProfileLinkParser`. That type lives in the API project, so the
web project cannot reference it. When `ProfileLinkParser` moves into `DoggoDex.Shared`, delete
`MockProfileApi.Classify` and call the shared one.
