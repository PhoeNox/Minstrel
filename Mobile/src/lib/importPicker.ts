// Device import edges. Android exposes the File System Access directory picker, so a
// whole collection imports in one action; iOS has no folder access, so it falls back
// to the multi-file <input> / share target. Both converge on File[] for the
// platform-agnostic MusicStore.

const AUDIO_EXTENSIONS = ['.mp3', '.m4a', '.aac', '.ogg', '.oga', '.opus', '.flac', '.wav'];

interface DirectoryPickerWindow {
	showDirectoryPicker?: () => Promise<FileSystemDirectoryHandle>;
}

export function supportsDirectoryPicker(): boolean {
	return typeof (window as DirectoryPickerWindow).showDirectoryPicker === 'function';
}

export async function pickDirectoryFiles(): Promise<File[]> {
	const picker = (window as DirectoryPickerWindow).showDirectoryPicker!;
	const handle = await picker();
	return collectAudioFiles(handle);
}

async function collectAudioFiles(dir: FileSystemDirectoryHandle): Promise<File[]> {
	const files: File[] = [];
	for await (const entry of dir.values()) {
		if (entry.kind === 'file') {
			if (isAudio(entry.name)) files.push(await entry.getFile());
		} else {
			files.push(...(await collectAudioFiles(entry)));
		}
	}
	return files;
}

function isAudio(name: string): boolean {
	const lower = name.toLowerCase();
	return AUDIO_EXTENSIONS.some((extension) => lower.endsWith(extension));
}
