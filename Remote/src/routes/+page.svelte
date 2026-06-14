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
</script>

<div class="app" class:night={isNight}>
	<header class="hub">
		<div class="brandbar">
			<span class="wordmark">Minstrel</span>
			<span class="conn" class:live={connected} title={connected ? 'Connected' : 'Reconnecting'}>
				<span class="conn-dot"></span>
				{connected ? 'Live' : 'Offline'}
			</span>
		</div>

		<div class="nowcard">
			<div class="phase-pill">
				<span class="phase-glyph">{isNight ? '☾' : '☀'}</span>
				<span class="phase-name">{isNight ? 'Night' : 'Day'}</span>
				<span class="phase-state">{snapshot.isPlaying ? 'playing' : 'paused'}</span>
			</div>
			{#if currentSong}
				<p class="np-title">{currentSong.title}</p>
				<p class="np-artist">{currentSong.artist}</p>
			{:else}
				<p class="np-title np-empty">Nothing cued</p>
				<p class="np-artist">Choose a song from a playlist</p>
			{/if}
		</div>

		<div class="timer" class:running={timerRunning} class:urgent>
			{#if timeLeft !== null}
				<span class="timer-label">Time remaining</span>
				<span class="timer-clock">{formatTimeLeft(timeLeft)}</span>
				<button class="timer-stop" onclick={() => stopTimer()}>Stop</button>
			{:else}
				<div class="stepper">
					<button class="step" onclick={() => stepMinutes(-1)} aria-label="Less time">−</button>
					<span class="step-value">{durationMinutes}<small>min</small></span>
					<button class="step" onclick={() => stepMinutes(1)} aria-label="More time">+</button>
				</div>
				<button
					class="timer-start"
					disabled={!connected}
					onclick={() => startTimer(durationMinutes * 60)}>Start timer</button
				>
			{/if}
		</div>

		<div class="transport">
			<button class="switch" disabled={!canSwitch} onclick={() => switchPhase()}>
				<span class="switch-glyph">{isNight ? '☀' : '☾'}</span>
				<span class="switch-text">
					<span class="switch-eyebrow">Switch to</span>
					<span class="switch-phase">{isNight ? 'Day' : 'Night'}</span>
				</span>
			</button>
			<button
				class="play"
				class:playing={snapshot.isPlaying}
				disabled={!connected}
				onclick={togglePlay}
			>
				<span class="play-glyph">{snapshot.isPlaying ? '❚❚' : '▶'}</span>
				<span class="play-text">{snapshot.isPlaying ? 'Pause' : 'Play'}</span>
			</button>
		</div>
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

	/* ---------- Transport hub ---------- */
	.hub {
		flex: none;
		padding: calc(env(safe-area-inset-top) + 0.6rem) 0.85rem 0.85rem;
		display: flex;
		flex-direction: column;
		gap: 0.7rem;
		border-bottom: 1px solid var(--line);
		background: linear-gradient(180deg, rgba(var(--accent-rgb), 0.05), transparent 70%);
	}

	.hub > * {
		animation: rise 0.5s ease both;
	}
	.nowcard {
		animation-delay: 0.04s;
	}
	.timer {
		animation-delay: 0.08s;
	}
	.transport {
		animation-delay: 0.12s;
	}

	@keyframes rise {
		from {
			opacity: 0;
			transform: translateY(8px);
		}
	}

	.brandbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.wordmark {
		font-family: var(--display);
		font-weight: 700;
		font-size: 0.95rem;
		letter-spacing: 0.32em;
		text-transform: uppercase;
		color: var(--accent);
		text-shadow: 0 0 18px rgba(var(--accent-rgb), 0.25);
	}

	.conn {
		display: inline-flex;
		align-items: center;
		gap: 0.4rem;
		font-size: 0.74rem;
		letter-spacing: 0.18em;
		text-transform: uppercase;
		color: var(--muted);
	}

	.conn-dot {
		width: 7px;
		height: 7px;
		border-radius: 50%;
		background: #6b2b2b;
	}

	.conn.live {
		color: var(--dim);
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

	.nowcard {
		position: relative;
		padding: 0.85rem 1rem;
		border: 1px solid var(--panel-edge);
		border-radius: 14px;
		background: linear-gradient(180deg, rgba(var(--accent-rgb), 0.06), rgba(0, 0, 0, 0.2)), var(--panel);
		box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.03);
		overflow: hidden;
	}

	.nowcard::before {
		content: '';
		position: absolute;
		left: 0;
		top: 0;
		bottom: 0;
		width: 3px;
		background: linear-gradient(var(--accent), var(--accent-deep));
		box-shadow: 0 0 14px rgba(var(--accent-rgb), 0.5);
	}

	.phase-pill {
		display: inline-flex;
		align-items: baseline;
		gap: 0.45rem;
		margin-bottom: 0.45rem;
	}

	.phase-glyph {
		font-size: 1.1rem;
		color: var(--accent);
	}

	.phase-name {
		font-family: var(--display);
		font-weight: 600;
		font-size: 0.82rem;
		letter-spacing: 0.24em;
		text-transform: uppercase;
		color: var(--accent);
	}

	.phase-state {
		font-size: 0.74rem;
		letter-spacing: 0.16em;
		text-transform: uppercase;
		color: var(--muted);
	}

	.np-title {
		margin: 0;
		font-size: 1.65rem;
		font-weight: 600;
		line-height: 1.1;
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
		margin: 0.2rem 0 0;
		font-size: 1.1rem;
		color: var(--dim);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	/* ---------- Timer ---------- */
	.timer {
		display: flex;
		align-items: center;
		gap: 0.6rem;
		min-height: 3rem;
		padding: 0.4rem 0.55rem 0.4rem 0.85rem;
		border: 1px solid var(--line);
		border-radius: 12px;
		background: rgba(0, 0, 0, 0.22);
	}

	.timer.running {
		border-color: rgba(var(--accent-rgb), 0.3);
	}

	.timer-label {
		flex: 1;
		font-size: 0.74rem;
		letter-spacing: 0.18em;
		text-transform: uppercase;
		color: var(--muted);
	}

	.timer-clock {
		font-family: var(--display);
		font-weight: 700;
		font-size: 1.95rem;
		font-variant-numeric: tabular-nums;
		letter-spacing: 0.02em;
		color: var(--accent);
	}

	.timer.urgent .timer-clock {
		color: var(--blood-bright);
		animation: throb 1s ease-in-out infinite;
	}

	.timer.urgent {
		border-color: rgba(226, 59, 52, 0.5);
	}

	@keyframes throb {
		50% {
			opacity: 0.45;
		}
	}

	.stepper {
		flex: 1;
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}

	.step {
		width: 2.4rem;
		height: 2.4rem;
		flex: none;
		border: 1px solid var(--line);
		border-radius: 9px;
		background: var(--row);
		color: var(--text);
		font-size: 1.4rem;
		line-height: 1;
		cursor: pointer;
	}

	.step:active {
		background: rgba(var(--accent-rgb), 0.16);
	}

	.step-value {
		min-width: 3.6rem;
		text-align: center;
		font-family: var(--display);
		font-weight: 600;
		font-size: 1.4rem;
		font-variant-numeric: tabular-nums;
		color: var(--text);
	}

	.step-value small {
		font-family: var(--body);
		font-size: 0.8rem;
		letter-spacing: 0.1em;
		text-transform: uppercase;
		color: var(--muted);
		margin-left: 0.25rem;
	}

	.timer-start,
	.timer-stop {
		flex: none;
		height: 2.6rem;
		padding: 0 1.1rem;
		border-radius: 9px;
		border: 1px solid rgba(var(--accent-rgb), 0.35);
		background: rgba(var(--accent-rgb), 0.12);
		color: var(--accent);
		font-family: var(--body);
		font-size: 0.95rem;
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
		flex-direction: column;
		gap: 0.55rem;
	}

	/* Switch is the primary action — large and dominant */
	.switch {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 0.85rem;
		min-height: 4.9rem;
		border: none;
		border-radius: 16px;
		background: linear-gradient(180deg, var(--blood-bright), var(--blood) 55%, var(--blood-deep));
		color: #fff5ef;
		cursor: pointer;
		box-shadow:
			0 14px 30px -12px rgba(192, 31, 31, 0.75),
			inset 0 1px 0 rgba(255, 255, 255, 0.2);
		transition: transform 0.08s ease;
	}

	.switch-glyph {
		font-size: 2.1rem;
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
		font-size: 0.82rem;
		letter-spacing: 0.16em;
		text-transform: uppercase;
		color: rgba(255, 245, 239, 0.78);
	}

	.switch-phase {
		font-family: var(--display);
		font-weight: 700;
		font-size: 1.7rem;
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

	/* Play/Pause is a secondary, infrequent action */
	.play {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 0.55rem;
		min-height: 3rem;
		border: 1px solid var(--panel-edge);
		border-radius: 12px;
		background: #1a1413;
		color: var(--dim);
		font-family: var(--body);
		font-size: 1.05rem;
		letter-spacing: 0.08em;
		text-transform: uppercase;
		cursor: pointer;
		transition: transform 0.08s ease;
	}

	.play-glyph {
		font-size: 0.9rem;
		color: var(--accent);
	}

	.play.playing .play-glyph {
		color: var(--dim);
	}

	.play:active {
		transform: scale(0.99);
		background: #211a18;
	}

	.play:disabled {
		opacity: 0.4;
		cursor: default;
	}

	/* ---------- Deck (scrolling tab content) ---------- */
	.deck {
		flex: 1;
		min-height: 0;
		overflow-y: auto;
		-webkit-overflow-scrolling: touch;
		padding: 0.85rem 0.85rem 1.2rem;
	}

	/* ---------- Bottom tab bar ---------- */
	.tabbar {
		flex: none;
		display: grid;
		grid-template-columns: repeat(3, 1fr);
		border-top: 1px solid var(--line);
		background: linear-gradient(180deg, #120d0c, var(--ink));
		padding-bottom: env(safe-area-inset-bottom);
	}

	.tab {
		position: relative;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: 0.15rem;
		min-height: 3.6rem;
		border: none;
		background: none;
		color: var(--muted);
		cursor: pointer;
	}

	.tab-glyph {
		font-size: 1.3rem;
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
