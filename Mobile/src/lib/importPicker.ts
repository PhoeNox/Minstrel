// Device import edges. Folder import has two backends: desktop Chrome/Edge expose the
// File System Access directory picker (showDirectoryPicker), while mobile Chrome reaches a
// directory through an <input webkitdirectory>, which yields every file in the tree to be
// narrowed to audio here. iOS supports neither, so it falls back to the multi-file <input>
// / share target. All paths converge on File[] for the platform-agnostic MusicStore.

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

// Narrows a flat file list — as an <input webkitdirectory> delivers for a whole folder tree —
// down to the audio files the Library accepts.
export function audioFiles(files: Iterable<File>): File[] {
	return [...files].filter((file) => isAudio(file.name));
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
