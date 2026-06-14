import type { AudioOperation } from './reconciler';

const FADE_SECONDS = 5;

interface Voice {
	source: AudioBufferSourceNode;
	gain: GainNode;
}

export class AudioEngine {
	private readonly context = new AudioContext();
	private readonly buffers = new Map<string, AudioBuffer>();
	private readonly voices = new Map<string, Voice>();
	private currentSongId: string | null = null;

	get playingSongId(): string | null {
		return this.currentSongId;
	}

	get loadedSongIds(): string[] {
		return [...this.buffers.keys()];
	}

	async apply(operations: AudioOperation[]): Promise<void> {
		for (const operation of operations) {
			if (operation.type === 'fade-in') {
				await this.fadeIn(operation.songId, operation.offset);
			} else if (operation.type === 'prefetch') {
				await this.load(operation.songId);
			} else {
				this.fadeOut(operation.songId);
			}
		}
	}

	private async fadeIn(songId: string, offset: number): Promise<void> {
		await this.context.resume();
		const buffer = await this.load(songId);
		const gain = this.context.createGain();
		const now = this.context.currentTime;
		gain.gain.setValueAtTime(0, now);
		gain.gain.linearRampToValueAtTime(1, now + FADE_SECONDS);
		gain.connect(this.context.destination);

		const source = this.context.createBufferSource();
		source.buffer = buffer;
		source.connect(gain);
		source.start(0, offset);

		this.voices.set(songId, { source, gain });
		this.currentSongId = songId;
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
