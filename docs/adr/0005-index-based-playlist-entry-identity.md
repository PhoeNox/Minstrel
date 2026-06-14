# Index-based playlist-entry identity

Status: accepted

A Playlist may contain the same song more than once (original parity: add appends unconditionally). Because `songId` is a hash of the file path, duplicate entries share an id, so an entry is identified by its **index** in the Playlist, not by `songId`. The current song, song selection, auto-advance, and current-song repair are all index arithmetic; `Position.SongId` is retained only to tell the Player which audio file to fetch and fade. The state snapshot exposes `CurrentIndex` per Playlist (not `CurrentSongId`), and `SelectCommand` carries `(phase, index)`.

## Considered Options

- **Id-keyed identity (rejected):** simpler and the obvious default, but `FindSong`/`NextSong`/`FindIndex` resolve to the first matching id, so a duplicated song breaks advance, selection, and current-song highlighting.
- **Per-entry instance id (rejected for now):** a second id concept distinct from `songId`, robust to reorder races, but adds a type to the snapshot, commands, and M3U round-trip for no present benefit — the index already disambiguates.

## Consequences

- The snapshot DTO and command set are index-shaped; a reader must not "simplify" back to id-keyed identity without re-banning duplicates.
- Move was already index-based and is unaffected; remove and select become index-based to match.
