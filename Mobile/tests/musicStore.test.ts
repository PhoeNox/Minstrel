import { describe, it, expect } from 'vitest';
import {
	createMusicStore,
	type AudioBytesStore,
	type MusicStore,
	type PersistedSession,
	type SessionMetadataStore,
	type SongMetadataStore,
	type SongRecord
} from '../src/lib/musicStore';
import type { TagParser } from '../src/lib/metadata';

function fakeAudio(): AudioBytesStore {
	const bytes = new Map<string, ArrayBuffer>();
	return {
		put: async (id, data) => void bytes.set(id, data),
		get: async (id) => (bytes.has(id) ? new Blob([bytes.get(id)!]) : null),
		delete: async (id) => void bytes.delete(id),
		ids: async () => [...bytes.keys()]
	};
}

function fakeMetadata(): SongMetadataStore {
	const records = new Map<string, SongRecord>();
	return {
		put: async (record) => void records.set(record.id, record),
		all: async () => [...records.values()],
		delete: async (id) => void records.delete(id)
	};
}

// Tags keyed off the file name so each fixture has deterministic, distinct metadata
// without decoding real audio.
function fakeTags(): TagParser {
	return {
		read: async (file) => ({ title: `Title of ${file.name}`, artist: `Artist of ${file.name}`, length: 123 })
	};
}

function fakeSession(): SessionMetadataStore {
	let saved: PersistedSession | null = null;
	return {
		load: async () => saved,
		save: async (session) => void (saved = session)
	};
}

function makeStore(audio: AudioBytesStore = fakeAudio()): {
	store: MusicStore;
	audio: AudioBytesStore;
	persistCalls: () => number;
} {
	let persistCalls = 0;
	const store = createMusicStore(audio, fakeMetadata(), fakeSession(), fakeTags(), async () => {
		persistCalls += 1;
		return true;
	});
	return { store, audio, persistCalls: () => persistCalls };
}

const file = (name = 'overture.mp3', bytes = [1, 2, 3, 4]) =>
	new File([new Uint8Array(bytes)], name);

describe('importFiles', () => {
	it('records each imported file with its parsed metadata', async () => {
		const { store } = makeStore();

		await store.importFiles([file('overture.mp3'), file('dirge.mp3', [5, 6, 7])]);

		const songs = await store.songs();
		expect(songs).toHaveLength(2);
		expect(songs.map((song) => song.title).sort()).toEqual([
			'Title of dirge.mp3',
			'Title of overture.mp3'
		]);
		expect(songs[0].length).toBe(123);
	});

	it('imports a whole folder of files in one action', async () => {
		const { store } = makeStore();

		const result = await store.importFiles([
			file('a.mp3', [1]),
			file('b.mp3', [2]),
			file('c.mp3', [3])
		]);

		expect(result.added).toHaveLength(3);
		expect(await store.songs()).toHaveLength(3);
	});

	it('does not duplicate songs when the same files are re-imported', async () => {
		const { store } = makeStore();
		await store.importFiles([file('overture.mp3', [1, 2, 3, 4])]);

		const result = await store.importFiles([file('overture.mp3', [1, 2, 3, 4])]);

		expect(result.added).toHaveLength(0);
		expect(result.duplicates).toHaveLength(1);
		expect(await store.songs()).toHaveLength(1);
	});

	it('requests persistence once across repeated imports', async () => {
		const { store, persistCalls } = makeStore();

		await store.importFiles([file()]);
		await store.importFiles([file('second.mp3', [9])]);

		expect(persistCalls()).toBe(1);
	});

	it('does not request persistence for an empty import', async () => {
		const { store, persistCalls } = makeStore();

		await store.importFiles([]);

		expect(persistCalls()).toBe(0);
	});
});

describe('remove', () => {
	it('drops the song from the list and its stored bytes', async () => {
		const { store } = makeStore();
		const [record] = (await store.importFiles([file()])).added;

		await store.remove(record.id);

		expect(await store.songs()).toHaveLength(0);
		expect(await store.audioBlob(record.id)).toBeNull();
	});
});

describe('evictedSongs', () => {
	it('reports a known song whose audio bytes have gone', async () => {
		const audio = fakeAudio();
		const { store } = makeStore(audio);
		const [record] = (await store.importFiles([file()])).added;

		await audio.delete(record.id);

		const evicted = await store.evictedSongs();
		expect(evicted.map((song) => song.id)).toEqual([record.id]);
	});

	it('reports nothing while every song still has its bytes', async () => {
		const { store } = makeStore();
		await store.importFiles([file()]);

		expect(await store.evictedSongs()).toHaveLength(0);
	});

	it('clears the eviction once the same file is re-imported', async () => {
		const audio = fakeAudio();
		const { store } = makeStore(audio);
		const [record] = (await store.importFiles([file('overture.mp3', [1, 2, 3, 4])])).added;
		await audio.delete(record.id);

		await store.importFiles([file('overture.mp3', [1, 2, 3, 4])]);

		expect(await store.evictedSongs()).toHaveLength(0);
		expect(await store.audioBlob(record.id)).not.toBeNull();
	});
});

describe('audioBlob', () => {
	it('returns the bytes stored at import', async () => {
		const { store } = makeStore();

		const [record] = (await store.importFiles([file('overture.mp3', [1, 2, 3, 4])])).added;

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
