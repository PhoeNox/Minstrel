import { describe, expect, it } from 'vitest';
import { get } from 'svelte/store';
import { deriveTimeLeft, type Rng } from '$shared/core';
import { createLocalSession, type SessionStore } from '../src/lib/localSession';
import type { PersistedSession, SongRecord } from '../src/lib/musicStore';

// A SessionStore over an in-memory Library, mirroring localSession.test.ts.
function memoryStore(songs: SongRecord[] = []): SessionStore {
	let saved: PersistedSession | null = null;
	return {
		songs: async () => songs,
		loadSession: async () => saved,
		saveSession: async (session) => void (saved = session)
	};
}

function seededRng(seed: number): Rng {
	let state = seed >>> 0;
	return () => {
		state = (state + 0x6d2b79f5) | 0;
		let t = Math.imul(state ^ (state >>> 15), 1 | state);
		t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
		return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
	};
}

// A session over a mutable clock, so a test can let time pass between start, tick, and stop
// without touching audio — the Gong is hand-validated, these assert the anchor state only.
function clockedSession(clock: { now: number }) {
	return createLocalSession(memoryStore(), () => clock.now, seededRng(1));
}

describe('startTimer', () => {
	it('arms a running anchor at the chosen duration', async () => {
		const session = clockedSession({ now: 1000 });

		await session.startTimer(60);

		const anchor = get(session.timer);
		expect(anchor?.running).toBe(true);
		expect(anchor?.durationLeftAtAnchor).toBe(60);
	});
});

describe('deriveTimeLeft', () => {
	it('counts the anchor down as the clock advances', async () => {
		const clock = { now: 1000 };
		const session = clockedSession(clock);
		await session.startTimer(60);

		clock.now = 41000;

		expect(deriveTimeLeft(get(session.timer)!, clock.now)).toBe(20);
	});
});

describe('stopTimer', () => {
	it('retires a running anchor before it expires', async () => {
		const session = clockedSession({ now: 1000 });
		await session.startTimer(60);

		await session.stopTimer();

		expect(get(session.timer)).toBeNull();
	});
});

describe('tick', () => {
	it('leaves a running anchor in place before it expires', async () => {
		const clock = { now: 1000 };
		const session = clockedSession(clock);
		await session.startTimer(60);

		clock.now = 41000;
		session.tick();

		expect(get(session.timer)?.running).toBe(true);
	});

	it('retires the anchor once it has run out', async () => {
		const clock = { now: 1000 };
		const session = clockedSession(clock);
		await session.startTimer(60);

		clock.now = 61001;
		session.tick();

		expect(get(session.timer)).toBeNull();
	});

	it('does nothing when no timer is running', () => {
		const session = clockedSession({ now: 1000 });

		session.tick();

		expect(get(session.timer)).toBeNull();
	});
});
