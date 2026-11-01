import { describe, expect, it } from 'vitest';
import type { PlaylistEntry } from 'minstrel-shared/core';
import { sessionWithReplayGain, withReplayGain } from '../src/lib/legacyReplayGain';
import type { PersistedSession, SongRecord } from '../src/lib/musicStore';

// Shapes as stored before ReplayGain was read: the field is absent, not null.
const legacyRecord = {
	id: 'a',
	title: 'Title a',
	artist: 'Artist a',
	length: 100,
	name: 'a.mp3',
	importedAt: 0
} as SongRecord;

const legacyEntry = { id: 'a', path: 'a.mp3', title: 'Title a', artist: 'Artist a', length: 100 } as PlaylistEntry;

const legacySession: PersistedSession = {
	activePhase: 'Day',
	day: { entries: [legacyEntry], cursor: 0, gain: 1 },
	night: { entries: [legacyEntry], cursor: null, gain: 0.5 }
};

describe('withReplayGain', () => {
	it('reads a missing ReplayGain as null', () => {
		const record = withReplayGain(legacyRecord);

		expect(record.replayGain).toBeNull();
	});

	it('keeps a stored ReplayGain', () => {
		const record = withReplayGain({ ...legacyRecord, replayGain: -6.5 });

		expect(record.replayGain).toBe(-6.5);
	});
});

describe('sessionWithReplayGain', () => {
	it('reads a missing ReplayGain as null on the entries of both Playlists', () => {
		const session = sessionWithReplayGain(legacySession);

		expect(session.day.entries[0].replayGain).toBeNull();
		expect(session.night.entries[0].replayGain).toBeNull();
	});

	it('keeps the rest of the persisted session', () => {
		const session = sessionWithReplayGain(legacySession);

		expect(session.activePhase).toBe('Day');
		expect(session.day.cursor).toBe(0);
		expect(session.night.gain).toBe(0.5);
	});
});
