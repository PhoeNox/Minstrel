# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Minstrel** is a Blazor Server music player for Storytellers running *Blood on the Clocktower* tabletop games. It manages day/night playlists with automatic switching, volume fading, and an integrated countdown timer.

## Commands

This project uses `just` as a task runner:

```bash
just build          # Build solution
just test           # Run tests
just ci             # Build + test (Release config)
just publish RID="linux-x64"    # Self-contained single-file binary
just publish RID="osx-x64"
just publish RID="win-x64"
```

Equivalent .NET CLI:
```bash
dotnet build Minstrel.slnx -c Release
dotnet test --solution Minstrel.slnx -c Release
```

Tests use the **TUnit** framework (not xUnit/NUnit). Run a single test file with:
```bash
dotnet test Features/Features.Tests/Features.Tests.csproj --filter "FullyQualifiedName~ClassName"
```

## Architecture

The app is structured in four layers:

- **`Core/`** — Immutable domain records (`Song`, `Playlist`, `GamePhase` enum). No dependencies.
- **`Features/`** — Fluxor (Redux) state management. Each feature has `State`, `Actions`, `Reducers`, `Effects`, and sometimes `Signals`/`Interactions`.
- **`Infrastructure.*`** — Side-effect implementations: `Infrastructure.Playback` (Web Audio API via JS interop), `Infrastructure.FileSystem` (M3U playlist persistence, TagLibSharp metadata), `Infrastructure.Network` (local IP detection).
- **`App/`** — Blazor Server components. Components subscribe to Fluxor state and dispatch actions; they contain no business logic.

### State Management Pattern

All app state lives in the Fluxor store. The data flow is:

```
UI Component → Dispatch Action/Signal → Reducer (pure state update) + Effect (side effect)
```

**Signals** are used for inter-feature communication (e.g., `SwitchPlaylistSignal` from `GamePhases` → `Playback`). They differ from actions in that they represent events rather than commands.

### Key Behaviors

- **Audio fading:** Songs fade in/out over 5 seconds via Web Audio API gain nodes (`PlaybackService`).
- **Playlist auto-advance:** When a song ends (`SongEndedSignal`), the next song in the active playlist loads automatically.
- **Phase switching:** `SwitchGamePhaseAction` → toggles Day/Night state → sends `SwitchPlaylistSignal` → fades out current song, fades in next song from the other playlist.
- **Timer:** Uses `PeriodicTimer` (100ms ticks); plays `Gong.mp3` at configurable volume when it expires.

### Configuration

The app reads `Music:Directory` from `appsettings.json` (wrapped in `MusicOptions`). The HTTP port defaults to 5000.
