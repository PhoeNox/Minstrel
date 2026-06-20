// Mobile's audio layer (ADR-0008): the media-element rebuild of Player's `audioEngine.ts`.
// Two `HTMLAudioElement → MediaElementAudioSourceNode → GainNode → destination` channels
// alternate as the lead so a song can crossfade into the next while the old one fades out,
// and — being media elements streaming from object URLs — playback survives screen-lock and
// app-background where Player's decoded-buffer engine would not. The decoded-PCM bufferCache
// falls away: the browser streams each blob, so there is nothing to cache or evict.

export const FADE_SECONDS = 5;
const GAIN_RAMP_SECONDS = 0.3;

// The denormalized song the engine needs to play one entry and label it on the lock screen.
export interface EngineTrack {
	id: string;
	title: string;
	artist: string;
	length: number;
}

// The imperative surface LocalSession drives after each command. Kept DOM-free so the session
// can be unit-tested against a fake; MobileAudioEngine is the one browser implementation.
export interface PlaybackEngine {
	play(track: EngineTrack, offset: number, gain: number): void;
	pause(): void;
	stop(): void;
	setGain(gain: number): void;
	readonly currentSongId: string | null;
	// Raised when the lead song reaches its natural end, or the lock-screen transport is used.
	onEnded: () => void;
	onMediaPlay: () => void;
	onMediaPause: () => void;
	onMediaNext: () => void;
}

interface Channel {
	el: HTMLAudioElement;
	gain: GainNode;
	songId: string | null;
	url: string | null;
	stopTimer: ReturnType<typeof setTimeout> | null;
	// The lead song's length and whether it has already raised onEnded — so the nearing-end
	// crossfade and the natural `ended` together fire exactly once per song.
	length: number;
	advanced: boolean;
}

export class MobileAudioEngine implements PlaybackEngine {
	private readonly context = new AudioContext();
	private readonly channels: [Channel, Channel];
	private leadIndex: number | null = null;

	onEnded: () => void = () => {};
	onMediaPlay: () => void = () => {};
	onMediaPause: () => void = () => {};
	onMediaNext: () => void = () => {};

	constructor(private readonly audioBlob: (id: string) => Promise<Blob | null>) {
		this.channels = [this.createChannel(), this.createChannel()];
		this.wireMediaSession();
	}

	get currentSongId(): string | null {
		return this.leadIndex === null ? null : this.channels[this.leadIndex].songId;
	}

	async play(track: EngineTrack, offset: number, gain: number): Promise<void> {
		await this.resume();
		const blob = await this.audioBlob(track.id);
		// A missing blob means the OS evicted these bytes; leave the lead untouched (silent)
		// rather than tearing down what is playing — re-import is the recovery path.
		if (blob === null) {
			return;
		}

		const outgoing = this.lead;
		const incoming = this.idle;
		incoming.length = track.length;
		incoming.advanced = false;
		await this.start(incoming, track.id, blob, offset);
		this.fadeIn(incoming, gain);
		if (outgoing !== null && outgoing !== incoming) {
			this.fadeOut(outgoing);
		}
		this.leadIndex = this.channels.indexOf(incoming);
		this.announce(track);
	}

	pause(): void {
		this.silence('paused');
	}

	stop(): void {
		this.silence('none');
	}

	setGain(gain: number): void {
		const lead = this.lead;
		if (lead === null) {
			return;
		}
		const now = this.context.currentTime;
		lead.gain.gain.cancelScheduledValues(now);
		lead.gain.gain.setValueAtTime(lead.gain.gain.value, now);
		lead.gain.gain.linearRampToValueAtTime(gain, now + GAIN_RAMP_SECONDS);
	}

	private get lead(): Channel | null {
		return this.leadIndex === null ? null : this.channels[this.leadIndex];
	}

	private get idle(): Channel {
		return this.leadIndex === 0 ? this.channels[1] : this.channels[0];
	}

	private silence(state: MediaSessionPlaybackState): void {
		const lead = this.lead;
		if (lead !== null) {
			this.fadeOut(lead);
		}
		this.leadIndex = null;
		if ('mediaSession' in navigator) {
			if (state === 'none') {
				navigator.mediaSession.metadata = null;
			}
			navigator.mediaSession.playbackState = state;
		}
	}

	private async start(channel: Channel, songId: string, blob: Blob, offset: number): Promise<void> {
		if (channel.stopTimer !== null) {
			clearTimeout(channel.stopTimer);
			channel.stopTimer = null;
		}
		if (channel.url !== null) {
			URL.revokeObjectURL(channel.url);
		}
		channel.url = URL.createObjectURL(blob);
		channel.songId = songId;
		channel.el.src = channel.url;
		channel.el.load();
		await this.metadataReady(channel.el);
		channel.el.currentTime = offset;
		try {
			await channel.el.play();
		} catch {
			// A rejected play() (autoplay policy) leaves the channel silent; the next user
			// gesture re-issues play(), which is enough to start it.
		}
	}

	private fadeIn(channel: Channel, target: number): void {
		const now = this.context.currentTime;
		channel.gain.gain.cancelScheduledValues(now);
		channel.gain.gain.setValueAtTime(0, now);
		channel.gain.gain.linearRampToValueAtTime(target, now + FADE_SECONDS);
	}

	private fadeOut(channel: Channel): void {
		const now = this.context.currentTime;
		channel.gain.gain.cancelScheduledValues(now);
		channel.gain.gain.setValueAtTime(channel.gain.gain.value, now);
		channel.gain.gain.linearRampToValueAtTime(0, now + FADE_SECONDS);
		// Pause the element once it is inaudible so a backgrounded phase is not still
		// decoding; cleared in start() if this channel is reused before the fade completes.
		if (channel.stopTimer !== null) {
			clearTimeout(channel.stopTimer);
		}
		channel.stopTimer = setTimeout(() => {
			channel.el.pause();
			channel.stopTimer = null;
		}, FADE_SECONDS * 1000);
	}

	private createChannel(): Channel {
		const el = new Audio();
		el.preload = 'auto';
		const gain = this.context.createGain();
		gain.gain.value = 0;
		this.context.createMediaElementSource(el).connect(gain).connect(this.context.destination);
		const channel: Channel = {
			el,
			gain,
			songId: null,
			url: null,
			stopTimer: null,
			length: 0,
			advanced: false
		};
		// Advance a fade-length before the end so the next song crossfades in over this one's
		// tail (a real overlap, not a fade-in over silence); `timeupdate` is the media clock.
		el.addEventListener('timeupdate', () => this.maybeAdvance(channel));
		// Songs shorter than a fade get no nearing-end window, so the natural end is the
		// fallback trigger. Either path fires onEnded once; a faded-out element is never lead.
		el.addEventListener('ended', () => {
			if (this.lead === channel && !channel.advanced) {
				channel.advanced = true;
				this.onEnded();
			}
		});
		return channel;
	}

	private maybeAdvance(channel: Channel): void {
		if (this.lead !== channel || channel.advanced) {
			return;
		}
		if (channel.length > FADE_SECONDS && channel.el.currentTime >= channel.length - FADE_SECONDS) {
			channel.advanced = true;
			this.onEnded();
		}
	}

	private wireMediaSession(): void {
		if (!('mediaSession' in navigator)) {
			return;
		}
		navigator.mediaSession.setActionHandler('play', () => this.onMediaPlay());
		navigator.mediaSession.setActionHandler('pause', () => this.onMediaPause());
		navigator.mediaSession.setActionHandler('nexttrack', () => this.onMediaNext());
	}

	private announce(track: EngineTrack): void {
		if (!('mediaSession' in navigator)) {
			return;
		}
		navigator.mediaSession.metadata = new MediaMetadata({ title: track.title, artist: track.artist });
		navigator.mediaSession.playbackState = 'playing';
		if (navigator.mediaSession.setPositionState && track.length > 0) {
			const position = Math.min(this.lead?.el.currentTime ?? 0, track.length);
			navigator.mediaSession.setPositionState({ duration: track.length, position, playbackRate: 1 });
		}
	}

	private async resume(): Promise<void> {
		try {
			await this.context.resume();
		} catch {
			// resume() rejects only outside a user gesture; the triggering tap is one, so this
			// is a defensive guard, not an expected path.
		}
	}

	private metadataReady(el: HTMLAudioElement): Promise<void> {
		if (el.readyState >= HTMLMediaElement.HAVE_METADATA) {
			return Promise.resolve();
		}
		return new Promise((resolve) => {
			const done = () => {
				el.removeEventListener('loadedmetadata', done);
				el.removeEventListener('error', done);
				resolve();
			};
			el.addEventListener('loadedmetadata', done);
			el.addEventListener('error', done);
		});
	}
}
