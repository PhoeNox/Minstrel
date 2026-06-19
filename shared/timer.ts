import { readable, type Readable } from 'svelte/store';
import type { TimerAnchor } from './core';

// The timer SSE subscription is a transport concern, not a domain derivation, so it
// stays here; the pure `deriveTimeLeft`/`formatTimeLeft` live in `core`.
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
