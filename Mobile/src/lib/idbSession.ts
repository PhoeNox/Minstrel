import type { PersistedSession, SessionMetadataStore } from './musicStore';
import { openMinstrelDb, promisify, SESSION_STORE } from './idb';

// There is one session, so its snapshot lives under a single fixed key.
const KEY = 'current';

// The persisted session (both Playlists, each phase's Current Entry and Gain, the active
// phase) in IndexedDB — restored on app start so curation survives a restart (ADR-0008).
export function idbSession(): SessionMetadataStore {
	return {
		async load() {
			const db = await openMinstrelDb();
			const session = await promisify<PersistedSession | undefined>(
				db.transaction(SESSION_STORE, 'readonly').objectStore(SESSION_STORE).get(KEY)
			);
			db.close();
			return session ?? null;
		},
		async save(session) {
			const db = await openMinstrelDb();
			await promisify(
				db.transaction(SESSION_STORE, 'readwrite').objectStore(SESSION_STORE).put(session, KEY)
			);
			db.close();
		}
	};
}
