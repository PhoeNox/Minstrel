import { BUFFER_CACHE_BYTES, evictions, type CachedBuffer } from './bufferCache';
import { FADE_SECONDS, type AudioOperation } from './reconciler';

const GAIN_RAMP_SECONDS = 0.3;

interface Voice {
	source: AudioBufferSourceNode;
	gain: GainNode;
	songId: string;
	startedAt: number;
	startOffset: number;
	targetGain: number;
}

const GONG_URL = '/gong.mp3';

async function readOk(response: Response, url: string): Promise<ArrayBuffer> {
	if (!response.ok) {
		throw new Error(`Audio fetch failed (${response.status}) for ${url}`);
	}
	return response.arrayBuffer();
}

export class AudioEngine {
	private readonly context = new AudioContext();
	private readonly buffers = new Map<string, AudioBuffer>();
	private readonly voices = new Map<number, Voice>();
	private gongBuffer: AudioBuffer | null = null;
	private leadHandle: number | null = null;
	private nextHandle = 0;
	private retainedIds: string[] = [];

	get leadSongId(): string | null {
		return this.lead?.songId ?? null;
	}

	get leadPosition(): number | null {
		const voice = this.lead;
		return voice ? this.context.currentTime - voice.startedAt + voice.startOffset : null;
	}

	get leadGain(): number | null {
		return this.lead?.targetGain ?? null;
	}

	get loadedSongIds(): string[] {
		return [...this.buffers.keys()];
	}

	get retainedSongIds(): string[] {
		return [...this.retainedIds];
	}

	private get lead(): Voice | null {
		return this.leadHandle === null ? null : this.voices.get(this.leadHandle) ?? null;
	}

	async apply(operations: AudioOperation[]): Promise<void> {
		for (const operation of operations) {
			if (operation.type === 'play') {
				await this.play(operation.songId, operation.offset, operation.gain);
			} else if (operation.type === 'set-gain') {
				this.setLeadGain(operation.gain);
			} else if (operation.type === 'retain') {
				this.retainedIds = [...operation.songIds];
			} else if (operation.type === 'prefetch') {
				await this.load(operation.songId);
			} else {
				this.fadeOutLead();
			}
		}
	}

	async playGong(gain: number): Promise<void> {
		await this.context.resume();
		const buffer = await this.loadGong();
		const source = this.context.createBufferSource();
		source.buffer = buffer;
		const amplifier = this.context.createGain();
		amplifier.gain.value = gain;
		source.connect(amplifier);
		amplifier.connect(this.context.destination);
		source.onended = () => {
			source.disconnect();
			amplifier.disconnect();
		};
		source.start();
	}

	private async loadGong(): Promise<AudioBuffer> {
		if (this.gongBuffer) {
			return this.gongBuffer;
		}

		const response = await fetch(GONG_URL);
		const encoded = await readOk(response, GONG_URL);
		this.gongBuffer = await this.context.decodeAudioData(encoded);
		return this.gongBuffer;
	}

	private async play(songId: string, offset: number, target: number): Promise<void> {
		await this.context.resume();
		// Fade the outgoing voice out before awaiting the incoming buffer: a cold
		// fetch + decode must not hold the current song at full volume. With the
		// next song warmed (see the reconciler's prefetch) the load resolves at
		// once and the two ramps form a symmetric crossfade; cold, the outgoing
		// fade still starts immediately and the incoming voice joins once ready.
		this.fadeOutLead();
		const buffer = await this.load(songId);

		const now = this.context.currentTime;
		const gain = this.context.createGain();
		gain.gain.setValueAtTime(0, now);
		gain.gain.linearRampToValueAtTime(target, now + FADE_SECONDS);
		gain.connect(this.context.destination);

		const source = this.context.createBufferSource();
		source.buffer = buffer;
		source.connect(gain);
		source.start(0, offset);

		const handle = this.nextHandle++;
		this.voices.set(handle, { source, gain, songId, startedAt: now, startOffset: offset, targetGain: target });
		this.leadHandle = handle;
		source.onended = () => this.retire(handle);
	}

	private setLeadGain(target: number): void {
		const voice = this.lead;
		if (!voice) {
			return;
		}

		const now = this.context.currentTime;
		voice.gain.gain.cancelScheduledValues(now);
		voice.gain.gain.setValueAtTime(voice.gain.gain.value, now);
		voice.gain.gain.linearRampToValueAtTime(target, now + GAIN_RAMP_SECONDS);
		voice.targetGain = target;
	}

	private fadeOutLead(): void {
		const voice = this.lead;
		if (!voice) {
			return;
		}

		const now = this.context.currentTime;
		voice.gain.gain.cancelScheduledValues(now);
		voice.gain.gain.setValueAtTime(voice.gain.gain.value, now);
		voice.gain.gain.linearRampToValueAtTime(0, now + FADE_SECONDS);
		voice.source.stop(now + FADE_SECONDS);
		this.leadHandle = null;
	}

	private retire(handle: number): void {
		this.voices.delete(handle);
		if (this.leadHandle === handle) {
			this.leadHandle = null;
		}
	}

	private async load(songId: string): Promise<AudioBuffer> {
		const cached = this.buffers.get(songId);
		if (cached) {
			this.touch(songId, cached);
			return cached;
		}

		const url = `/playback/audio/${songId}`;
		const response = await fetch(url);
		const encoded = await readOk(response, url);
		const decoded = await this.context.decodeAudioData(encoded);
		this.buffers.set(songId, decoded);
		this.evict();
		return decoded;
	}

	private touch(songId: string, buffer: AudioBuffer): void {
		this.buffers.delete(songId);
		this.buffers.set(songId, buffer);
	}

	private evict(): void {
		const order: CachedBuffer[] = [...this.buffers].map(([songId, buffer]) => ({
			songId,
			bytes: buffer.length * buffer.numberOfChannels * Float32Array.BYTES_PER_ELEMENT
		}));
		for (const songId of evictions(order, this.retained(), BUFFER_CACHE_BYTES)) {
			this.buffers.delete(songId);
		}
	}

	private retained(): string[] {
		return [this.leadSongId, ...this.retainedIds].filter((id): id is string => id !== null);
	}
}
