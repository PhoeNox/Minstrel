<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { playbackStore } from '$lib/sseStore';
	import { reconcile } from '$lib/reconciler';
	import { AudioEngine } from '$lib/audioEngine';
	import { activePlaylist, emptyState, type PlaybackState } from '$lib/state';
	import { fetchRemoteUrl } from '$lib/connection';
	import { qrSvg } from '$lib/qr';
	import { deriveTimeLeft, formatTimeLeft } from '$shared/timer';

	const playback = playbackStore(() => void engine?.playGong());

	let engine: AudioEngine | null = null;
	let snapshot: PlaybackState = $state(emptyState);
	let now = $state(Date.now());
	let qr: string | null = $state(null);

	const unsubscribe = playback.subscribe((next) => {
		snapshot = next;
		void sync();
	});

	const ticker = setInterval(() => (now = Date.now()), 250);

	onMount(async () => {
		const remoteUrl = await fetchRemoteUrl();
		qr = qrSvg(remoteUrl);
	});

	onDestroy(() => {
		unsubscribe();
		clearInterval(ticker);
	});

	const timeLeft = $derived(snapshot.timer ? deriveTimeLeft(snapshot.timer, now) : null);

	async function sync(): Promise<void> {
		if (!engine) {
			return;
		}

		const operations = reconcile(
			snapshot,
			{ playingSongId: engine.playingSongId, loadedSongIds: engine.loadedSongIds, gain: engine.gain },
			Date.now()
		);
		await engine.apply(operations);
	}

	function enableSound(): void {
		if (engine) {
			return;
		}

		engine = new AudioEngine();
		void sync();
	}

	const currentSong = $derived(
		activePlaylist(snapshot).songs.find((song) => song.id === snapshot.currentSongId) ?? null
	);
</script>

<main class:night={snapshot.phase === 'Night'}>
	<figure class="artwork">
		<img src="/artwork.webp" alt="Blood on the Clocktower" />
	</figure>

	<section class="status">
		{#if timeLeft !== null}
			<p class="timer">{formatTimeLeft(timeLeft)}</p>
		{/if}

		{#if currentSong}
			<p class="now-playing">{currentSong.title} — {currentSong.artist}</p>
		{:else}
			<p class="now-playing">No song selected.</p>
		{/if}

		{#if !engine}
			<button onclick={enableSound}>Enable sound</button>
		{/if}
	</section>

	{#if qr}
		<aside class="pairing">
			<div class="qr">{@html qr}</div>
			<p>Scan to control</p>
		</aside>
	{/if}
</main>

<style>
	main {
		font-family: system-ui, sans-serif;
		min-height: 100vh;
		margin: 0;
		padding: 2rem;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: 1.5rem;
		background: #fdf6e3;
		color: #1a1a1a;
		transition:
			background 5s ease,
			color 5s ease;
	}

	main.night {
		background: #0b1026;
		color: #e8ecff;
	}

	.artwork {
		margin: 0;
		max-width: min(70vmin, 32rem);
	}

	.artwork img {
		display: block;
		width: 100%;
		height: auto;
		border-radius: 1rem;
	}

	.status {
		text-align: center;
	}

	.timer {
		font-size: 3rem;
		font-variant-numeric: tabular-nums;
		font-weight: 700;
		margin: 0 0 0.5rem;
	}

	.now-playing {
		font-size: 1.25rem;
		margin: 0 0 1rem;
	}

	button {
		font-size: 1rem;
		padding: 0.5rem 1rem;
		cursor: pointer;
	}

	.pairing {
		position: fixed;
		bottom: 1.5rem;
		right: 1.5rem;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 0.25rem;
		padding: 0.75rem;
		background: #ffffff;
		border-radius: 0.75rem;
		box-shadow: 0 2px 12px rgb(0 0 0 / 0.25);
		color: #1a1a1a;
	}

	.qr {
		width: 8rem;
		height: 8rem;
	}

	.qr :global(svg) {
		display: block;
		width: 100%;
		height: 100%;
	}

	.pairing p {
		margin: 0;
		font-size: 0.85rem;
	}
</style>
