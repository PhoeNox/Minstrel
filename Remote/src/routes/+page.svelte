<script lang="ts">
	import { onDestroy } from 'svelte';
	import { playbackStore } from '$lib/sseStore';
	import { play, pause } from '$lib/commandClient';
	import { emptyState, type PlaybackState } from '$lib/state';

	const playback = playbackStore();

	let snapshot: PlaybackState = $state(emptyState);
	const unsubscribe = playback.subscribe((next) => (snapshot = next));
	onDestroy(unsubscribe);

	const currentSong = $derived(
		snapshot.songs.find((song) => song.id === snapshot.currentSongId) ?? null
	);
</script>

<main>
	<h1>Minstrel Remote</h1>

	<p class="status">{snapshot.isPlaying ? 'Playing' : 'Paused'}</p>

	{#if currentSong}
		<p class="song">{currentSong.title} — {currentSong.artist}</p>
	{:else}
		<p class="song">No song selected.</p>
	{/if}

	<div class="controls">
		<button onclick={() => play()} disabled={snapshot.isPlaying}>Play</button>
		<button onclick={() => pause()} disabled={!snapshot.isPlaying}>Pause</button>
	</div>
</main>

<style>
	main {
		font-family: system-ui, sans-serif;
		max-width: 32rem;
		margin: 4rem auto;
		padding: 0 1rem;
		text-align: center;
	}

	.status {
		font-size: 1.25rem;
		font-weight: 600;
	}

	.song {
		color: #555;
	}

	.controls {
		display: flex;
		gap: 1rem;
		justify-content: center;
		margin-top: 1.5rem;
	}

	button {
		font-size: 1rem;
		padding: 0.75rem 1.5rem;
		cursor: pointer;
	}

	button:disabled {
		cursor: default;
		opacity: 0.5;
	}
</style>
