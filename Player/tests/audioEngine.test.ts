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

	it('starts fading the lead out before the incoming buffer finishes loading', async () => {
		type FakeGain = {
			gain: {
				value: number;
				setValueAtTime: ReturnType<typeof vi.fn>;
				linearRampToValueAtTime: ReturnType<typeof vi.fn>;
				cancelScheduledValues: ReturnType<typeof vi.fn>;
			};
			connect: ReturnType<typeof vi.fn>;
			disconnect: ReturnType<typeof vi.fn>;
		};
		const gains: FakeGain[] = [];
		const makeGain = (): FakeGain => {
			const node = {
				gain: {
					value: 1,
					setValueAtTime: vi.fn(),
					linearRampToValueAtTime: vi.fn(),
					cancelScheduledValues: vi.fn()
				},
				connect: vi.fn(),
				disconnect: vi.fn()
			};
			gains.push(node);
			return node;
		};
		context.createGain.mockImplementation(makeGain);
		context.createBufferSource.mockImplementation(() => ({
			buffer: null as AudioBuffer | null,
			connect: vi.fn(),
			start: vi.fn(),
			stop: vi.fn(),
			onended: null as (() => void) | null
		}));

		let releaseIncoming!: () => void;
		const incoming = new Promise<void>((resolve) => {
			releaseIncoming = resolve;
		});
		let call = 0;
		vi.stubGlobal(
			'fetch',
			vi.fn(() => {
				call += 1;
				return call === 1
					? Promise.resolve(response({ ok: true, status: 200 }))
					: incoming.then(() => response({ ok: true, status: 200 }));
			})
		);

		const engine = new AudioEngine();
		await engine.apply([{ type: 'play', songId: 'first', offset: 0, gain: 1 }]);
		const leadGain = gains[0];

		const pending = engine.apply([{ type: 'play', songId: 'second', offset: 0, gain: 1 }]);
		await Promise.resolve();
		await Promise.resolve();

		expect(leadGain.gain.linearRampToValueAtTime).toHaveBeenCalledWith(0, expect.any(Number));

		releaseIncoming();
		await pending;
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
