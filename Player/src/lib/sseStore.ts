import { readable, type Readable } from 'svelte/store';
import { emptyState, type PlaybackState } from './state';

export function playbackStore(onGong?: () => void): Readable<PlaybackState> {
	return readable<PlaybackState>(emptyState, (set) => {
		const source = new EventSource('/sse');
		source.onmessage = (event) => set(JSON.parse(event.data) as PlaybackState);
		if (onGong) {
			source.addEventListener('gong', () => onGong());
		}
		return () => source.close();
	});
}
