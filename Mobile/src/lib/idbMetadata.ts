import type { SongMetadataStore, SongRecord } from './musicStore';

const DB_NAME = 'minstrel';
const DB_VERSION = 1;
const STORE = 'songs';

function openDb(): Promise<IDBDatabase> {
	return new Promise((resolve, reject) => {
		const request = indexedDB.open(DB_NAME, DB_VERSION);
		request.onupgradeneeded = () => request.result.createObjectStore(STORE, { keyPath: 'id' });
		request.onsuccess = () => resolve(request.result);
		request.onerror = () => reject(request.error);
	});
}

function promisify<T>(request: IDBRequest<T>): Promise<T> {
	return new Promise((resolve, reject) => {
		request.onsuccess = () => resolve(request.result);
		request.onerror = () => reject(request.error);
	});
}

// Song metadata in IndexedDB, the durable index the OPFS bytes hang off.
export function idbSongMetadata(): SongMetadataStore {
	return {
		async put(record) {
			const db = await openDb();
			await promisify(db.transaction(STORE, 'readwrite').objectStore(STORE).put(record));
			db.close();
		},
		async all() {
			const db = await openDb();
			const records = await promisify<SongRecord[]>(
				db.transaction(STORE, 'readonly').objectStore(STORE).getAll()
			);
			db.close();
			return records;
		}
	};
}
