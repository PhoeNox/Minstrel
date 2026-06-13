<script lang="ts">
	import { onDestroy } from 'svelte';
	import { playbackStore } from '$lib/sseStore';
	import { reconcile } from '$lib/reconciler';
	import { AudioEngine } from '$lib/audioEngine';
	import { emptyState, type PlaybackState } from '$lib/state';

	const playback = playbackStore();

	let engine: AudioEngine | null = null;
	let snapshot: PlaybackState = $state(emptyState);

	const unsubscribe = playback.subscribe((next) => {
		snapshot = next;
		void sync();
	});

	onDestroy(unsubscribe);

	async function sync(): Promise<void> {
		if (!engine) {
			return;
		}

		const operations = reconcile(snapshot, { playingSongId: engine.playingSongId }, Date.now());
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
		snapshot.songs.find((song) => song.id === snapshot.currentSongId) ?? null
	);
</script>

<main>
	<h1>Minstrel Player</h1>

	{#if !engine}
		<button onclick={enableSound}>Enable sound</button>
	{/if}

	<p>State: {snapshot.isPlaying ? 'Playing' : 'Idle'}</p>

	{#if currentSong}
		<p>Now playing: {currentSong.title} — {currentSong.artist}</p>
	{:else}
		<p>No song selected.</p>
	{/if}
</main>

<style>
	main {
		font-family: system-ui, sans-serif;
		max-width: 32rem;
		margin: 4rem auto;
		padding: 0 1rem;
	}

	button {
		font-size: 1rem;
		padding: 0.5rem 1rem;
		cursor: pointer;
	}
</style>
