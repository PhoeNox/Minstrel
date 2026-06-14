<script lang="ts">
	import { onDestroy } from 'svelte';
	import { playbackStore } from '$lib/sseStore';
	import { play, pause, switchPhase, startTimer, stopTimer } from '$lib/commandClient';
	import Playlist from '$lib/Playlist.svelte';
	import { emptyState, type PlaybackState } from '$lib/state';
	import { deriveTimeLeft, formatTimeLeft } from '$shared/timer';

	const playback = playbackStore();

	let snapshot: PlaybackState = $state(emptyState);
	let now = $state(Date.now());
	let durationMinutes = $state(5);

	const unsubscribe = playback.subscribe((next) => (snapshot = next));
	const ticker = setInterval(() => (now = Date.now()), 250);
	onDestroy(() => {
		unsubscribe();
		clearInterval(ticker);
	});

	const timeLeft = $derived(snapshot.timer ? deriveTimeLeft(snapshot.timer, now) : null);

	const currentSong = $derived(
		[...snapshot.playlists.day.songs, ...snapshot.playlists.night.songs].find(
			(song) => song.id === snapshot.currentSongId
		) ?? null
	);

	const otherPlaylist = $derived(
		snapshot.phase === 'Day' ? snapshot.playlists.night : snapshot.playlists.day
	);
	const canSwitch = $derived(otherPlaylist.currentSongId !== null);
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
		<button onclick={() => switchPhase()} disabled={!canSwitch}>
			Switch to {snapshot.phase === 'Day' ? 'Night' : 'Day'}
		</button>
	</div>

	<div class="timer">
		{#if timeLeft !== null}
			<p class="countdown">{formatTimeLeft(timeLeft)}</p>
			<button onclick={() => stopTimer()}>Stop timer</button>
		{:else}
			<label>
				<input type="number" min="1" bind:value={durationMinutes} />
				min
			</label>
			<button onclick={() => startTimer(durationMinutes * 60)}>Start timer</button>
		{/if}
	</div>

	<div class="playlists">
		<Playlist phase="Day" playlist={snapshot.playlists.day} />
		<Playlist phase="Night" playlist={snapshot.playlists.night} />
	</div>
</main>

<style>
	main {
		font-family: system-ui, sans-serif;
		max-width: 40rem;
		margin: 2rem auto;
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

	.timer {
		display: flex;
		gap: 1rem;
		align-items: center;
		justify-content: center;
		margin-top: 1.5rem;
	}

	.countdown {
		font-size: 2rem;
		font-weight: 700;
		font-variant-numeric: tabular-nums;
		margin: 0;
	}

	.timer input {
		width: 4rem;
		font-size: 1rem;
		padding: 0.5rem;
	}

	.playlists {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 2rem;
		margin-top: 2rem;
	}

	@media (max-width: 32rem) {
		.playlists {
			grid-template-columns: 1fr;
		}
	}
</style>
