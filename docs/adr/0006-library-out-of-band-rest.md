# Library served out-of-band via REST, not in the SSE snapshot

Status: accepted

The Library — every song under the music directory — is fetched once via `GET /library` and is **not** part of the SSE state snapshot. This is a deliberate exception to ADR-0001's full-snapshot declarative replication: the Library is static reference data (it changes only when files appear on disk), whereas the snapshot re-broadcasts on every play/pause/tick. Embedding the Library would re-send the whole pool on every state change for data that does not change. The two phase Playlists remain in the snapshot because they are mutable playback state.

## Consequences

- `SongLibrary` indexes the full directory (`LoadSongs()`), not just songs already in the two M3U Playlists, so a Library song not yet added still resolves for `/audio/{id}` and `add`.
- A future "rescan library while running" capability needs an explicit refresh (re-fetch or a change event); there is no live re-scan today (YAGNI).
- The wire contract is no longer a single channel: one REST fetch for reference data plus the SSE stream for state. The snapshot stays small.
