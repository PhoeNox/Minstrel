import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import { IDLE_PLAYBACK, type PlaybackState, type Track } from '$shared/core';
import { tracks } from '../fixtures';

function playing(...songs: [string, number][]): { songs: Track[]; state: PlaybackState } {
	const list = tracks(...songs);
	const state = playbackTimeline.select(
		{ ...IDLE_PLAYBACK, day: { cursor: 0, gain: 1.0, resumeOffset: 0 } },
		'Day',
		0,
		list[0],
		1000
	);
	return { songs: list, state };
}

describe('advance (via tick at song end)', () => {
	it('advances to the next song when the current one ends', () => {
		const { songs, state } = playing(['song-1', 10], ['song-2', 10]);

		const ticked = playbackTimeline.tick(state, songs, 12000);

		expect(ticked.position.songId).toBe('song-2');
		expect(ticked.day.cursor).toBe(1);
		expect(ticked.position.offset).toBe(0);
		expect(ticked.position.isPlaying).toBe(true);
	});

	it('advances by occurrence when the same song appears twice', () => {
		const { songs, state } = playing(['song-1', 10], ['dup', 10], ['dup', 10]);
		const onFirstDup = playbackTimeline.select(state, 'Day', 1, songs[1], 2000);

		const ticked = playbackTimeline.tick(onFirstDup, songs, 13000);

		expect(ticked.position.songId).toBe('dup');
		expect(ticked.day.cursor).toBe(2);

		const wrapped = playbackTimeline.tick(ticked, songs, 24000);

		expect(wrapped.day.cursor).toBe(0);
		expect(wrapped.position.songId).toBe('song-1');
	});

	it('does not advance before the current song ends', () => {
		const { songs, state } = playing(['song-1', 10], ['song-2', 10]);

		const ticked = playbackTimeline.tick(state, songs, 6000);

		expect(ticked.position.songId).toBe('song-1');
		expect(ticked.position.offset).toBe(5);
	});

	it('wraps to the first song at the end of the playlist', () => {
		const { songs, state } = playing(['song-1', 10], ['song-2', 10]);
		const onLast = playbackTimeline.select(state, 'Day', 1, songs[1], 2000);

		const ticked = playbackTimeline.tick(onLast, songs, 13000);

		expect(ticked.position.songId).toBe('song-1');
	});

	it('wraps to the same song when the playlist has one song', () => {
		const { songs, state } = playing(['song-1', 10]);

		const ticked = playbackTimeline.tick(state, songs, 12000);

		expect(ticked.position.songId).toBe('song-1');
		expect(ticked.position.offset).toBe(0);
	});

	it('reports a jump when a single song loops', () => {
		const { songs, state } = playing(['song-1', 10]);

		const ticked = playbackTimeline.tick(state, songs, 12000);

		expect(playbackTimeline.playbackJumped(state, ticked, 12000)).toBe(true);
	});

	it('reports no jump while the song is still playing', () => {
		const { songs, state } = playing(['song-1', 10]);

		const ticked = playbackTimeline.tick(state, songs, 6000);

		expect(playbackTimeline.playbackJumped(state, ticked, 6000)).toBe(false);
	});
});
