import type { SongMetadataStore, SongRecord } from './musicStore';
import { openMinstrelDb, promisify, SONGS_STORE } from './idb';

// Song metadata in IndexedDB, the durable index the OPFS bytes hang off.
export function idbSongMetadata(): SongMetadataStore {
	return {
		async put(record) {
			const db = await openMinstrelDb();
			await promisify(db.transaction(SONGS_STORE, 'readwrite').objectStore(SONGS_STORE).put(record));
			db.close();
		},
		async all() {
			const db = await openMinstrelDb();
			const records = await promisify<SongRecord[]>(
				db.transaction(SONGS_STORE, 'readonly').objectStore(SONGS_STORE).getAll()
			);
			db.close();
			return records;
		},
		async delete(id) {
			const db = await openMinstrelDb();
			await promisify(db.transaction(SONGS_STORE, 'readwrite').objectStore(SONGS_STORE).delete(id));
			db.close();
		}
	};
}
