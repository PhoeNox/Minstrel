# Backend feature modules wired by edge integrations, not inter-module dependencies

Status: accepted

The Backend is restructured into independent, pure feature modules — **Playback**, **Playlist**, **Timer** — over a shared **Pool** of reference data (the song library). No module references another. All composition happens in **integrations at the edge** (IOSP): the command endpoints, the per-stream SSE snapshot assemblers, and the `PlaybackClock` background service. An integration resolves a `SongId` against the Pool, drives a pure module operation, performs the I/O, and reconciles sibling modules — but holds no domain logic itself. This supersedes the layered alternative (`Playback → Playlist → Library`) that a literal slicing would have produced.

The split forces a relocation of state: **Current Entry (the per-phase index cursor), Gain, and ResumeOffset move into Playback**; the Playlist becomes a pure ordered list of denormalized song entries (`Id`, `Path`, `Title`, `Artist`, `Length`) that knows nothing of playback. Each Playlist entry is denormalized so the Playlist can be persisted to M3U and snapshotted without reaching into the Pool at runtime; `CONTEXT.md` was updated to match (Current Entry and Gain are now playback terms, not Playlist terms).

## Considered Options

- **Strict independent slices, fused snapshot (rejected):** one SSE snapshot per slice plus a composition edge. Foundered on the current-entry/position/song-metadata invariants — Playback cannot render or advance without the current song, which lives in the Playlist.
- **Layered dependency DAG `Playback → Playlist → Pool` (rejected):** honest and acyclic, but bakes inter-module compile-time dependencies. The IOSP move dissolves them: auto-advance is fed by the clock-integration, command paths by the endpoint-integration, so no module need import another.
- **Current Entry / Gain kept in the Playlist (rejected):** matched the old glossary, but the cursor is set on a *dormant* phase's playlist (select on the inactive phase changes nothing playing) and is per-phase live state — dynamic playback state, not a property of the persisted list.

## Consequences

- **No module emits a complete snapshot alone.** The Player's view (current song ⨝ position ⨝ gain) and the Remote's editable playlist view (songs ⨝ cursor ⨝ gain) are assembled by the SSE endpoint integrations from Playback + Playlist.
- **Auto-advance must stay a pure operation.** Song-end has no request thread; `PlaybackClock` ticks at 250ms and is the integration that detects it. The advance decision (`position ≥ length → nextIndex`) must remain a pure function fed inputs from both modules, or the clock silently becomes a logic-bearing integration in violation of IOSP.
- **Consistency is restored by a single coordinating lock at the integration boundary**, replacing the old single `PlaybackSession` gate. Every command and every tick takes it. It serializes all state mutation — acceptable and intended for a single-session app; not a per-module lock (which would admit snapshot-ordering races) nor an actor loop (YAGNI here).
- **The cursor stays index-based** (ADR-0005 holds), so the Playlist→Playback change signal carried by the endpoint integration must include structural detail (removed-at-N, moved-old→new, shuffle permutation) for the cursor to follow its entry.
- **Pool resolution and M3U persistence are integration-level I/O**, not module logic: the Playlist module returns a new list; the endpoint persists it. Domain logic stays free of I/O.
- **Timer is the one genuinely independent module** — own state, own clock, own `/sse/timer`, own Gong one-shot — depending on nothing but `GongOptions`.
