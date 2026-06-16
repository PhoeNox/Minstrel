import type { AudioOperation } from './reconciler';

const FADE_SECONDS = 5;
const GAIN_RAMP_SECONDS = 0.3;

interface Voice {
	source: AudioBufferSourceNode;
	gain: GainNode;
	startedAt: number;
	startOffset: number;
}

const GONG_URL = '/gong.mp3';

export class AudioEngine {
	private readonly context = new AudioContext();
	private readonly buffers = new Map<string, AudioBuffer>();
	private readonly voices = new Map<string, Voice>();
	private gongBuffer: AudioBuffer | null = null;
	private currentSongId: string | null = null;
	private currentGain = 1;

	get playingSongId(): string | null {
		return this.currentSongId;
	}

	get playingPosition(): number | null {
		if (this.currentSongId === null) {
			return null;
		}

		const voice = this.voices.get(this.currentSongId);
		return voice ? this.context.currentTime - voice.startedAt + voice.startOffset : null;
	}

	get loadedSongIds(): string[] {
		return [...this.buffers.keys()];
	}

	get gain(): number {
		return this.currentGain;
	}

	async apply(operations: AudioOperation[]): Promise<void> {
		for (const operation of operations) {
			if (operation.type === 'fade-in') {
				await this.fadeIn(operation.songId, operation.offset, operation.gain);
			} else if (operation.type === 'set-gain') {
				this.setGain(operation.songId, operation.gain);
			} else if (operation.type === 'prefetch') {
				await this.load(operation.songId);
			} else {
				this.fadeOut(operation.songId);
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
		source.start();
	}

	private async loadGong(): Promise<AudioBuffer> {
		if (this.gongBuffer) {
			return this.gongBuffer;
		}

		const response = await fetch(GONG_URL);
		const encoded = await response.arrayBuffer();
		this.gongBuffer = await this.context.decodeAudioData(encoded);
		return this.gongBuffer;
	}

	private async fadeIn(songId: string, offset: number, target: number): Promise<void> {
		await this.context.resume();
		const buffer = await this.load(songId);
		const gain = this.context.createGain();
		const now = this.context.currentTime;
		gain.gain.setValueAtTime(0, now);
		gain.gain.linearRampToValueAtTime(target, now + FADE_SECONDS);
		gain.connect(this.context.destination);

		const source = this.context.createBufferSource();
		source.buffer = buffer;
		source.connect(gain);
		source.start(0, offset);

		this.voices.set(songId, { source, gain, startedAt: now, startOffset: offset });
		this.currentSongId = songId;
		this.currentGain = target;
	}

	private setGain(songId: string, target: number): void {
		const voice = this.voices.get(songId);
		if (!voice) {
			return;
		}

		const now = this.context.currentTime;
		voice.gain.gain.cancelScheduledValues(now);
		voice.gain.gain.setValueAtTime(voice.gain.gain.value, now);
		voice.gain.gain.linearRampToValueAtTime(target, now + GAIN_RAMP_SECONDS);
		this.currentGain = target;
	}

	private fadeOut(songId: string): void {
		const voice = this.voices.get(songId);
		if (!voice) {
			return;
		}

		const now = this.context.currentTime;
		voice.gain.gain.setValueAtTime(voice.gain.gain.value, now);
		voice.gain.gain.linearRampToValueAtTime(0, now + FADE_SECONDS);
		voice.source.stop(now + FADE_SECONDS);

		this.voices.delete(songId);
		if (this.currentSongId === songId) {
			this.currentSongId = null;
		}
	}

	private async load(songId: string): Promise<AudioBuffer> {
		const cached = this.buffers.get(songId);
		if (cached) {
			return cached;
		}

		const response = await fetch(`/audio/${songId}`);
		const encoded = await response.arrayBuffer();
		const decoded = await this.context.decodeAudioData(encoded);
		this.buffers.set(songId, decoded);
		return decoded;
	}
}
