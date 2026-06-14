import type { AudioOperation } from './reconciler';

export class AudioEngine {
	private readonly context = new AudioContext();
	private readonly buffers = new Map<string, AudioBuffer>();
	private current: { songId: string; source: AudioBufferSourceNode } | null = null;

	get playingSongId(): string | null {
		return this.current?.songId ?? null;
	}

	get loadedSongIds(): string[] {
		return [...this.buffers.keys()];
	}

	async apply(operations: AudioOperation[]): Promise<void> {
		for (const operation of operations) {
			if (operation.type === 'start') {
				await this.start(operation.songId, operation.offset);
			} else if (operation.type === 'prefetch') {
				await this.load(operation.songId);
			} else {
				this.stop(operation.songId);
			}
		}
	}

	private async start(songId: string, offset: number): Promise<void> {
		await this.context.resume();
		const buffer = await this.load(songId);
		const source = this.context.createBufferSource();
		source.buffer = buffer;
		source.connect(this.context.destination);
		source.start(0, offset);
		this.current = { songId, source };
	}

	private stop(songId: string): void {
		if (this.current?.songId === songId) {
			this.current.source.stop();
			this.current = null;
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
