# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Minstrel** is a music player for Storytellers running *Blood on the Clocktower* tabletop games. An ASP.NET Core **Backend** owns all playback state; two SvelteKit frontends attach to it — the **Player** renders the audio and the **Remote** is a phone control surface. It manages day/night playlists with automatic switching, volume fading, and an integrated countdown timer.

See `CONTEXT.md` for the domain glossary and `docs/adr/` for the architectural decisions.

## Commands

This project uses `just` as a task runner:

```bash
just build          # Build Player + Remote bundles, then the .NET solution
just test           # Run Backend (.NET) and Player (Vitest) tests
just ci             # Build + test (Release config)
just run            # Build bundles, then run the Backend (serves both frontends)
just publish RID="linux-x64"    # Self-contained, shippable Backend folder
just publish RID="osx-x64"
just publish RID="win-x64"
```

Equivalent .NET CLI:
```bash
dotnet build Minstrel.slnx -c Release
dotnet test --solution Minstrel.slnx -c Release
```

.NET tests use the **TUnit** framework (not xUnit/NUnit); Player tests use **Vitest**. Run a single Backend test class with:
```bash
dotnet test Backend.Tests/Backend.Tests.csproj --filter "FullyQualifiedName~ClassName"
```

## Architecture

- **`Core/`** — Immutable domain records (`Song`, `Playlist`, `GamePhase` enum). No dependencies.
- **`Infrastructure.FileSystem/`** — M3U playlist persistence and TagLibSharp metadata.
- **`Infrastructure.Network/`** — local IP detection.
- **`Backend/`** — ASP.NET Core. The authoritative playback state machine (plain C#) and the HTTP surface. Organized by concern: `Playback`, `Library`, `Timer`, `Endpoints`, `Contracts`, `Commands`, `Music`.
- **`Player/`** — SvelteKit (TypeScript) frontend that renders the Backend's state into sound via native Web Audio. Disposable: it can be closed and reopened, fetching current state and resuming.
- **`Remote/`** — SvelteKit (TypeScript) control surface. Emits commands; holds no playback state of its own.
- **`shared/`** — TypeScript shared by both frontends (`position`, `timer`).

The Backend reuses `Core`, `Infrastructure.FileSystem`, and `Infrastructure.Network` via `ProjectReference`.

### State and Transport

The Backend holds the single source of truth (active playlist, current song, position within it) — there is one global session, so both frontends attach to the same state. The data flow is:

```
Frontend → REST command → Backend state machine → SSE state snapshot → both frontends
```

- **Backend → frontend:** Server-Sent Events carry full state snapshots plus one-shot events (e.g. the Gong).
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
