import { readable, type Readable } from 'svelte/store';
import { emptyState, type PlaybackState } from './state';

export interface Connection {
	state: PlaybackState;
	connected: boolean;
}

const initial: Connection = { state: emptyState, connected: false };

export function playbackStore(): Readable<Connection> {
	return readable<Connection>(initial, (set) => {
		let state = emptyState;
		const source = new EventSource('/sse');
		source.onopen = () => set({ state, connected: true });
		source.onmessage = (event) => {
			state = JSON.parse(event.data) as PlaybackState;
			set({ state, connected: true });
		};
		source.onerror = () => set({ state, connected: false });
		return () => source.close();
	});
}
