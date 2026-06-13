# SvelteKit frontends, .NET backend, SSE + REST

Status: accepted

The Player and Remote are rewritten as **SvelteKit (TypeScript)** apps against an **ASP.NET Core** backend. Blazor and Fluxor are dropped; backend state becomes a plain C# state machine that reuses the `Core` records and the existing domain logic from `Features`. Transport is **Server-Sent Events** for Backend→frontend (full state snapshots and one-shot events such as the Gong) plus **REST** for frontend→Backend commands. There is one global session, so both frontends simply subscribe to the same SSE stream — no session correlation. Audio is delivered over HTTP by opaque id: `GET /audio/{songId}`; DTOs never expose filesystem paths.

## Considered Options

- **Blazor WASM both + SignalR**: maximal C# reuse (records as the literal wire contract) but keeps the framework being escaped and the JS-interop audio wrapper.
- **Blazor Server, restructured**: least UI rewrite, but Player rendering stays on a Server circuit — only partially escapes the coupling that motivated the rewrite.
- **Node/TS backend**: single language end-to-end, but discards working C# domain logic and tests.
- **SignalR / raw WebSocket** for transport: SSE+REST chosen for being standard, trivially browser-debuggable, and a natural fit for one broadcast stream of declarative state.

## Consequences

- The C# Web Audio interop layer (`Infrastructure.Playback`, the `AudioContext` wrapper) is **deleted, not ported** — the Player uses native Web Audio. This was the heaviest, least-testable code in the repo.
- The wire contract is JSON DTOs, no longer shared C# records; TypeScript types are generated from the C# DTOs (or hand-written, the surface is small).
- Preserved: `Core`, the domain logic in `Features` (reimplemented as plain C#), `Infrastructure.FileSystem`, `Infrastructure.Network`. Discarded: Blazor `App/`, Fluxor, the audio interop, and the cross-frontend load-gating signals.
