import { describe, expect, it } from 'vitest';
import type { PlaylistDto, PlaylistsDto, SongDto } from 'minstrel-shared/ui/state';
import { unusedSongs } from 'minstrel-shared/ui/unused';

const chime: SongDto = { id: 'chime', title: 'Chime', artist: 'Bell', length: 90, replayGain: null };
const dirge: SongDto = { id: 'dirge', title: 'Dirge', artist: 'Choir', length: 120, replayGain: null };
const reel: SongDto = { id: 'reel', title: 'Reel', artist: 'Fiddle', length: 150, replayGain: null };
const library: SongDto[] = [chime, dirge, reel];

function playlist(songs: SongDto[]): PlaylistDto {
	return { songs, currentIndex: null, gain: 1, resumeOffset: 0 };
}

function playlists(day: SongDto[], night: SongDto[]): PlaylistsDto {
	return { day: playlist(day), night: playlist(night) };
}

describe('unusedSongs', () => {
	it('returns the whole library when both playlists are empty', () => {
		const result = unusedSongs(library, playlists([], []));

		expect(result).toEqual([chime, dirge, reel]);
	});

	it('excludes a song that appears only in the Day playlist', () => {
		const result = unusedSongs(library, playlists([dirge], []));

		expect(result).toEqual([chime, reel]);
	});

	it('excludes a song that appears only in the Night playlist', () => {
		const result = unusedSongs(library, playlists([], [reel]));

		expect(result).toEqual([chime, dirge]);
	});

	it('excludes a song duplicated within one playlist exactly like a single occurrence', () => {
		const result = unusedSongs(library, playlists([chime, chime], []));

		expect(result).toEqual([dirge, reel]);
	});

	it('returns an empty list when every library song is in a playlist', () => {
		const result = unusedSongs(library, playlists([chime, dirge], [reel]));

		expect(result).toEqual([]);
	});
});
