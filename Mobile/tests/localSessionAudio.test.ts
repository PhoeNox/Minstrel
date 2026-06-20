import { describe, expect, it } from 'vitest';
import type { Rng } from '$shared/core';
import { createLocalSession, type SessionStore } from '../src/lib/localSession';
import type { EngineTrack, PlaybackEngine } from '../src/lib/mobileAudioEngine';
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

function song(id: string, length = 100): SongRecord {
	return { id, title: `Title ${id}`, artist: `Artist ${id}`, length, name: `${id}.mp3`, importedAt: 0 };
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

// A PlaybackEngine fake that records the commands the session issues and tracks which song is
// the lead — the one piece of engine state renderAudio reads back to choose play vs re-gain.
interface FakeEngine extends PlaybackEngine {
	readonly calls: string[];
}

function fakeEngine(): FakeEngine {
	const calls: string[] = [];
	let lead: string | null = null;
	return {
		calls,
		get currentSongId() {
			return lead;
		},
		play(track: EngineTrack, offset: number, gain: number) {
			calls.push(`play ${track.id} @${offset} g${gain}`);
			lead = track.id;
		},
		pause() {
			calls.push('pause');
			lead = null;
		},
		stop() {
			calls.push('stop');
			lead = null;
		},
		setGain(gain: number) {
			calls.push(`setGain ${gain}`);
		},
		onEnded: () => {},
		onMediaPlay: () => {},
		onMediaPause: () => {},
		onMediaNext: () => {}
	};
}

function newSession(songs: SongRecord[], engine: PlaybackEngine) {
	return createLocalSession(memoryStore(songs), () => 1000, seededRng(1), engine);
}

describe('play', () => {
	it('fades in the active current entry from its start', async () => {
		const engine = fakeEngine();
		const session = newSession([song('a')], engine);
		await session.addSong('Day', 'a');

		await session.play();

		expect(engine.calls).toContain('play a @0 g1');
	});
});

describe('pause', () => {
	it('fades the lead out', async () => {
		const engine = fakeEngine();
		const session = newSession([song('a')], engine);
		await session.addSong('Day', 'a');
		await session.play();

		await session.pause();

		expect(engine.calls.at(-1)).toBe('pause');
	});
});

describe('selectSong', () => {
	it('jumps the engine to the chosen entry in the active phase', async () => {
		const engine = fakeEngine();
		const session = newSession([song('a'), song('b')], engine);
		await session.addSong('Day', 'a');
		await session.addSong('Day', 'b');

		await session.selectSong('Day', 1);

		expect(engine.calls.at(-1)).toBe('play b @0 g1');
	});

	it('does not start audio when selecting in the inactive phase', async () => {
		const engine = fakeEngine();
		const session = newSession([song('a')], engine);
		await session.addSong('Night', 'a');

		await session.selectSong('Night', 0);

		expect(engine.calls.some((call) => call.startsWith('play'))).toBe(false);
	});
});

describe('setGain', () => {
	it('ramps the lead gain when the playing phase changes volume', async () => {
		const engine = fakeEngine();
		const session = newSession([song('a')], engine);
		await session.addSong('Day', 'a');
		await session.play();

		await session.setGain('Day', 0.4);

		expect(engine.calls.at(-1)).toBe('setGain 0.4');
	});
});

describe('switchPhase', () => {
	it('crossfades to the other phase current entry', async () => {
		const engine = fakeEngine();
		const session = newSession([song('a'), song('b')], engine);
		await session.addSong('Day', 'a');
		await session.addSong('Night', 'b');
		await session.play();

		await session.switchPhase();

		expect(engine.calls.at(-1)).toBe('play b @0 g1');
	});

	it('falls silent when switching to an empty phase', async () => {
		const engine = fakeEngine();
		const session = newSession([song('a')], engine);
		await session.addSong('Day', 'a');
		await session.play();

		await session.switchPhase();

		expect(engine.calls.at(-1)).toBe('stop');
	});
});

describe('auto-advance', () => {
	it('plays the next entry when the lead song ends', async () => {
		const engine = fakeEngine();
		const session = newSession([song('a'), song('b')], engine);
		await session.addSong('Day', 'a');
		await session.addSong('Day', 'b');
		await session.play();

		engine.onEnded();

		expect(engine.calls.at(-1)).toBe('play b @0 g1');
	});

	it('wraps from the last entry back to the first', async () => {
		const engine = fakeEngine();
		const session = newSession([song('a'), song('b')], engine);
		await session.addSong('Day', 'a');
		await session.addSong('Day', 'b');
		await session.selectSong('Day', 1);

		engine.onEnded();

		expect(engine.calls.at(-1)).toBe('play a @0 g1');
	});

	it('crossfades a single-entry playlist into itself', async () => {
		const engine = fakeEngine();
		const session = newSession([song('a')], engine);
		await session.addSong('Day', 'a');
		await session.play();

		engine.onEnded();

		expect(engine.calls.at(-1)).toBe('play a @0 g1');
	});

	it('advances on the lock-screen next control', async () => {
		const engine = fakeEngine();
		const session = newSession([song('a'), song('b')], engine);
		await session.addSong('Day', 'a');
		await session.addSong('Day', 'b');
		await session.play();

		engine.onMediaNext();

		expect(engine.calls.at(-1)).toBe('play b @0 g1');
	});
});

describe('removeSong', () => {
	it('plays the entry that slides into the playing slot', async () => {
		const engine = fakeEngine();
		const session = newSession([song('a'), song('b')], engine);
		await session.addSong('Day', 'a');
		await session.addSong('Day', 'b');
		await session.play();

		await session.removeSong('Day', 0);

		expect(engine.calls.at(-1)).toBe('play b @0 g1');
	});

	it('falls silent when the only entry is removed', async () => {
		const engine = fakeEngine();
		const session = newSession([song('a')], engine);
		await session.addSong('Day', 'a');
		await session.play();

		await session.removeSong('Day', 0);

		expect(engine.calls.at(-1)).toBe('stop');
	});
});
