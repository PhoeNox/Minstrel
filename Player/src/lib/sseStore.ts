import { readable, type Readable } from 'svelte/store';
import { emptyState, type PlaybackState } from './state';

export function playbackStore(onGong?: (gain: number) => void): Readable<PlaybackState> {
	return readable<PlaybackState>(emptyState, (set) => {
		const source = new EventSource('/sse');
		source.onmessage = (event) => set(JSON.parse(event.data) as PlaybackState);
		if (onGong) {
			source.addEventListener('gong', (event) => onGong(JSON.parse(event.data).gain as number));
		}
		return () => source.close();
	});
}
