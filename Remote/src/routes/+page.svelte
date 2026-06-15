<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import { playbackStore } from '$lib/sseStore';
	import { play, pause, switchPhase, startTimer, stopTimer } from '$lib/commandClient';
	import { fetchLibrary } from '$lib/libraryClient';
	import Playlist from '$lib/Playlist.svelte';
	import Library from '$lib/Library.svelte';
	import { emptyState, type PlaybackState, type SongDto } from '$lib/state';
	import { deriveTimeLeft, formatTimeLeft } from '$shared/timer';

	type Tab = 'day' | 'night' | 'library';

	const playback = playbackStore();

	let snapshot: PlaybackState = $state(emptyState);
	let connected = $state(false);
	let now = $state(Date.now());
	let durationMinutes = $state(5);
	let library: SongDto[] = $state([]);
	let tab: Tab = $state('day');
	let timerDialogOpen = $state(false);

	onMount(async () => {
		library = await fetchLibrary();
	});

	const unsubscribe = playback.subscribe((next) => {
		snapshot = next.state;
		connected = next.connected;
	});
	const ticker = setInterval(() => (now = Date.now()), 250);
	onDestroy(() => {
		unsubscribe();
		clearInterval(ticker);
	});

	const timeLeft = $derived(snapshot.timer ? deriveTimeLeft(snapshot.timer, now) : null);
	const timerRunning = $derived(timeLeft !== null);
	const urgent = $derived(timeLeft !== null && timeLeft <= 10);

	const currentSong = $derived(
		[...snapshot.playlists.day.songs, ...snapshot.playlists.night.songs].find(
			(song) => song.id === snapshot.currentSongId
		) ?? null
	);

	const isNight = $derived(snapshot.phase === 'Night');
	const otherPlaylist = $derived(
		isNight ? snapshot.playlists.day : snapshot.playlists.night
	);
	const canSwitch = $derived(connected && otherPlaylist.currentIndex !== null);

	function stepMinutes(delta: number): void {
		durationMinutes = Math.min(180, Math.max(1, durationMinutes + delta));
	}

	function togglePlay(): void {
		if (snapshot.isPlaying) {
			void pause();
		} else {
			void play();
		}
	}

	function handleStartTimer(): void {
		void startTimer(durationMinutes * 60);
		timerDialogOpen = false;
	}

	function handleStopTimer(): void {
		void stopTimer();
		timerDialogOpen = false;
	}
</script>

<svelte:window
	onkeydown={(e) => e.key === 'Escape' && timerDialogOpen && (timerDialogOpen = false)}
/>

<div class="app" class:night={isNight}>
	<header class="topbar">
		<section class="now">
			<div class="now-line">
				<span class="phase">
					<span class="phase-glyph">{isNight ? '☾' : '☀'}</span>
					<span class="phase-name">{isNight ? 'Night' : 'Day'}</span>
				</span>
				<span class="now-sep">·</span>
				<span class="phase-state">{snapshot.isPlaying ? 'playing' : 'paused'}</span>
				<span
					class="conn"
					class:live={connected}
					title={connected ? 'Connected' : 'Reconnecting'}
				>
					<span class="conn-dot"></span>
				</span>
			</div>
			{#if currentSong}
				<p class="np">
					<span class="np-title">{currentSong.title}</span>
					<span class="np-artist">{currentSong.artist}</span>
					<span class="np-length">{formatTimeLeft(currentSong.length)}</span>
				</p>
			{:else}
				<p class="np">
					<span class="np-title np-empty">Nothing cued</span>
					<span class="np-artist">Choose a song from a playlist</span>
				</p>
			{/if}
		</section>
	</header>

	<main class="deck">
		{#if tab === 'day'}
			<Playlist phase="Day" playlist={snapshot.playlists.day} />
		{:else if tab === 'night'}
			<Playlist phase="Night" playlist={snapshot.playlists.night} />
		{:else}
			<Library songs={library} />
		{/if}
	</main>

	<nav class="tabbar">
		<button class="tab" class:active={tab === 'day'} onclick={() => (tab = 'day')}>
			<span class="tab-glyph">☀</span>
			<span class="tab-label">Day</span>
		</button>
		<button class="tab" class:active={tab === 'night'} onclick={() => (tab = 'night')}>
			<span class="tab-glyph">☾</span>
			<span class="tab-label">Night</span>
		</button>
		<button class="tab" class:active={tab === 'library'} onclick={() => (tab = 'library')}>
			<span class="tab-glyph">♪</span>
			<span class="tab-label">Library</span>
		</button>
	</nav>

	<footer class="controls">
		<div class="transport">
			<button
				class="play"
				class:playing={snapshot.isPlaying}
				disabled={!connected}
				aria-label={snapshot.isPlaying ? 'Pause' : 'Play'}
				onclick={togglePlay}
			>
				<span class="play-glyph">{snapshot.isPlaying ? '❚❚' : '▶'}</span>
			</button>
			<button class="switch" disabled={!canSwitch} onclick={() => switchPhase()}>
				<span class="switch-glyph">{isNight ? '☀' : '☾'}</span>
				<span class="switch-text">
					<span class="switch-eyebrow">Switch to</span>
					<span class="switch-phase">{isNight ? 'Day' : 'Night'}</span>
				</span>
			</button>
			<button
				class="timerbtn"
				class:running={timerRunning}
				class:urgent
				aria-label="Timer"
				onclick={() => (timerDialogOpen = true)}
			>
				{#if timeLeft !== null}
					<span class="timerbtn-clock">{formatTimeLeft(timeLeft)}</span>
				{:else}
					<span class="timerbtn-glyph">⏱</span>
				{/if}
			</button>
		</div>
	</footer>

	{#if timerDialogOpen}
		<div
			class="scrim"
			role="presentation"
			onclick={(e) => e.target === e.currentTarget && (timerDialogOpen = false)}
		>
			<div class="dialog" role="dialog" aria-label="Timer" tabindex="-1">
				<p class="dialog-title">Timer</p>
				{#if timeLeft !== null}
					<span class="dialog-clock" class:urgent>{formatTimeLeft(timeLeft)}</span>
					<button class="timer-stop" onclick={handleStopTimer}>Stop timer</button>
				{:else}
					<div class="stepper">
						<span class="step-value">{durationMinutes}<small>min</small></span>
						<div class="step-row">
							<button class="step" onclick={() => stepMinutes(-1)} aria-label="Less time">−</button>
							<button class="step" onclick={() => stepMinutes(1)} aria-label="More time">+</button>
						</div>
					</div>
					<button class="timer-start" disabled={!connected} onclick={handleStartTimer}>
						Start timer
					</button>
				{/if}
			</div>
		</div>
	{/if}
</div>

<style>
	:global(:root) {
		--ink: #0a0608;
		--panel: #15100f;
		--panel-edge: rgba(239, 230, 211, 0.1);
		--row: #1c1614;
		--row-edge: rgba(239, 230, 211, 0.06);
		--line: rgba(239, 230, 211, 0.08);
		--text: #efe6d3;
		--dim: #b9ac93;
		--muted: #7c7263;
		--blood: #c01f1f;
		--blood-deep: #7d0f0f;
		--blood-bright: #e23b34;
		--display: 'Cinzel', 'Times New Roman', serif;
		--body: 'EB Garamond', Georgia, serif;
		/* Phase accent — Day = ember gold */
		--accent: #e3c177;
		--accent-deep: #cd9a3f;
		--accent-rgb: 227, 193, 119;
	}

	:global(body) {
		color: var(--text);
		font-family: var(--body);
		-webkit-font-smoothing: antialiased;
	}

	:global(*) {
		-webkit-tap-highlight-color: transparent;
	}

	.app {
		position: fixed;
		inset: 0;
		display: flex;
		flex-direction: column;
		background:
			radial-gradient(120% 70% at 50% -10%, rgba(var(--accent-rgb), 0.08), transparent 60%),
			linear-gradient(180deg, #100b0a 0%, var(--ink) 60%);
		overflow: hidden;
	}

	/* Night = moon blue */
	.app.night {
		--accent: #cdd6ea;
		--accent-deep: #9aa6c4;
		--accent-rgb: 205, 214, 234;
	}

	/* ---------- Top bar (now playing) ---------- */
	.topbar {
		flex: none;
		padding: calc(env(safe-area-inset-top) + 0.55rem) 0.85rem 0.7rem;
		border-bottom: 1px solid var(--line);
		background: linear-gradient(180deg, rgba(var(--accent-rgb), 0.05), transparent 70%);
		animation: rise 0.5s ease both;
	}

	/* ---------- Bottom controls ---------- */
	.controls {
		flex: none;
		padding: 0.7rem 0.85rem calc(env(safe-area-inset-bottom) + 0.7rem);
		border-top: 1px solid var(--line);
		background: linear-gradient(0deg, rgba(var(--accent-rgb), 0.05), transparent 80%), var(--ink);
		animation: rise 0.5s ease both;
		animation-delay: 0.06s;
	}

	@keyframes rise {
		from {
			opacity: 0;
			transform: translateY(8px);
		}
	}

	/* ---------- Now playing ---------- */
	.now {
		position: relative;
		padding: 0.55rem 0.8rem 0.6rem;
		border: 1px solid var(--panel-edge);
		border-radius: 13px;
		background: linear-gradient(180deg, rgba(var(--accent-rgb), 0.06), rgba(0, 0, 0, 0.2)), var(--panel);
		box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.03);
		overflow: hidden;
	}

	.now::before {
		content: '';
		position: absolute;
		left: 0;
		top: 0;
		bottom: 0;
		width: 3px;
		background: linear-gradient(var(--accent), var(--accent-deep));
		box-shadow: 0 0 14px rgba(var(--accent-rgb), 0.5);
	}

	.now-line {
		display: flex;
		align-items: center;
		gap: 0.45rem;
		margin-bottom: 0.2rem;
	}

	.phase {
		display: inline-flex;
		align-items: baseline;
		gap: 0.35rem;
	}

	.phase-glyph {
		font-size: 0.95rem;
		color: var(--accent);
	}

	.phase-name {
		font-family: var(--display);
		font-weight: 600;
		font-size: 0.78rem;
		letter-spacing: 0.22em;
		text-transform: uppercase;
		color: var(--accent);
	}

	.now-sep {
		color: var(--muted);
		font-size: 0.74rem;
	}

	.phase-state {
		font-size: 0.72rem;
		letter-spacing: 0.16em;
		text-transform: uppercase;
		color: var(--muted);
	}

	.conn {
		margin-left: auto;
		display: inline-flex;
		align-items: center;
	}

	.conn-dot {
		width: 7px;
		height: 7px;
		border-radius: 50%;
		background: #6b2b2b;
	}

	.conn.live .conn-dot {
		background: #4fae6a;
		box-shadow: 0 0 0 0 rgba(79, 174, 106, 0.6);
		animation: pulse 2.4s ease-out infinite;
	}

	@keyframes pulse {
		70%,
		100% {
			box-shadow: 0 0 0 6px rgba(79, 174, 106, 0);
		}
	}

	.np {
		margin: 0;
		display: flex;
		align-items: baseline;
		gap: 0.5rem;
		min-width: 0;
	}

	.np-title {
		flex: 0 1 auto;
		font-size: 1.2rem;
		font-weight: 600;
		line-height: 1.15;
		color: var(--text);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.np-title.np-empty {
		color: var(--muted);
		font-style: italic;
		font-weight: 400;
	}

	.np-artist {
		flex: 1 1 auto;
		font-size: 0.9rem;
		color: var(--dim);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.np-length {
		flex: none;
		font-size: 0.85rem;
		font-variant-numeric: tabular-nums;
		color: var(--muted);
	}

	/* ---------- Timer button (in transport row) ---------- */
	.timerbtn {
		flex: none;
		min-width: 3.6rem;
		padding: 0 0.65rem;
		display: flex;
		align-items: center;
		justify-content: center;
		border: 1px solid var(--panel-edge);
		border-radius: 13px;
		background: #1a1413;
		color: var(--dim);
		cursor: pointer;
		transition: transform 0.08s ease;
	}

	.timerbtn:active {
		transform: scale(0.97);
	}

	.timerbtn-glyph {
		font-size: 1.25rem;
		line-height: 1;
	}

	.timerbtn-clock {
		font-family: var(--display);
		font-weight: 700;
		font-size: 1.2rem;
		font-variant-numeric: tabular-nums;
		color: var(--accent);
	}

	.timerbtn.running {
		border-color: rgba(var(--accent-rgb), 0.35);
		background: rgba(var(--accent-rgb), 0.1);
	}

	.timerbtn.urgent {
		border-color: rgba(226, 59, 52, 0.5);
	}

	.timerbtn.urgent .timerbtn-clock {
		color: var(--blood-bright);
		animation: throb 1s ease-in-out infinite;
	}

	@keyframes throb {
		50% {
			opacity: 0.45;
		}
	}

	/* ---------- Timer dialog ---------- */
	.scrim {
		position: fixed;
		inset: 0;
		z-index: 20;
		display: flex;
		align-items: flex-end;
		justify-content: center;
		padding: 0.85rem;
		padding-bottom: calc(env(safe-area-inset-bottom) + 0.85rem);
		background: rgba(5, 3, 3, 0.62);
		backdrop-filter: blur(2px);
		animation: fade 0.18s ease both;
	}

	@keyframes fade {
		from {
			opacity: 0;
		}
	}

	.dialog {
		width: 100%;
		max-width: 26rem;
		display: flex;
		flex-direction: column;
		align-items: stretch;
		gap: 1.4rem;
		padding: 1.6rem 1.5rem;
		border: 1px solid var(--panel-edge);
		border-radius: 18px;
		background: linear-gradient(180deg, rgba(var(--accent-rgb), 0.06), rgba(0, 0, 0, 0.2)), var(--panel);
		box-shadow: 0 -18px 40px -16px rgba(0, 0, 0, 0.7);
		animation: rise 0.24s ease both;
	}

	.dialog-title {
		margin: 0;
		font-family: var(--display);
		font-weight: 600;
		font-size: 1rem;
		letter-spacing: 0.28em;
		text-transform: uppercase;
		color: var(--accent);
		text-align: center;
	}

	.dialog-clock {
		font-family: var(--display);
		font-weight: 700;
		font-size: 5rem;
		line-height: 1;
		font-variant-numeric: tabular-nums;
		color: var(--accent);
		text-align: center;
	}

	.dialog-clock.urgent {
		color: var(--blood-bright);
		animation: throb 1s ease-in-out infinite;
	}

	.stepper {
		display: flex;
		flex-direction: column;
		align-items: stretch;
		gap: 0.9rem;
	}

	.step-value {
		text-align: center;
		font-family: var(--display);
		font-weight: 600;
		font-size: 3.4rem;
		line-height: 1;
		font-variant-numeric: tabular-nums;
		color: var(--text);
	}

	.step-value small {
		font-family: var(--body);
		font-size: 1.1rem;
		letter-spacing: 0.1em;
		text-transform: uppercase;
		color: var(--muted);
		margin-left: 0.4rem;
	}

	.step-row {
		display: flex;
		gap: 0.9rem;
	}

	.step {
		flex: 1;
		height: 3.6rem;
		border: 1px solid var(--line);
		border-radius: 13px;
		background: var(--row);
		color: var(--accent);
		font-size: 2rem;
		line-height: 1;
		cursor: pointer;
		transition: transform 0.08s ease;
	}

	.step:active {
		transform: scale(0.97);
		background: rgba(var(--accent-rgb), 0.16);
	}

	.timer-start,
	.timer-stop {
		width: 100%;
		height: 3.7rem;
		padding: 0 1.1rem;
		border-radius: 13px;
		border: 1px solid rgba(var(--accent-rgb), 0.35);
		background: rgba(var(--accent-rgb), 0.12);
		color: var(--accent);
		font-family: var(--body);
		font-size: 1.25rem;
		letter-spacing: 0.04em;
		cursor: pointer;
	}

	.timer-start:active,
	.timer-stop:active {
		background: rgba(var(--accent-rgb), 0.22);
	}

	.timer-start:disabled {
		opacity: 0.4;
		cursor: default;
	}

	/* ---------- Transport buttons ---------- */
	.transport {
		display: flex;
		align-items: stretch;
		gap: 0.55rem;
	}

	/* Play/Pause is a secondary action — small square on the left */
	.play {
		flex: none;
		width: 3.6rem;
		display: flex;
		align-items: center;
		justify-content: center;
		border: 1px solid var(--panel-edge);
		border-radius: 13px;
		background: #1a1413;
		color: var(--accent);
		cursor: pointer;
		transition: transform 0.08s ease;
	}

	.play-glyph {
		font-size: 1.05rem;
		line-height: 1;
	}

	.play.playing .play-glyph {
		font-size: 0.95rem;
		color: var(--dim);
	}

	.play:active {
		transform: scale(0.97);
		background: #211a18;
	}

	.play:disabled {
		opacity: 0.4;
		cursor: default;
	}

	/* Switch is the primary action — large and dominant, fills the row */
	.switch {
		flex: 1;
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 0.7rem;
		min-height: 3.7rem;
		border: none;
		border-radius: 13px;
		background: linear-gradient(180deg, var(--blood-bright), var(--blood) 55%, var(--blood-deep));
		color: #fff5ef;
		cursor: pointer;
		box-shadow:
			0 12px 26px -12px rgba(192, 31, 31, 0.75),
			inset 0 1px 0 rgba(255, 255, 255, 0.2);
		transition: transform 0.08s ease;
	}

	.switch-glyph {
		font-size: 1.7rem;
		line-height: 1;
		filter: drop-shadow(0 0 12px rgba(255, 240, 220, 0.35));
	}

	.switch-text {
		display: flex;
		flex-direction: column;
		align-items: flex-start;
		line-height: 1.05;
	}

	.switch-eyebrow {
		font-family: var(--body);
		font-size: 0.78rem;
		letter-spacing: 0.16em;
		text-transform: uppercase;
		color: rgba(255, 245, 239, 0.78);
	}

	.switch-phase {
		font-family: var(--display);
		font-weight: 700;
		font-size: 1.45rem;
		letter-spacing: 0.06em;
		text-transform: uppercase;
	}

	.switch:active {
		transform: scale(0.985);
	}

	.switch:disabled {
		background: linear-gradient(180deg, #241b1a, #181211);
		color: var(--muted);
		box-shadow: inset 0 0 0 1px var(--line);
		cursor: default;
	}

	.switch:disabled .switch-eyebrow {
		color: var(--muted);
	}

	.switch:disabled .switch-glyph {
		filter: none;
		opacity: 0.5;
	}

	/* ---------- Deck (scrolling tab content) ---------- */
	.deck {
		flex: 1;
		min-height: 0;
		overflow-y: auto;
		-webkit-overflow-scrolling: touch;
		padding: 0.85rem 0.85rem 1.2rem;
	}

	/* ---------- Section tabs (below the deck) ---------- */
	.tabbar {
		flex: none;
		display: grid;
		grid-template-columns: repeat(3, 1fr);
		border-top: 1px solid var(--line);
		background: rgba(0, 0, 0, 0.2);
	}

	.tab {
		position: relative;
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 0.4rem;
		min-height: 2.9rem;
		border: none;
		background: none;
		color: var(--muted);
		cursor: pointer;
	}

	.tab-glyph {
		font-size: 1.1rem;
		line-height: 1;
	}

	.tab-label {
		font-size: 0.72rem;
		letter-spacing: 0.16em;
		text-transform: uppercase;
	}

	.tab.active {
		color: var(--accent);
	}

	.tab.active::before {
		content: '';
		position: absolute;
		top: 0;
		left: 50%;
		transform: translateX(-50%);
		width: 2.2rem;
		height: 2px;
		border-radius: 0 0 2px 2px;
		background: var(--accent);
		box-shadow: 0 0 10px rgba(var(--accent-rgb), 0.6);
	}
</style>
