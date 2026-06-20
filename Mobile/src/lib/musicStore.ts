import { opfsAudioBytes } from './opfsAudio';
import { idbSongMetadata } from './idbMetadata';
import { idbSession } from './idbSession';
import { musicMetadataParser, type TagParser } from './metadata';
import { deriveSongId } from './songId';
import { findEvicted, partitionNew } from './library';
import type { GamePhase, PlaylistEntry } from '$shared/core';

// Library metadata for one imported song: a stable content id, the Title/Artist/Length
// read at import, plus the source file name and import time (ADR-0008). Superset of the
// `SongDto` the shared Library UI binds to.
export interface SongRecord {
	id: string;
	title: string;
	artist: string;
	length: number;
	name: string;
	importedAt: number;
}

// What one import action added versus skipped as already present — lets the surface
// report "N added, M already present" and makes re-import (after eviction) visibly a
// no-grow restore.
export interface ImportResult {
	added: SongRecord[];
	duplicates: SongRecord[];
}

// Audio bytes live in OPFS, keyed by song id. get returns null when the bytes are
// missing; ids enumerates what is actually stored, the input to eviction detection.
export interface AudioBytesStore {
	put(id: string, bytes: ArrayBuffer): Promise<void>;
	get(id: string): Promise<Blob | null>;
	delete(id: string): Promise<void>;
	ids(): Promise<string[]>;
}

// Song metadata lives in IndexedDB, the durable index over the OPFS bytes.
export interface SongMetadataStore {
	put(record: SongRecord): Promise<void>;
	all(): Promise<SongRecord[]>;
	delete(id: string): Promise<void>;
}

// One Playlist as persisted: its denormalized entries plus the phase's Current Entry and
// Gain (ADR-0008). The entries carry their own Title/Artist/Length so a restart restores
// the Playlist without re-resolving against the Library.
export interface PersistedPlaylist {
	entries: PlaylistEntry[];
	cursor: number | null;
	gain: number;
}

// The whole session as persisted: both Playlists and the active Game Phase. Position is
// playback state (slice 6), not curation, so it is not persisted.
export interface PersistedSession {
	activePhase: GamePhase;
	day: PersistedPlaylist;
	night: PersistedPlaylist;
}

// The single persisted session snapshot lives in IndexedDB alongside the Library index.
export interface SessionMetadataStore {
	load(): Promise<PersistedSession | null>;
	save(session: PersistedSession): Promise<void>;
}

export interface MusicStore {
	importFiles(files: File[]): Promise<ImportResult>;
	songs(): Promise<SongRecord[]>;
	remove(id: string): Promise<void>;
	evictedSongs(): Promise<SongRecord[]>;
	audioBlob(id: string): Promise<Blob | null>;
	loadSession(): Promise<PersistedSession | null>;
	saveSession(session: PersistedSession): Promise<void>;
}

interface Candidate {
	record: SongRecord;
	bytes: ArrayBuffer;
}

// Coordinator over the storage and tag-parser edges. Pure of any concrete browser API
// so it runs against in-memory fakes in tests; the browser wiring is browserMusicStore
// below.
export function createMusicStore(
	audio: AudioBytesStore,
	metadata: SongMetadataStore,
	session: SessionMetadataStore,
	tags: TagParser,
	requestPersistence: () => Promise<unknown>
): MusicStore {
	let persistenceRequested = false;

	async function ensurePersistence(): Promise<void> {
		if (persistenceRequested) return;
		persistenceRequested = true;
		await requestPersistence();
	}

	async function readCandidate(file: File): Promise<Candidate> {
		const bytes = await file.arrayBuffer();
		const id = await deriveSongId(bytes);
		const { title, artist, length } = await tags.read(file);
		return { record: { id, title, artist, length, name: file.name, importedAt: Date.now() }, bytes };
	}

	return {
		async importFiles(files) {
			if (files.length === 0) return { added: [], duplicates: [] };
			await ensurePersistence();

			const candidates = await Promise.all(files.map(readCandidate));
			const byId = new Map(candidates.map((candidate) => [candidate.record.id, candidate.bytes]));
			const existing = await metadata.all();
			const { added, duplicates } = partitionNew(
				new Set(existing.map((record) => record.id)),
				candidates.map((candidate) => candidate.record)
			);

			// Bytes are written for every imported file — duplicates restore evicted bytes
			// under the same id; metadata is written only for genuinely new songs.
			for (const record of [...added, ...duplicates]) await audio.put(record.id, byId.get(record.id)!);
			for (const record of added) await metadata.put(record);

			return { added, duplicates };
		},
		songs: () => metadata.all(),
		async remove(id) {
			await audio.delete(id);
			await metadata.delete(id);
		},
		async evictedSongs() {
			const records = await metadata.all();
			const present = new Set(await audio.ids());
			return findEvicted(records, present);
		},
		audioBlob: (id) => audio.get(id),
		loadSession: () => session.load(),
		saveSession: (snapshot) => session.save(snapshot)
	};
}

// Browser wiring: OPFS bytes, IndexedDB metadata, music-metadata tags, and
// persistence-as-cache durability.
export function browserMusicStore(): MusicStore {
	return createMusicStore(
		opfsAudioBytes(),
		idbSongMetadata(),
		idbSession(),
		musicMetadataParser(),
		() => navigator.storage?.persist?.() ?? Promise.resolve(false)
	);
}
