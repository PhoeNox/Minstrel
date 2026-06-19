import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import * as playlists from '$shared/core/playlists';
import {
	IDLE_PLAYBACK,
	type GamePhase,
	type PlaybackState,
	type PlaylistEntry,
	type Track
} from '$shared/core';
import { entry } from '../fixtures';

function activePlaying(
	cursor: number,
	...ids: string[]
): { entries: PlaylistEntry[]; state: PlaybackState } {
	const entries = ids.map((id) => entry(id, 100));
	const state = playbackTimeline.playAt(
		{ ...IDLE_PLAYBACK, day: { cursor, gain: 1.0, resumeOffset: 0 } },
		ids[cursor],
		30,
		1000
	);
	return { entries, state };
}

const toTracks = (entries: PlaylistEntry[]): Track[] =>
	entries.map((e) => ({ id: e.id, length: e.length }));

function remove(
	entries: PlaylistEntry[],
	state: PlaybackState,
	phase: GamePhase,
	index: number,
	now: number
): { remaining: PlaylistEntry[]; state: PlaybackState } {
	const remaining = playlists.removeAt(entries, index);
	const reindexed = playbackTimeline.reindexAfterRemove(
		state,
		phase,
		index,
		entries.length,
		toTracks(remaining),
		now
	);
	return { remaining, state: reindexed };
}

describe('reindexAfterRemove', () => {
	it('removing before current shifts current down and keeps playing', () => {
		const { entries, state } = activePlaying(2, 'a', 'b', 'c');

		const { remaining, state: removed } = remove(entries, state, 'Day', 0, 2000);

		expect(remaining.map((e) => e.id)).toEqual(['b', 'c']);
		expect(removed.day.cursor).toBe(1);
		expect(removed.position.songId).toBe('c');
		expect(removed.position).toEqual(state.position);
	});

	it('removing after current leaves playback unchanged', () => {
		const { entries, state } = activePlaying(0, 'a', 'b', 'c');

		const { remaining, state: removed } = remove(entries, state, 'Day', 2, 2000);

		expect(remaining.map((e) => e.id)).toEqual(['a', 'b']);
		expect(removed.day.cursor).toBe(0);
		expect(removed.position.songId).toBe('a');
		expect(removed.position).toEqual(state.position);
	});

	it('removing current starts the slid-in song from zero', () => {
		const { entries, state } = activePlaying(1, 'a', 'b', 'c');

		const { remaining, state: removed } = remove(entries, state, 'Day', 1, 2000);

		expect(remaining.map((e) => e.id)).toEqual(['a', 'c']);
		expect(removed.day.cursor).toBe(1);
		expect(removed.position.songId).toBe('c');
		expect(removed.position.offset).toBe(0);
		expect(removed.position.isPlaying).toBe(true);
	});

	it('removing the current last entry wraps to the start', () => {
		const { entries, state } = activePlaying(2, 'a', 'b', 'c');

		const { remaining, state: removed } = remove(entries, state, 'Day', 2, 2000);

		expect(remaining.map((e) => e.id)).toEqual(['a', 'b']);
		expect(removed.day.cursor).toBe(0);
		expect(removed.position.songId).toBe('a');
		expect(removed.position.offset).toBe(0);
		expect(removed.position.isPlaying).toBe(true);
	});

	it('removing the last remaining song goes idle', () => {
		const { entries, state } = activePlaying(0, 'a');

		const { remaining, state: removed } = remove(entries, state, 'Day', 0, 2000);

		expect(remaining).toEqual([]);
		expect(removed.day.cursor).toBeNull();
		expect(removed.position.songId).toBeNull();
		expect(removed.position.isPlaying).toBe(false);
	});

	it('removing from the inactive phase repairs its current index without touching playback', () => {
		const night = ['x', 'y', 'z'].map((id) => entry(id, 100));
		const state = playbackTimeline.playAt(
			{
				...IDLE_PLAYBACK,
				day: { cursor: 0, gain: 1.0, resumeOffset: 0 },
				night: { cursor: 2, gain: 1.0, resumeOffset: 0 }
			},
			'a',
			30,
			1000
		);

		const { remaining, state: removed } = remove(night, state, 'Night', 0, 2000);

		expect(remaining.map((e) => e.id)).toEqual(['y', 'z']);
		expect(removed.night.cursor).toBe(1);
		expect(removed.position).toEqual(state.position);
	});

	it('ignores an out-of-range index', () => {
		const { entries, state } = activePlaying(0, 'a', 'b');

		const { state: removed } = remove(entries, state, 'Day', 5, 2000);

		expect(removed).toEqual(state);
	});
});
