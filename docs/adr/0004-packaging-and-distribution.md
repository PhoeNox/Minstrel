# Packaging and distribution

Status: accepted

> **Retired 2026-06-15:** "Minstrel.Next" below is now just **Minstrel** — the old Blazor stack is removed and its solution renamed `Minstrel.slnx`. The stale `publish` target that built `App/App.csproj` has been replaced by the Backend + Player + Remote pipeline described here.

Minstrel.Next ships as a **per-OS zip of a self-contained folder**, targeting Windows, Linux, and macOS. Each zip contains a self-contained Backend executable (the .NET runtime is bundled — nothing to install), `appsettings.json`, the built Player + Remote bundles under `wwwroot/`, an empty `Music/` folder seeded with sample `day.m3u` / `night.m3u`, and a README. The Storyteller unzips, sets the music folder once in `appsettings.json`, and double-clicks the executable; the Backend serves both frontends, auto-opens the Player in the default browser, and shows a QR code that a phone on the same wifi scans to reach the Remote.

The music directory is **configured in `appsettings.json`**, not chosen through a UI. The default `../App/Music` (a path into the retired Blazor tree) is replaced, and a relative path is resolved against the executable's own location rather than the working directory, since a double-clicked binary's CWD is unpredictable.

For phone reachability the Backend improves local-IP detection (prefer the Up, non-virtual interface owning the default gateway, to avoid encoding a VirtualBox/WSL/Docker/VPN address into the QR) and exposes a `Host` override in `appsettings.json` as an escape hatch. The default HTTP port moves off 5000 (taken by the macOS AirPlay Receiver and a common dev-server port) to a quieter, user-overridable port.

macOS binaries are shipped **unsigned**; the README documents the Gatekeeper bypass (`xattr -dr com.apple.quarantine` / right-click → Open).

## Considered Options

- **Per-OS self-contained zip (chosen)**: honest about the fact that editable `appsettings.json`, a `Music/` folder, and the `wwwroot/` static assets must sit beside the binary anyway, so the deliverable is inherently a folder. Self-contained means no .NET install.
- **Single literal file (`PublishSingleFile`)**: rejected as moot — ASP.NET's `wwwroot` static assets are emitted as a folder beside the exe regardless, and config + music already force a folder. Embedding `wwwroot` as manifest resources adds complexity for no real gain.
- **Framework-dependent build**: smaller download, rejected — requires the user to install the .NET runtime first, a hard blocker for a non-technical audience.
- **First-run folder picker** (native OS dialog / server-rendered setup page / browser File System Access API): rejected as overkill. The app has no native desktop shell; a browser picker cannot hand the Backend a real server-side path (audio is streamed by filesystem path); native dialogs mean fragile per-OS code (Linux often lacks `zenity`/`kdialog`). A single `appsettings.json` line is enough.
- **macOS Developer ID + notarization**: rejected for v1 — $99/yr plus CI signing secrets for a true double-click experience, not justified while macOS is not a primary audience. Documented bypass instead.

## Consequences

- The deliverable is a folder, not one file; users are told "drop your songs in the folder you point `appsettings.json` at, and curate `day.m3u` / `night.m3u`." Playlist authoring stays the existing M3U workflow (unchanged from today, per the rewrite PRD).
- The stale `justfile publish` target (which still publishes the retired `App/App.csproj`) is replaced by a pipeline that builds the Player + Remote bundles, publishes `Backend` self-contained per RID, stages `appsettings.json` / `Music/` / README, and zips per OS.
- First-launch friction is documented, not engineered away: Windows shows a SmartScreen notice and a Defender Firewall "Allow access?" prompt (the latter must be allowed for the phone to connect); macOS needs the quarantine bypass.
- `Aspire/ServiceDefaults` remains referenced for the `just aspire` dev workflow; in the shipped binary its OpenTelemetry exporters no-op without a collector, so it is left in place.
