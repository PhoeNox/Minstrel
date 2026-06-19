// Pure countdown transforms — the TS mirror of C# `Core.Timer.CountdownTimer`.
// Durations are seconds (the C# `TimeSpan` collapses to its total seconds on the wire).

import type { TimerAnchor } from './types';

export function start(durationSeconds: number, now: number): TimerAnchor {
	return { running: true, anchorTimestamp: now, durationLeftAtAnchor: durationSeconds };
}

export function deriveTimeLeft(anchor: TimerAnchor, now: number): number {
	return anchor.running
		? Math.max(0, anchor.durationLeftAtAnchor - (now - anchor.anchorTimestamp) / 1000)
		: anchor.durationLeftAtAnchor;
}

export function hasExpired(anchor: TimerAnchor, now: number): boolean {
	return anchor.running && deriveTimeLeft(anchor, now) <= 0;
}

export function formatTimeLeft(seconds: number): string {
	const total = Math.ceil(seconds);
	const minutes = Math.floor(total / 60);
	const remainder = total % 60;
	return `${minutes}:${remainder.toString().padStart(2, '0')}`;
}
