import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import { IDLE_PLAYBACK } from '$shared/core';

describe('play', () => {
	it('sets the current song and starts playing', () => {
		const result = playbackTimeline.play(IDLE_PLAYBACK, 'song-1', 1000);

		expect(result.position.songId).toBe('song-1');
		expect(result.position.isPlaying).toBe(true);
	});

	it('anchors the new song at offset zero', () => {
		const result = playbackTimeline.play(IDLE_PLAYBACK, 'song-1', 1000);

		expect(result.position.offset).toBe(0);
		expect(result.position.anchorTimestamp).toBe(1000);
	});

	it('replaces the previous current song', () => {
		const playing = playbackTimeline.play(IDLE_PLAYBACK, 'song-1', 1000);

		const result = playbackTimeline.play(playing, 'song-2', 5000);

		expect(result.position.songId).toBe('song-2');
		expect(result.position.offset).toBe(0);
		expect(result.position.isPlaying).toBe(true);
	});
});
