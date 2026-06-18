import { afterEach, describe, expect, it, vi } from 'vitest';
import { CommandError, play, selectSong } from '../src/lib/commandClient';

afterEach(() => {
	vi.unstubAllGlobals();
});

function stubFetch(impl: typeof fetch): void {
	vi.stubGlobal('fetch', vi.fn(impl));
}

describe('command client', () => {
	it('resolves when the response is ok', async () => {
		stubFetch(async () => new Response(null, { status: 204 }));

		await expect(play()).resolves.toBeUndefined();
	});

	it('rejects with a labelled CommandError when the response is not ok', async () => {
		stubFetch(async () => new Response(null, { status: 400 }));

		await expect(play()).rejects.toBeInstanceOf(CommandError);
		await expect(play()).rejects.toMatchObject({ action: 'Play' });
	});

	it('rejects when fetch throws (Backend unreachable)', async () => {
		stubFetch(async () => {
			throw new TypeError('Failed to fetch');
		});

		await expect(selectSong('Day', 0)).rejects.toBeInstanceOf(CommandError);
		await expect(selectSong('Day', 0)).rejects.toMatchObject({ action: 'Select song' });
	});

	it('sends a JSON body for commands that carry one', async () => {
		const fetchMock = vi.fn(async () => new Response(null, { status: 204 }));
		vi.stubGlobal('fetch', fetchMock);

		await selectSong('Night', 3);

		expect(fetchMock).toHaveBeenCalledWith('/playback/select', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ phase: 'Night', index: 3 })
		});
	});
});
