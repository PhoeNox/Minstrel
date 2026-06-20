// The single IndexedDB database both the Library index and the session snapshot hang
// off. One opener owns the schema so every store is created in one upgrade — opening the
// same database under two openers with diverging schemas would race the version.
const DB_NAME = 'minstrel';
const DB_VERSION = 2;

export const SONGS_STORE = 'songs';
export const SESSION_STORE = 'session';

export function openMinstrelDb(): Promise<IDBDatabase> {
	return new Promise((resolve, reject) => {
		const request = indexedDB.open(DB_NAME, DB_VERSION);
		request.onupgradeneeded = () => {
			const db = request.result;
			if (!db.objectStoreNames.contains(SONGS_STORE)) {
				db.createObjectStore(SONGS_STORE, { keyPath: 'id' });
			}
			if (!db.objectStoreNames.contains(SESSION_STORE)) {
				db.createObjectStore(SESSION_STORE);
			}
		};
		request.onsuccess = () => resolve(request.result);
		request.onerror = () => reject(request.error);
	});
}

export function promisify<T>(request: IDBRequest<T>): Promise<T> {
	return new Promise((resolve, reject) => {
		request.onsuccess = () => resolve(request.result);
		request.onerror = () => reject(request.error);
	});
}
