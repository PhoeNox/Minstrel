# Backend-authoritative playback with declarative state replication

Status: accepted

The Backend owns the authoritative playback timeline — active Game Phase, playlists, current song, gains, Timer, and **Position** — while each Player renders that state into sound and owns only its local Fades. Position is held as an anchor `(songId, offset, anchorTimestamp, isPlaying)`, not a streamed number: the Backend recomputes it on demand and broadcasts only on change; Player and Remote derive the live value locally. The Backend pushes **full state snapshots** (declarative replication, not an imperative command stream), and the Player reconciles each snapshot against its actual audio graph. One-shot transients that are not state — the Gong — travel on a separate event channel.

## Considered Options

- **Imperative command stream** (the prior in-process `PlaySongSignal`/`PauseSongSignal` model): rejected because a disposable, reconnecting Player cannot replay commands it missed — it can only fetch current state and reconcile.
- **Streamed position ticks**: rejected as chatty and prone to seek-stutter and clock drift; the anchor is derived locally instead.
- **Backend-timed low-level audio** (Backend schedules gain ramps over the wire): rejected — fades are latency-sensitive and belong at the edge.

## Consequences

- **Song-end detection moves to the Backend.** The Backend's clock advances the anchor and decides the next song; the former Player-side `SongEndedSignal`/`MonitorSongEnd` is removed. This also avoids duplicate end events if more than one Player is open.
- **Players are disposable.** Reload or crash loses nothing: on reconnect the Player gets a snapshot, derives Position from the anchor, seeks, and resumes. This is the direct cure for the original pain — audio state dying with a Blazor circuit.
- **No single-active-Player enforcement** (YAGNI): in practice one Player sounds at a time; the failover case has the old Player already gone.
