import type { AudioBytesStore } from './musicStore';

const AUDIO_DIR = 'audio';

async function audioDir(): Promise<FileSystemDirectoryHandle> {
	const root = await navigator.storage.getDirectory();
	return root.getDirectoryHandle(AUDIO_DIR, { create: true });
}

// Audio bytes in the Origin Private File System, one file per song id. A missing file
// (eviction or never-imported) surfaces as null rather than an error.
export function opfsAudioBytes(): AudioBytesStore {
	return {
		async put(id, bytes) {
			const dir = await audioDir();
			const handle = await dir.getFileHandle(id, { create: true });
			const writable = await handle.createWritable();
			await writable.write(bytes);
			await writable.close();
		},
		async get(id) {
			const dir = await audioDir();
			try {
				const handle = await dir.getFileHandle(id);
				return await handle.getFile();
			} catch {
				return null;
			}
		}
	};
}
