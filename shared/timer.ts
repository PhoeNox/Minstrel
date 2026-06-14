export interface TimerAnchor {
	running: boolean;
	anchorTimestamp: number;
	durationLeftAtAnchor: number;
}

export function deriveTimeLeft(anchor: TimerAnchor, now: number): number {
	return anchor.running
		? Math.max(0, anchor.durationLeftAtAnchor - (now - anchor.anchorTimestamp) / 1000)
		: anchor.durationLeftAtAnchor;
}

export function formatTimeLeft(seconds: number): string {
	const total = Math.ceil(seconds);
	const minutes = Math.floor(total / 60);
	const remainder = total % 60;
	return `${minutes}:${remainder.toString().padStart(2, '0')}`;
}
