<script lang="ts">
	import { onDestroy } from 'svelte';
	import { playbackStore } from '$lib/sseStore';
	import { reconcile } from '$lib/reconciler';
	import { AudioEngine } from '$lib/audioEngine';
	import { activePlaylist, emptyState, type PlaybackState } from '$lib/state';
	import { deriveTimeLeft, formatTimeLeft } from '$shared/timer';

	const playback = playbackStore(() => void engine?.playGong());

	let engine: AudioEngine | null = null;
	let snapshot: PlaybackState = $state(emptyState);
	let now = $state(Date.now());

	const unsubscribe = playback.subscribe((next) => {
		snapshot = next;
		void sync();
	});

	const ticker = setInterval(() => (now = Date.now()), 250);

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
	<h1>Minstrel Player</h1>

	{#if !engine}
		<button onclick={enableSound}>Enable sound</button>
	{/if}

	<p>State: {snapshot.isPlaying ? 'Playing' : 'Idle'}</p>

	{#if timeLeft !== null}
		<p class="timer">{formatTimeLeft(timeLeft)}</p>
	{/if}

	{#if currentSong}
		<p>Now playing: {currentSong.title} — {currentSong.artist}</p>
	{:else}
		<p>No song selected.</p>
	{/if}
</main>

<style>
	main {
		font-family: system-ui, sans-serif;
		min-height: 100vh;
		margin: 0;
		padding: 4rem 1rem;
		text-align: center;
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

	button {
		font-size: 1rem;
		padding: 0.5rem 1rem;
		cursor: pointer;
	}

	.timer {
		font-size: 3rem;
		font-variant-numeric: tabular-nums;
		font-weight: 700;
		margin: 1rem 0;
	}
</style>
