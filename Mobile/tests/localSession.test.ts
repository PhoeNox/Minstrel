import { describe, expect, it } from 'vitest';
import { get } from 'svelte/store';
import type { Rng } from '$shared/core';
import { createLocalSession, type SessionStore } from '../src/lib/localSession';
import type { PersistedSession, SongRecord } from '../src/lib/musicStore';

// A SessionStore over an in-memory Library and a single mutable session record, so two
// sessions sharing one store model an app restart (the second `load`s what the first saved).
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

// A deterministic [0, 1) generator (mulberry32) so `shuffle` is reproducible in tests.
function seededRng(seed: number): Rng {
	let state = seed >>> 0;
	return () => {
		state = (state + 0x6d2b79f5) | 0;
		let t = Math.imul(state ^ (state >>> 15), 1 | state);
		t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
		return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
	};
}

function newSession(songs: SongRecord[], store: SessionStore = memoryStore(songs)) {
	return createLocalSession(store, () => 1000, seededRng(1));
}

function dayOf(session: ReturnType<typeof createLocalSession>) {
	return get(session.snapshot).state.playlists.day;
}

function nightOf(session: ReturnType<typeof createLocalSession>) {
	return get(session.snapshot).state.playlists.night;
}

describe('addSong', () => {
	it('adds the resolved library song as a new playlist entry', async () => {
		const session = newSession([song('a'), song('b')]);

		await session.addSong('Day', 'a');

		expect(dayOf(session).songs.map((entry) => entry.id)).toEqual(['a']);
		expect(dayOf(session).currentIndex).toBe(0);
	});

	it('lets the same song be added more than once', async () => {
		const session = newSession([song('a')]);

		await session.addSong('Day', 'a');
		await session.addSong('Day', 'a');

		expect(dayOf(session).songs.map((entry) => entry.id)).toEqual(['a', 'a']);
	});

	it('ignores a song id absent from the library', async () => {
		const session = newSession([song('a')]);

		await session.addSong('Day', 'ghost');

		expect(dayOf(session).songs).toHaveLength(0);
	});
});

describe('removeSong', () => {
	it('keeps the current entry on its song when an earlier entry is removed', async () => {
		const session = newSession([song('a'), song('b'), song('c')]);
		await session.addSong('Day', 'a');
		await session.addSong('Day', 'b');
		await session.addSong('Day', 'c');
		await session.selectSong('Day', 1);

		await session.removeSong('Day', 0);

		expect(dayOf(session).songs.map((entry) => entry.id)).toEqual(['b', 'c']);
		expect(dayOf(session).currentIndex).toBe(0);
		expect(get(session.snapshot).state.currentSongId).toBe('b');
	});

	it('advances the current entry to the next song when it is removed', async () => {
		const session = newSession([song('a'), song('b'), song('c')]);
		await session.addSong('Day', 'a');
		await session.addSong('Day', 'b');
		await session.addSong('Day', 'c');
		await session.selectSong('Day', 0);

		await session.removeSong('Day', 0);

		expect(dayOf(session).songs.map((entry) => entry.id)).toEqual(['b', 'c']);
		expect(dayOf(session).currentIndex).toBe(0);
		expect(get(session.snapshot).state.currentSongId).toBe('b');
	});

	it('falls to Nothing Cued when the last entry is removed', async () => {
		const session = newSession([song('a')]);
		await session.addSong('Day', 'a');
		await session.selectSong('Day', 0);

		await session.removeSong('Day', 0);

		expect(dayOf(session).songs).toHaveLength(0);
		expect(dayOf(session).currentIndex).toBeNull();
		expect(get(session.snapshot).state.currentSongId).toBeNull();
	});
});

describe('moveSong', () => {
	it('keeps the current entry on its song when it is reordered', async () => {
		const session = newSession([song('a'), song('b'), song('c')]);
		await session.addSong('Day', 'a');
		await session.addSong('Day', 'b');
		await session.addSong('Day', 'c');
		await session.selectSong('Day', 0);

		await session.moveSong('Day', 0, 2);

		expect(dayOf(session).songs.map((entry) => entry.id)).toEqual(['b', 'c', 'a']);
		expect(dayOf(session).currentIndex).toBe(2);
		expect(get(session.snapshot).state.currentSongId).toBe('a');
	});
});

describe('shuffle', () => {
	it('keeps a started song first when shuffling', async () => {
		const session = newSession([song('a'), song('b'), song('c')]);
		await session.addSong('Day', 'a');
		await session.addSong('Day', 'b');
		await session.addSong('Day', 'c');
		await session.selectSong('Day', 1);

		await session.shuffle('Day');

		const day = dayOf(session);
		expect(day.songs).toHaveLength(3);
		expect(day.currentIndex).toBe(0);
		expect(day.songs[0].id).toBe('b');
		expect(get(session.snapshot).state.currentSongId).toBe('b');
	});

	it('promotes the shuffled front when no song has started', async () => {
		const session = newSession([song('a'), song('b'), song('c')]);
		await session.addSong('Day', 'a');
		await session.addSong('Day', 'b');
		await session.addSong('Day', 'c');

		await session.shuffle('Day');

		const day = dayOf(session);
		expect(day.songs).toHaveLength(3);
		expect(day.currentIndex).toBe(0);
		expect(get(session.snapshot).state.currentSongId).toBeNull();
	});
});

describe('selectSong', () => {
	it('makes the chosen entry the current entry', async () => {
		const session = newSession([song('a'), song('b')]);
		await session.addSong('Day', 'a');
		await session.addSong('Day', 'b');

		await session.selectSong('Day', 1);

		expect(dayOf(session).currentIndex).toBe(1);
		expect(get(session.snapshot).state.currentSongId).toBe('b');
	});
});

describe('setGain', () => {
	it('sets the gain of one phase without touching the other', async () => {
		const session = newSession([]);

		await session.setGain('Night', 0.4);

		expect(nightOf(session).gain).toBe(0.4);
		expect(dayOf(session).gain).toBe(1);
	});
});

describe('persistence', () => {
	it('restores both playlists, current entries, and gains across a reload', async () => {
		const store = memoryStore([song('a'), song('b'), song('c')]);
		const first = createLocalSession(store, () => 1000, seededRng(1));
		await first.load();
		await first.addSong('Day', 'a');
		await first.addSong('Day', 'b');
		await first.addSong('Night', 'c');
		await first.selectSong('Day', 1);
		await first.setGain('Night', 0.4);

		const restored = createLocalSession(store, () => 1000, seededRng(1));
		await restored.load();

		expect(dayOf(restored).songs.map((entry) => entry.id)).toEqual(['a', 'b']);
		expect(dayOf(restored).currentIndex).toBe(1);
		expect(nightOf(restored).songs.map((entry) => entry.id)).toEqual(['c']);
		expect(nightOf(restored).gain).toBe(0.4);
	});

	it('loads an empty store as Nothing Cued', async () => {
		const session = createLocalSession(memoryStore(), () => 1000, seededRng(1));

		await session.load();

		expect(dayOf(session).songs).toHaveLength(0);
		expect(dayOf(session).currentIndex).toBeNull();
		expect(nightOf(session).songs).toHaveLength(0);
	});
});
