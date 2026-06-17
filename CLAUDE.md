# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Minstrel** is a music player for Storytellers running *Blood on the Clocktower* tabletop games. An ASP.NET Core **Backend** owns all playback state; two SvelteKit frontends attach to it — the **Player** renders the audio and the **Remote** is a phone control surface. It manages day/night playlists with automatic switching, volume fading, and an integrated countdown timer.

See `CONTEXT.md` for the domain glossary and `docs/adr/` for the architectural decisions.

## Commands

This project uses `just` as a task runner:

```bash
just build          # Build Player + Remote bundles, then the .NET solution
just push-check     # Primary pre-push gate: full build + every test, including E2E
just test           # Run Backend (.NET) and Player (Vitest) tests
just e2e            # Run only the Playwright E2E snapshot suite (local-only; excluded from CI)
just ci             # Build + test (Release config); does not run E2E
just run            # Build bundles, then run the Backend (serves both frontends)
just publish RID="linux-x64"    # Self-contained, shippable Backend folder
just publish RID="osx-x64"
just publish RID="win-x64"
```

Equivalent .NET CLI:
```bash
dotnet build Minstrel.slnx -c Release
dotnet test --project Backend.Tests/Backend.Tests.csproj -c Release   # E2E suite excluded; run it via `just e2e`
```

.NET tests use the **TUnit** framework (not xUnit/NUnit); Player tests use **Vitest**. Run a single Backend test class with:
```bash
dotnet test Backend.Tests/Backend.Tests.csproj --filter "FullyQualifiedName~ClassName"
```

**`Player.E2E.Tests`** drives the real Backend (launched out-of-process with `LaunchBrowser=false`, serving the built `wwwroot`) through headless Chromium via **Playwright**, asserting **image snapshots** with **Verify**. It is **local-only** — too unstable for CI, so the GitHub pipeline never runs it; `just ci`/`just test` exclude it and only `just e2e` (or `just push-check`) runs it. Because it serves built bundles, run it through `just e2e` / `just push-check` (which build the frontends first), not a bare `dotnet test` on a stale `wwwroot`. Baselines are `*.verified.png` committed beside the test; a mismatch writes `*.verified.png` → `*.received.png` (gitignored). To accept a new baseline, replace the `.verified.png` with the `.received.png`. Snapshots depend on web-font load at capture time and on OS font rendering, so a baseline is environment-specific.

## Architecture

- **`Core/`** — The pure, dependency-free domain: immutable records plus the **pure feature modules** (ADR-0007) that own domain logic and reference one another not at all:
  - **`Core/Playback/`** — `PlaybackState` (per-phase `{cursor, gain, resumeOffset}`, `ActivePhase`, `Position`) and `PlaybackTimeline`, the pure transforms (play/pause/select/switch, cursor reconciliation after structural edits, and the pure `Tick`/`Advance` auto-advance fed `Track[]`).
  - **`Core/Playlist/`** — `PlaylistEntry` (denormalized: `Id`, `Path`, `Title`, `Artist`, `Length`), `PlaylistBook` (the two phases' ordered lists), and `Playlists`, the pure ordering transforms (add/remove/move/shuffle). Holds no cursor, gain, or position.
  - **`Core/Library/`** — `SongPool`, the shared reference data both phases draw from.
  - **`Core/Timer/`** — `TimerAnchor` (`{Running, AnchorTimestamp, DurationLeftAtAnchor}`) and `CountdownTimer`, the pure `Start`/`DeriveTimeLeft`/`HasExpired` transforms.
  - Plus `Song`/`SongId`, the `GamePhase` enum, and the `Connection`/`Version` info records.
- **`FileSystem/`** — M3U playlist persistence and TagLibSharp metadata.
- **`Network/`** — local IP detection.
- **`Backend/`** — ASP.NET Core, in four layers: the pure `Core` modules, the stateful **`Sessions/`** coordinators, the I/O **providers** (`FileSystem`/`Network`), and the HTTP **`Api/`** endpoints. The coordinators wire the pure `Core` modules through **edge integrations** that carry no domain logic (ADR-0007):
  - **`Sessions/`** — the stateful coordinators, each holding live Core state behind one lock and broadcasting it to subscribers as raw Core records (no DTOs):
    - `LiveSession` holds `PlaybackState` + `PlaylistBook` — every command and every `PlaybackClock` tick takes its lock; it resolves a `SongId` via the Pool, drives a pure Playlist/Playback op, performs M3U I/O, reconciles the index-cursor from the structural change, and broadcasts `SessionEvent`.
    - `TimerSession` is the independent countdown — own state, `TimerClock` tick, `/timer/sse` stream, and Gong one-shot — broadcasting `TimerEvent`.
  - **`Api/`** — thin HTTP endpoint adapters (`PlaybackEndpoints`, `PlaylistEndpoints`, `LibraryEndpoints`, `TimerEndpoints`, `SystemEndpoints`) over the sessions; also owns the wire DTOs and the pure `SnapshotMapper` (`SnapshotDtos.cs`, assembling `Playback ⨝ Playlist` and mapping Core→wire per connected client at broadcast time) plus the inline `TimerDto` and `GongOptions`.
- **`Player/`** — SvelteKit (TypeScript) frontend that renders the Backend's state into sound via native Web Audio. Disposable: it can be closed and reopened, fetching current state and resuming.
- **`Remote/`** — SvelteKit (TypeScript) control surface. Emits commands; holds no playback state of its own.
- **`shared/`** — TypeScript shared by both frontends (`position`, `timer`).
- **`Player.E2E.Tests/`** — TUnit + Playwright + Verify end-to-end image-snapshot tests that exercise the shipped Backend + frontends together.

The Backend reuses `Core`, `FileSystem`, and `Network` via `ProjectReference`.

### State and Transport

The Backend holds the single source of truth (active playlist, current song, position within it) — there is one global session, so both frontends attach to the same state. The data flow is:

```
Frontend → REST command → Backend state machine → SSE state snapshot → both frontends
```

- **Backend → frontend:** Server-Sent Events, one stream per concern. `/sse` carries the playback state snapshot; `/sse/timer` carries the timer snapshot plus the `gong` one-shot event. Each frontend subscribes to both.
- **Frontend → Backend:** REST commands (play, pause, switch phase, select song, set gain, run timer).
- **Audio:** delivered over HTTP by opaque id (`GET /audio/{songId}`). DTOs never expose filesystem paths.

### Key Behaviors

- **Authoritative position:** the Backend owns the playback clock, so any Player resumes at the same point.
- **Audio fading:** purely a Player concern — the gain envelope when a song starts/stops, shaped via native Web Audio.
- **Playlist auto-advance:** when the current song ends, the next entry in the active playlist loads automatically.
- **Phase switching:** toggling Day/Night crossfades from the current playlist's song to the other playlist's current song.
- **Timer:** a countdown that plays the Gong at a configurable gain when it expires.

### Configuration

The Backend reads `Music:Directory` from `appsettings.json` (wrapped in `MusicOptions`). The HTTP port defaults to `5757` (override via the `Port` configuration key).

## Agent skills

### Issue tracker

Issues and PRDs live as local markdown files under `.scratch/<feature-slug>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical triage roles, recorded as a `Status:` line per issue file. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
