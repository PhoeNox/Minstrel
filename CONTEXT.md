# Minstrel

A music player for the Storyteller of *Blood on the Clocktower*: day/night playlists with automatic switching, volume fading, and a countdown timer. This glossary fixes the language used across the backend / two-frontend split.

## Language

**Backend**:
The authoritative owner of playback state — which playlist is active, which song is current, and the position within it. Holds the playback clock; the single source of truth both frontends attach to.
_Avoid_: Server, host, hub.

**Player**:
A frontend that renders the backend's playback state into actual sound. Owns the browser audio engine. Disposable — it can be closed and reopened, fetching current state and resuming.
_Avoid_: Playing client, display, output.

**Remote**:
A frontend that projects backend state into a control surface and emits commands (play, pause, switch phase, select song, set gain, run timer). Holds no playback state of its own.
_Avoid_: Controller, control client, admin.

**Mobile**:
The standalone single-device variant of Minstrel, for running a game off a phone alone. It collapses the **Backend**, **Player**, and **Remote** roles into one offline app: it owns its own state, renders its own sound, and is its own control surface, with no network between them. Same domain and language as the desktop deployment (Game Phases, Playlists, Library, Timer, Fade); only the topology differs.
_Avoid_: App, native app, mini.

**Game Phase**:
Day or Night. Each phase has its own Playlist; switching phase crossfades from one phase's Current Entry to the other's. Switching to a phase whose Playlist is empty cues nothing (Nothing Cued) — silence until you switch back or add a song.
_Avoid_: Mode, state (overloaded).

**Playlist**:
The ordered list of songs for one Game Phase. Mutable: songs are added from the Library, removed, reordered, or shuffled, and the result is persisted as M3U. An entry is addressed by its index, so the same song may appear more than once. Carries no dynamic playback state of its own — Current Entry and Gain belong to playback.
_On Mobile_: persisted as app-held data rather than M3U files; the role is unchanged.
_Avoid_: Queue, tracklist.

**Current Entry**:
The selected song within a Game Phase's Playlist, addressed by index. Each phase has its own, remembered even while that phase is dormant; switching to a phase resumes at its Current Entry. An empty Playlist has no Current Entry, so switching to it leaves the phase Nothing Cued. Playback state, not part of the Playlist.
_Avoid_: Cursor, current song, selected track.

**Nothing Cued**:
The state of the active phase when it has no Current Entry — its Playlist is empty. Position is idle, no sound plays, and the Player fades to silence. Switching back to a phase with a Current Entry resumes playback.
_Avoid_: Stopped, empty, off.

**Gain**:
The per-phase output level applied to a Playlist's playback. Live playback state, not persisted with the Playlist. Distinct from Fade, which shapes loudness over time at a song's start or stop.
_Avoid_: Volume, level.

**Library**:
Every song available under the music directory — the pool a Playlist draws from. Static reference data, loaded once; not part of replicable playback state.
_On Mobile_: a pool of songs imported into the app rather than auto-scanned from a directory; still the pool a Playlist draws from.
_Avoid_: Database, catalog, song database.

**Position**:
The playback offset within the current song. Owned by the Backend, so any Player resumes at the same point.
_Avoid_: Progress, time, offset (in user-facing language).

**Fade**:
The gain envelope applied when a song starts or stops (in/out). Purely a Player concern — it shapes loudness over time, not Position.
_Avoid_: Ramp, transition.

**Gong**:
The one-shot sound played when the Timer expires. A transient event, not part of replicable playback state.
_Avoid_: Alarm, chime, bell.
