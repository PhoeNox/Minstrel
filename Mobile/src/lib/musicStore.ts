import { opfsAudioBytes } from './opfsAudio';
import { idbSongMetadata } from './idbMetadata';

// Library metadata for one imported song. Deliberately minimal for the tracer slice:
// Title/Artist/Length parsing lands with the Library epic; here a song is just its
// file name and an id (ADR-0008).
export interface SongRecord {
	id: string;
	name: string;
	importedAt: number;
}

// Audio bytes live in OPFS, keyed by song id. get returns null when the bytes are
// missing — the eviction signal the durability story rests on.
export interface AudioBytesStore {
	put(id: string, bytes: ArrayBuffer): Promise<void>;
	get(id: string): Promise<Blob | null>;
}

// Song metadata lives in IndexedDB, the durable index over the OPFS bytes.
export interface SongMetadataStore {
	put(record: SongRecord): Promise<void>;
	all(): Promise<SongRecord[]>;
}

export interface MusicStore {
	importSong(file: File): Promise<SongRecord>;
	songs(): Promise<SongRecord[]>;
	audioBlob(id: string): Promise<Blob | null>;
}

// Coordinator over the two storage edges. Pure of any concrete storage API so it runs
// against in-memory fakes in tests; the browser wiring is browserMusicStore below.
export function createMusicStore(
	audio: AudioBytesStore,
	metadata: SongMetadataStore,
	requestPersistence: () => Promise<unknown>
): MusicStore {
	let persistenceRequested = false;

	return {
		async importSong(file) {
			if (!persistenceRequested) {
				persistenceRequested = true;
				await requestPersistence();
			}
			const record: SongRecord = {
				id: crypto.randomUUID(),
				name: file.name,
				importedAt: Date.now()
			};
			await audio.put(record.id, await file.arrayBuffer());
			await metadata.put(record);
			return record;
		},
		songs: () => metadata.all(),
		audioBlob: (id) => audio.get(id)
	};
}

// Browser wiring: OPFS bytes, IndexedDB metadata, and persistence-as-cache durability.
export function browserMusicStore(): MusicStore {
	return createMusicStore(opfsAudioBytes(), idbSongMetadata(), () =>
		navigator.storage?.persist?.() ?? Promise.resolve(false)
	);
}
