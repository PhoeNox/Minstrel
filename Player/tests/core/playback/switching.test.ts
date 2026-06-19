import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import { FADE_SECONDS } from '$shared/core/playbackTimeline';
import { IDLE_PLAYBACK, IDLE_POSITION, type PlaybackState } from '$shared/core';
import { book, entry, selectIn, switchPhase } from '../fixtures';

const withPlaylists = () =>
	book(
		[entry('day-1', 10), entry('day-2', 10)],
		[entry('night-1', 10), entry('night-2', 10)]
	);

const started = (): PlaybackState =>
	playbackTimeline.play(
		{
			...IDLE_PLAYBACK,
			day: { cursor: 0, gain: 1.0, resumeOffset: 0 },
			night: { cursor: 0, gain: 1.0, resumeOffset: 0 }
		},
		'day-1',
		1000
	);

describe('switchPhase', () => {
	it('flips the phase and plays the other playlist current song', () => {
		const result = switchPhase(withPlaylists(), started(), 2000);

		expect(result.activePhase).toBe('Night');
		expect(result.position.songId).toBe('night-1');
		expect(result.position.isPlaying).toBe(true);
		expect(result.position.offset).toBe(0);
		expect(result.position.anchorTimestamp).toBe(2000);
	});

	it('switching back returns to the starting phase current song', () => {
		const source = withPlaylists();

		const switched = switchPhase(source, started(), 2000);
		const back = switchPhase(source, switched, 3000);

		expect(back.activePhase).toBe('Day');
		expect(back.position.songId).toBe('day-1');
	});

	it('switching back resumes the song at the position left plus the fadeout', () => {
		const source = withPlaylists();

		const switched = switchPhase(source, started(), 4000);
		const back = switchPhase(source, switched, 9000);

		expect(back.position.songId).toBe('day-1');
		expect(back.position.offset).toBe(3 + FADE_SECONDS);
		expect(back.position.anchorTimestamp).toBe(9000);
		expect(back.position.isPlaying).toBe(true);
	});

	it('caps the resume offset at the song length', () => {
		const source = withPlaylists();

		const switched = switchPhase(source, started(), 9000);
		const back = switchPhase(source, switched, 12000);

		expect(back.position.offset).toBe(10);
	});

	it('selecting another song in the idle phase resets its resume offset', () => {
		const source = withPlaylists();

		const switched = switchPhase(source, started(), 4000);
		const reselected = selectIn(source, switched, 'Day', 1, 6000);
		const back = switchPhase(source, reselected, 9000);

		expect(back.position.songId).toBe('day-2');
		expect(back.position.offset).toBe(0);
	});

	it('switching to an empty phase flips the phase and goes idle', () => {
		const source = book([entry('day-1', 10), entry('day-2', 10)], []);
		const state = playbackTimeline.play(
			{ ...IDLE_PLAYBACK, day: { cursor: 0, gain: 1.0, resumeOffset: 0 } },
			'day-1',
			1000
		);

		const result = switchPhase(source, state, 2000);

		expect(result.activePhase).toBe('Night');
		expect(result.position.songId).toBeNull();
		expect(result.position.isPlaying).toBe(false);
		expect(result.position).toEqual(IDLE_POSITION);
	});

	it('switching back from an empty phase resumes the departed song at the position left plus the fadeout', () => {
		const source = book([entry('day-1', 10), entry('day-2', 10)], []);
		const state = playbackTimeline.play(
			{ ...IDLE_PLAYBACK, day: { cursor: 0, gain: 1.0, resumeOffset: 0 } },
			'day-1',
			1000
		);

		const toEmpty = switchPhase(source, state, 4000);
		const back = switchPhase(source, toEmpty, 9000);

		expect(back.activePhase).toBe('Day');
		expect(back.position.songId).toBe('day-1');
		expect(back.position.offset).toBe(3 + FADE_SECONDS);
		expect(back.position.anchorTimestamp).toBe(9000);
		expect(back.position.isPlaying).toBe(true);
	});

	it('switching from an empty active phase plays the target current song', () => {
		const source = book([], [entry('night-1', 10)]);
		const state: PlaybackState = {
			...IDLE_PLAYBACK,
			night: { cursor: 0, gain: 1.0, resumeOffset: 0 }
		};

		const result = switchPhase(source, state, 2000);

		expect(result.activePhase).toBe('Night');
		expect(result.position.songId).toBe('night-1');
		expect(result.position.isPlaying).toBe(true);
	});

	it('flips the phase when both playlists are empty', () => {
		const source = book([], []);

		const result = switchPhase(source, IDLE_PLAYBACK, 2000);

		expect(result.activePhase).toBe('Night');
		expect(result.position.songId).toBeNull();
		expect(result.position.isPlaying).toBe(false);
	});
});
