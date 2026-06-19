import { describe, it, expect } from 'vitest';
import {
	createMusicStore,
	type AudioBytesStore,
	type SongMetadataStore,
	type SongRecord
} from '../src/lib/musicStore';

function fakeAudio(): AudioBytesStore {
	const bytes = new Map<string, ArrayBuffer>();
	return {
		put: async (id, data) => void bytes.set(id, data),
		get: async (id) => (bytes.has(id) ? new Blob([bytes.get(id)!]) : null)
	};
}

function fakeMetadata(): SongMetadataStore {
	const records = new Map<string, SongRecord>();
	return {
		put: async (record) => void records.set(record.id, record),
		all: async () => [...records.values()]
	};
}

function makeStore() {
	let persistCalls = 0;
	const store = createMusicStore(fakeAudio(), fakeMetadata(), async () => {
		persistCalls += 1;
		return true;
	});
	return { store, persistCalls: () => persistCalls };
}

const song = () => new File([new Uint8Array([1, 2, 3, 4])], 'overture.mp3');

describe('importSong', () => {
	it('records the imported file under the song list', async () => {
		const { store } = makeStore();

		await store.importSong(song());

		const songs = await store.songs();
		expect(songs).toHaveLength(1);
		expect(songs[0].name).toBe('overture.mp3');
	});

	it('requests persistence once across repeated imports', async () => {
		const { store, persistCalls } = makeStore();

		await store.importSong(song());
		await store.importSong(song());

		expect(persistCalls()).toBe(1);
	});
});

describe('audioBlob', () => {
	it('returns the bytes stored at import', async () => {
		const { store } = makeStore();

		const record = await store.importSong(song());

		const blob = await store.audioBlob(record.id);
		expect(blob).not.toBeNull();
		expect(await blob!.arrayBuffer()).toEqual(new Uint8Array([1, 2, 3, 4]).buffer);
	});

	it('returns null for a song whose bytes are absent', async () => {
		const { store } = makeStore();

		const blob = await store.audioBlob('never-imported');

		expect(blob).toBeNull();
	});
});
