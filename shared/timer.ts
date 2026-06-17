import { readable, type Readable } from 'svelte/store';

export interface TimerAnchor {
	running: boolean;
	anchorTimestamp: number;
	durationLeftAtAnchor: number;
}

export function timerStore(onGong?: (gain: number) => void): Readable<TimerAnchor | null> {
	return readable<TimerAnchor | null>(null, (set) => {
		const source = new EventSource('/timer/sse');
		source.onmessage = (event) => set(JSON.parse(event.data) as TimerAnchor | null);
		if (onGong) {
			source.addEventListener('gong', (event) => onGong(JSON.parse(event.data).gain as number));
		}
		return () => source.close();
	});
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
