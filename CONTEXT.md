# Minstrel

A music player for the Storyteller of *Blood on the Clocktower*: day/night playlists with automatic switching, volume fading, and a countdown timer. This glossary fixes the language used across the planned backend / two-frontend split.

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

**Game Phase**:
Day or Night. Each phase has its own Playlist; switching phase crossfades from one Playlist's current song to the other's.
_Avoid_: Mode, state (overloaded).

**Position**:
The playback offset within the current song. Owned by the Backend, so any Player resumes at the same point.
_Avoid_: Progress, time, offset (in user-facing language).

**Fade**:
The gain envelope applied when a song starts or stops (in/out). Purely a Player concern — it shapes loudness over time, not Position.
_Avoid_: Ramp, transition.

**Gong**:
The one-shot sound played when the Timer expires. A transient event, not part of replicable playback state.
_Avoid_: Alarm, chime, bell.
