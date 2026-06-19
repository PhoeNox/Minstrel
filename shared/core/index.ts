// The shared TS domain `core` (ADR-0008): a faithful mirror of C# `Core`, imported by
// Player, Remote, and Mobile. Pure and free of DOM/storage.

export * from './types';

export * as playlists from './playlists';
export * as playbackTimeline from './playbackTimeline';
export * as countdownTimer from './countdownTimer';

export type { Rng } from './playlists';

// Flat re-exports of the derivations folded in from `shared/position.ts` / `shared/timer.ts`.
export { derivePosition } from './playbackTimeline';
export { deriveTimeLeft, formatTimeLeft } from './countdownTimer';
