import { describe, expect, it } from 'vitest';
import * as playbackTimeline from '$shared/core/playbackTimeline';
import type { PositionAnchor } from '$shared/core';

describe('derivePosition', () => {
	it('advances with elapsed time while playing', () => {
		const anchor: PositionAnchor = {
			songId: 'song-1',
			offset: 10,
			anchorTimestamp: 2000,
			isPlaying: true
		};

		expect(playbackTimeline.derivePosition(anchor, 4500)).toBe(12.5);
	});

	it('stays at the offset while paused', () => {
		const anchor: PositionAnchor = {
			songId: 'song-1',
			offset: 10,
			anchorTimestamp: 2000,
			isPlaying: false
		};

		expect(playbackTimeline.derivePosition(anchor, 4500)).toBe(10);
	});

	it('is exactly the offset at the anchor instant', () => {
		const anchor: PositionAnchor = {
			songId: 'song-1',
			offset: 7,
			anchorTimestamp: 5000,
			isPlaying: true
		};

		expect(playbackTimeline.derivePosition(anchor, 5000)).toBe(7);
	});
});
