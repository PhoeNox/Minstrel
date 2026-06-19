import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AudioEngine } from '../src/lib/audioEngine';

// The engine talks to native Web Audio at construction time; node has no
// AudioContext, so stub the minimum surface the fetch boundary exercises.
class FakeAudioContext {
	currentTime = 0;
	destination = {};
	resume = vi.fn(() => Promise.resolve());
	decodeAudioData = vi.fn(() => Promise.resolve({} as AudioBuffer));
	createGain = vi.fn();
	createBufferSource = vi.fn();
}

let context: FakeAudioContext;

const response = (init: { ok: boolean; status: number }): Response =>
	({ ...init, arrayBuffer: () => Promise.resolve(new ArrayBuffer(8)) }) as unknown as Response;

describe('AudioEngine audio fetch boundary', () => {
	beforeEach(() => {
		context = new FakeAudioContext();
		vi.stubGlobal(
			'AudioContext',
			class {
				constructor() {
					return context;
				}
			}
		);
	});

	afterEach(() => {
		vi.unstubAllGlobals();
		vi.restoreAllMocks();
	});

	it('throws on a non-OK audio response before reaching decodeAudioData', async () => {
		vi.stubGlobal(
			'fetch',
			vi.fn(() => Promise.resolve(response({ ok: false, status: 404 })))
		);
		const engine = new AudioEngine();

		await expect(engine.apply([{ type: 'play', songId: 'gone', offset: 0, gain: 1 }])).rejects.toThrow(
			/404/
		);
		expect(context.decodeAudioData).not.toHaveBeenCalled();
	});

	it('throws on a non-OK gong response before reaching decodeAudioData', async () => {
		vi.stubGlobal(
			'fetch',
			vi.fn(() => Promise.resolve(response({ ok: false, status: 500 })))
		);
		const engine = new AudioEngine();

		await expect(engine.playGong(1)).rejects.toThrow(/500/);
		expect(context.decodeAudioData).not.toHaveBeenCalled();
	});

	it('disconnects the gong graph once playback ends', async () => {
		vi.stubGlobal(
			'fetch',
			vi.fn(() => Promise.resolve(response({ ok: true, status: 200 })))
		);
		const source = {
			buffer: null as AudioBuffer | null,
			connect: vi.fn(),
			disconnect: vi.fn(),
			start: vi.fn(),
			onended: null as (() => void) | null
		};
		const amplifier = { gain: { value: 0 }, connect: vi.fn(), disconnect: vi.fn() };
		context.createBufferSource.mockReturnValue(source);
		context.createGain.mockReturnValue(amplifier);
		const engine = new AudioEngine();

		await engine.playGong(0.5);
		expect(source.start).toHaveBeenCalled();
		expect(source.disconnect).not.toHaveBeenCalled();

		source.onended?.();

		expect(source.disconnect).toHaveBeenCalled();
		expect(amplifier.disconnect).toHaveBeenCalled();
	});
});
