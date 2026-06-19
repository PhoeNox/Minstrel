# Standalone Mobile PWA collapsing the Backend/two-frontend split

Status: accepted

Minstrel gains a second deployment, **Mobile**: an installable, offline PWA that runs a whole game off one phone, for when carrying the laptop is too much hassle. It keeps the full feature set (Day/Night Playlists, Library, Timer, crossfade) but **collapses the Backend, Player, and Remote into a single offline app** — no SSE, no REST, no network between roles. It targets both iOS and Android, so the iOS browser constraints (no File System Access API, aggressive storage eviction, suspended `AudioContext` when backgrounded) are the baseline, not an edge case.

Reuse is split by language and by layer, not wholesale:

- **Pure domain logic** (`Core/` Playback/Playlist/Timer/Library transforms) is **reimplemented in TypeScript**. It is small and pure; the C# cannot run client-side and a JS bridge is not worth it. Only Mobile needs it — the desktop frontends delegate this to the Backend.
- **The control-surface UI is shared with the Remote behind a command/query interface.** The Remote already *is* a phone control surface; the interface it programs against gets two implementations — Remote = POST-to-Backend (REST), Mobile = call-local-TS-Core (in-process) — so the Svelte components are shared, not forked.
- **The audio engine's structure is a template, not a copy.** Because background/lock-screen playback is a hard requirement, the engine is rebuilt on `HTMLAudioElement → MediaElementAudioSourceNode → GainNode → destination` (+ MediaSession for lock-screen controls), keeping crossfade while surviving backgrounding. The buffer-source core and decoded-PCM `bufferCache` of `Player/src/lib/audioEngine.ts` largely fall away.
- **Discarded entirely:** the transport/coordination layer — `Sessions/`, `Api/`, SSE, the position anchor, `reconciler`, `sseStore`, `connection`, `playbackSync`. In one process the `AudioContext` is the authoritative clock and song-end is a media `ended` event, so none of it is needed.

Music is **imported into app-managed storage** (no live folder, no M3U on disk): `navigator.storage.persist()` is requested and storage is treated as a cache, with fast folder/multi-file re-import as the recovery path after eviction or reinstall. Song metadata (Title/Artist/Length) is read at import time by a JS tag parser, replacing TagLibSharp. Mobile lives in **this monorepo** as a new sibling app; the genuinely shared TS (the low-level audio engine, `shared/timer`, `shared/position`) is promoted into `shared/` so Player and Mobile import one copy. It is distributed as a **static PWA on a free HTTPS host** (e.g. GitHub Pages), installed once and cached offline by a service worker.

## Considered Options

- **Capacitor / native shell** (rejected for now): real native file access and home-screen presence, but adds a build/signing toolchain and app-store friction for no capability the installed PWA lacks here.
- **.NET MAUI reusing `Core` C# directly** (rejected): would reuse the domain logic verbatim but throws away the Web Audio crossfade engine, forcing a native-audio rewrite of the hardest-won code — the wrong half to keep.
- **Backend-on-phone** (rejected): reuses almost everything but is hostile on iOS and absurd for a single-user local app.
- **Stream audio from a URL/cloud** instead of local storage (rejected): removes eviction risk but needs connectivity at the friend's house — the exact thing the offline app exists to avoid.
- **Fork the Remote UI / fork the audio engine into Mobile** (rejected): fastest start, but two drifting copies — the divergence ADR-0003 set out to avoid.
- **Pure Web Audio buffer engine + Wake Lock** (rejected): lets the engine be copied nearly as-is, but the screen must stay lit all game and Wake Lock drops on tab-hide; fails real backgrounding.

## Consequences

- **A new abstraction — the command/query interface — now sits between the Remote UI and its data source.** It earns its keep only because there are genuinely two implementations; do not grow speculative third impls or leak transport concerns (status codes, SSE) into it.
- **The TS `Core` is a second source of the domain rules**, parallel to the C# `Core`. They can silently diverge. Keep the TS port a faithful, behavior-test-backed mirror; when a domain rule changes in one, change both. This is the standing cost of a client-side offline variant.
- **`CONTEXT.md` gains a `Mobile` term**; Library and Playlist keep their roles but shift substance (imported pool, app-held persistence). Backend/Player/Remote remain the desktop-deployment roles.
- **iOS storage eviction is accepted, not engineered away** (per the durability choice): the recovery story is fast re-import, so re-import friction is a first-class UX concern, not an afterthought.
- **Crossfading two simultaneous media elements through Web Audio is the known iOS risk** in this design; validate it on a real device early, since the whole audio layer rests on it.
- **The static-host PWA is decoupled from the desktop Backend's release**; Mobile installability and the service-worker offline shell need real-device testing from the first slice, not at the end.
