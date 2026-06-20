import { readable, type Readable } from 'svelte/store';
import { emptyState, type PlaybackState } from '$shared/ui/state';
import type { Snapshot } from '$shared/ui/minstrelApi';

const initial: Snapshot = { state: emptyState, connected: false };

export function playbackStore(): Readable<Snapshot> {
	return readable<Snapshot>(initial, (set) => {
		let state = emptyState;
		const source = new EventSource('/playback/sse');
		source.onopen = () => set({ state, connected: true });
		source.onmessage = (event) => {
			state = JSON.parse(event.data) as PlaybackState;
			set({ state, connected: true });
		};
		source.onerror = () => set({ state, connected: false });
		return () => source.close();
	});
}
