// Title/Artist/Length read from an audio file at import — the JS-tag-parser
// replacement for TagLibSharp (FileSystem/), which cannot run client-side. The
// concrete music-metadata call is the only edge, kept behind TagParser so tests
// inject a deterministic fake instead of decoding real audio.
export interface SongTags {
	title: string;
	artist: string;
	length: number;
}

export interface TagParser {
	read(file: File): Promise<SongTags>;
}

// music-metadata is loaded lazily so its decoder stays out of the app-shell bundle
// and never enters the test build (the fake parser replaces it under Vitest).
export function musicMetadataParser(): TagParser {
	return {
		async read(file) {
			const { parseBlob } = await import('music-metadata');
			const metadata = await parseBlob(file);
			return {
				title: metadata.common.title ?? stripExtension(file.name),
				artist: metadata.common.artist ?? 'Unknown Artist',
				length: metadata.format.duration ?? 0
			};
		}
	};
}

function stripExtension(name: string): string {
	const dot = name.lastIndexOf('.');
	return dot > 0 ? name.slice(0, dot) : name;
}
