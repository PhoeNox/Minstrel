import type { SongDto } from 'minstrel-shared/ui/state';

export async function fetchLibrary(): Promise<SongDto[]> {
	const response = await fetch('/library');
	return (await response.json()) as SongDto[];
}
