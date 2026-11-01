# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Minstrel** is a music player for Storytellers running *Blood on the Clocktower* tabletop games. An ASP.NET Core **Backend** owns all playback state; two SvelteKit frontends attach to it — the **Player** renders the audio and the **Remote** is a phone control surface. A third frontend, **Mobile**, is a standalone PWA with no Backend (ADR-0008). It manages day/night playlists with automatic switching, volume fading, and an integrated countdown timer.

See `CONTEXT.md` for the domain glossary and `docs/adr/` for the architectural decisions.

## Commands

`just` is the task runner; `just --list` shows every recipe. The gates:

- `just push-check` — run by the global pre-push hook: `ci` + `e2e`.
- `just ci` — build + `check` (svelte-check on all frontends) + `test`; what GitHub CI runs. Excludes E2E.
- `just e2e` — local-only snapshot suites: `E2E.Tests` (.NET) and Mobile's native Playwright suite (ADR-0009).

.NET tests use **TUnit** (not xUnit/NUnit); frontends use **Vitest**. Single Backend test class:
```bash
dotnet test Backend.Tests/Backend.Tests.csproj --filter "FullyQualifiedName~ClassName"
```

**`E2E.Tests/`** drives the real Backend (launched out-of-process with `LaunchBrowser=false`, serving the built `wwwroot`) through headless Chromium via **Playwright**, asserting **image snapshots** with **Verify**. It is **local-only** — too unstable for CI. Because it serves built bundles, run it through `just e2e` / `just push-check` (which build the frontends first), not a bare `dotnet test` on a stale `wwwroot`. Baselines are `*.verified.png` beside the test; a mismatch writes `*.received.png` (gitignored). To accept a new baseline, replace the `.verified.png` with the `.received.png`. Snapshots depend on web-font load and OS font rendering, so a baseline is environment-specific. Any Player/Remote layout change can shift baselines.

## Architecture

- **`Core/`** — pure, dependency-free domain: immutable records plus pure feature modules (`Playback/`, `Playlist/`, `Library/`, `Timer/`) that never reference one another (ADR-0007).
- **`FileSystem/`** — M3U playlist persistence and TagLibSharp metadata. **`Network/`** — local IP detection.
- **`Backend/`** — ASP.NET Core: stateful **`Sessions/`** coordinators wire the pure `Core` modules through edge integrations that carry no domain logic (ADR-0007); **`Api/`** holds thin endpoint adapters, wire DTOs and `SnapshotMapper`.
  - Why the two sessions differ on the wire: the playback snapshot is identical for every subscriber, so `PlaybackSession` maps and serializes the SSE frame *once* per broadcast (via the injected `PlaybackEndpoints.RenderFrame`); `TimerSession` broadcasts raw Core records and the endpoint maps per subscriber.
- **`Player/`** — SvelteKit frontend rendering Backend state into sound via native Web Audio. Disposable: reopening fetches state and resumes.
- **`Remote/`** — SvelteKit control surface. Emits commands; holds no playback state.
- **`Mobile/`** — standalone SvelteKit PWA (ADR-0008): ports the Core logic to TypeScript, stores songs in IndexedDB, deployed to GitHub Pages via `just mobile BASE_PATH=…`.
- **`shared/`** — the `minstrel-shared` npm package, linked into each frontend via `file:../shared` and imported as `minstrel-shared/...`: TS Core port (`core/`), shared UI and API client (`ui/`), `timer`, `replayGain`.

### State and Transport

One global Backend session is the single source of truth; Player and Remote attach to the same state:

```
Frontend → REST command → Backend state machine → SSE state snapshot → frontends
```

- **Backend → frontend:** SSE, one stream per concern: `/playback/sse` (playback snapshot) and `/timer/sse` (timer snapshot plus the `gong` one-shot).
- **Audio:** HTTP by opaque id (`GET /audio/{songId}`). DTOs never expose filesystem paths.
- **Fading** and **ReplayGain** are purely Player concerns, applied via Web Audio.

### Configuration

The Backend reads `Music:Directory` from `appsettings.json` (`MusicOptions`). HTTP port defaults to `5757` (override via `Port`).

## Agent skills

### Issue tracker

Issues and PRDs live as local markdown files under `.scratch/<feature-slug>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical triage roles, recorded as a `Status:` line per issue file. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
