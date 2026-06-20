<script lang="ts">
	import { onMount } from 'svelte';
	import { browserMusicStore, type MusicStore, type SongRecord } from '$lib/musicStore';
	import { createLocalSession, type LocalSession } from '$lib/localSession';
	import { MobileAudioEngine } from '$lib/mobileAudioEngine';
	import { pickDirectoryFiles, supportsDirectoryPicker } from '$lib/importPicker';
	import Playlist from '$shared/ui/Playlist.svelte';
	import { commandError, report } from '$shared/ui/commandError';
	import { emptyState, type PlaybackState } from '$shared/ui/state';
	import { derivePosition } from '$shared/core';

	type Tab = 'day' | 'night' | 'library';

	let store: MusicStore;
	let session = $state<LocalSession>();
	let ready = $state(false);

	let snapshot = $state<PlaybackState>(emptyState);
	let now = $state(Date.now());
	let songs: SongRecord[] = $state([]);
	let evicted: SongRecord[] = $state([]);
	let evictedIds = $derived(new Set(evicted.map((entry) => entry.id)));

	let tab: Tab = $state('day');
	let importing = $state(false);
	let error: string | null = $state(null);
	let notice: string | null = $state(null);
	let canPickFolder = $state(false);
	let fileInput: HTMLInputElement | undefined = $state();

	// The now-playing song and its live position derive from the playback anchor (the engine
	// is the audio clock, but the anchor tracks it closely enough for the indicator); a light
	// 250ms ticker advances `now` so the progress fill animates without a server tick.
	let currentSong = $derived(
		[...snapshot.playlists.day.songs, ...snapshot.playlists.night.songs].find(
			(song) => song.id === snapshot.currentSongId
		) ?? null
	);
	let playProgress = $derived(
		currentSong && currentSong.length > 0
			? Math.min(1, Math.max(0, derivePosition(snapshot.position, now) / currentSong.length))
			: 0
	);
	let activePlaylist = $derived(
		snapshot.phase === 'Night' ? snapshot.playlists.night : snapshot.playlists.day
	);
	let canPlay = $derived(activePlaylist.currentIndex !== null);

	onMount(() => {
		store = browserMusicStore();
		const engine = new MobileAudioEngine((id) => store.audioBlob(id));
		session = createLocalSession(store, () => Date.now(), Math.random, engine);
		canPickFolder = supportsDirectoryPicker();
		const unsubscribe = session.snapshot.subscribe((next) => (snapshot = next.state));
		const ticker = setInterval(() => (now = Date.now()), 250);
		void start();
		return () => {
			unsubscribe();
			clearInterval(ticker);
		};
	});

	function togglePlay(): void {
		if (!session) return;
		void report(snapshot.isPlaying ? session.pause() : session.play());
	}

	async function start(): Promise<void> {
		if (!session) return;
		await session.load();
		await refresh();
		ready = true;
	}

	async function refresh(): Promise<void> {
		songs = await store.songs();
		evicted = await store.evictedSongs();
	}

	async function importFiles(files: File[]): Promise<void> {
		if (files.length === 0) return;
		importing = true;
		error = null;
		notice = null;
		try {
			const result = await store.importFiles(files);
			await refresh();
			notice = summarise(result.added.length, result.duplicates.length);
		} catch (e) {
			error = e instanceof Error ? e.message : 'Import failed';
		} finally {
			importing = false;
		}
	}

	function summarise(added: number, duplicates: number): string {
		const songWord = (n: number) => (n === 1 ? 'song' : 'songs');
		if (duplicates === 0) return `Imported ${added} ${songWord(added)}.`;
		if (added === 0) return `${duplicates} ${songWord(duplicates)} already in the library.`;
		return `Imported ${added} ${songWord(added)}, ${duplicates} already present.`;
	}

	async function pickFolder(): Promise<void> {
		try {
			await importFiles(await pickDirectoryFiles());
		} catch (e) {
			if (e instanceof DOMException && e.name === 'AbortError') return;
			error = e instanceof Error ? e.message : 'Import failed';
		}
	}

	async function handleFiles(event: Event): Promise<void> {
		const input = event.currentTarget as HTMLInputElement;
		const files = [...(input.files ?? [])];
		input.value = '';
		await importFiles(files);
	}

	async function removeFromLibrary(song: SongRecord): Promise<void> {
		await store.remove(song.id);
		await refresh();
	}

	function formatLength(seconds: number): string {
		const minutes = Math.floor(seconds / 60);
		const remainder = Math.floor(seconds % 60);
		return `${minutes}:${remainder.toString().padStart(2, '0')}`;
	}
</script>

<div class="app" class:night={tab === 'night'}>
	<header class="head">
		<span class="glyph">♪</span>
		<h1>Minstrel</h1>
		<p class="sub">{tab === 'library' ? 'Library' : `${tab === 'night' ? 'Night' : 'Day'} Playlist`}</p>
	</header>

	<main class="deck">
		{#if ready && session && tab !== 'library'}
			<Playlist
				api={session}
				phase={tab === 'night' ? 'Night' : 'Day'}
				playlist={tab === 'night' ? snapshot.playlists.night : snapshot.playlists.day}
				currentSongId={snapshot.currentSongId}
				progress={playProgress}
			/>
		{:else if tab === 'library'}
			{#if evicted.length > 0}
				<div class="evicted" role="alert">
					{evicted.length} song{evicted.length === 1 ? '' : 's'} need re-importing — the phone cleared
					their audio. Import the same files to restore them.
				</div>
			{/if}

			{#if songs.length === 0}
				<p class="empty">No songs yet. Import some to begin.</p>
			{:else}
				<ul class="songs">
					{#each songs as song (song.id)}
						<li class="song" class:gone={evictedIds.has(song.id)}>
							<span class="details">
								<span class="title">{song.title}</span>
								<span class="artist">{song.artist}</span>
							</span>
							{#if evictedIds.has(song.id)}
								<span class="reimport">re-import</span>
							{:else}
								<span class="length">{formatLength(song.length)}</span>
							{/if}
							<button
								class="add day"
								title="Add to Day playlist"
								onclick={() => session && report(session.addSong('Day', song.id))}>☀</button
							>
							<button
								class="add night"
								title="Add to Night playlist"
								onclick={() => session && report(session.addSong('Night', song.id))}>☾</button
							>
							<button class="remove" aria-label="Remove" onclick={() => removeFromLibrary(song)}>✕</button>
						</li>
					{/each}
				</ul>
			{/if}

			<input
				bind:this={fileInput}
				class="hidden-input"
				type="file"
				accept="audio/*"
				multiple
				onchange={handleFiles}
			/>
			{#if canPickFolder}
				<button class="import" disabled={importing} onclick={pickFolder}>
					{importing ? 'Importing…' : 'Import a folder'}
				</button>
			{:else}
				<button class="import" disabled={importing} onclick={() => fileInput?.click()}>
					{importing ? 'Importing…' : 'Import songs'}
				</button>
			{/if}
		{/if}
	</main>

	<footer class="transport" class:night={snapshot.phase === 'Night'}>
		<div class="np">
			{#if currentSong}
				<span class="np-title">{currentSong.title}</span>
				<span class="np-artist">{currentSong.artist}</span>
			{:else}
				<span class="np-title np-empty">Nothing cued</span>
				<span class="np-artist">{snapshot.phase} · choose a song to play</span>
			{/if}
			<span class="np-bar"><span class="np-fill" style="width: {playProgress * 100}%"></span></span>
		</div>
		<button
			class="play"
			class:playing={snapshot.isPlaying}
			disabled={!canPlay}
			aria-label={snapshot.isPlaying ? 'Pause' : 'Play'}
			onclick={togglePlay}
		>
			<span class="play-glyph">{snapshot.isPlaying ? '❚❚' : '▶'}</span>
		</button>
		<button
			class="switch"
			aria-label="Switch phase"
			onclick={() => session && report(session.switchPhase())}
		>
			<span class="switch-glyph">{snapshot.phase === 'Night' ? '☀' : '☾'}</span>
		</button>
	</footer>

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

	{#if error}
		<div class="toast" role="alert">{error}</div>
	{:else if notice}
		<div class="toast notice" role="status">{notice}</div>
	{:else if $commandError}
		<div class="toast" role="alert">{$commandError}</div>
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
		--blood-bright: #e23b34;
		--accent: #e3c177;
		--accent-deep: #cd9a3f;
		--accent-rgb: 227, 193, 119;
		--display: 'Cinzel', 'Times New Roman', serif;
		--body: 'EB Garamond', Georgia, serif;
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

	.app.night {
		--accent: #cdd6ea;
		--accent-deep: #9aa6c4;
		--accent-rgb: 205, 214, 234;
	}

	.head {
		flex: none;
		padding: calc(env(safe-area-inset-top) + 1rem) 1rem 0.9rem;
		text-align: center;
		border-bottom: 1px solid rgba(239, 230, 211, 0.08);
	}

	.glyph {
		font-size: 1.6rem;
		color: var(--accent);
	}

	.head h1 {
		margin: 0.2rem 0 0;
		font-family: var(--display);
		font-weight: 700;
		font-size: 1.5rem;
		letter-spacing: 0.12em;
		color: var(--text);
	}

	.sub {
		margin: 0.15rem 0 0;
		font-size: 0.72rem;
		letter-spacing: 0.22em;
		text-transform: uppercase;
		color: var(--muted);
	}

	.deck {
		flex: 1;
		min-height: 0;
		overflow-y: auto;
		-webkit-overflow-scrolling: touch;
		padding: 1rem;
	}

	.empty {
		margin-top: 3rem;
		text-align: center;
		color: var(--muted);
		font-style: italic;
	}

	.songs {
		list-style: none;
		margin: 0 0 1rem;
		padding: 0;
		display: flex;
		flex-direction: column;
		gap: 0.55rem;
	}

	.song {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		padding: 0.6rem 0.7rem;
		border: 1px solid var(--panel-edge);
		border-radius: 12px;
		background: var(--panel);
	}

	.song.gone {
		opacity: 0.7;
		border-color: rgba(226, 59, 52, 0.4);
	}

	.details {
		flex: 1;
		min-width: 0;
		display: flex;
		flex-direction: column;
		gap: 0.12rem;
	}

	.title {
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
		font-size: 1.05rem;
		color: var(--text);
	}

	.artist {
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
		font-size: 0.88rem;
		color: var(--dim);
	}

	.length {
		flex: none;
		font-size: 0.85rem;
		font-variant-numeric: tabular-nums;
		color: var(--muted);
	}

	.reimport {
		flex: none;
		font-size: 0.78rem;
		letter-spacing: 0.04em;
		color: var(--blood-bright);
	}

	.add {
		flex: none;
		width: 2.5rem;
		height: 2.5rem;
		border-radius: 9px;
		border: 1px solid var(--line);
		background: rgba(0, 0, 0, 0.25);
		font-size: 1.05rem;
		cursor: pointer;
	}

	.add.day {
		color: #e3c177;
	}

	.add.day:active {
		background: rgba(227, 193, 119, 0.18);
		border-color: rgba(227, 193, 119, 0.4);
	}

	.add.night {
		color: #cdd6ea;
	}

	.add.night:active {
		background: rgba(205, 214, 234, 0.18);
		border-color: rgba(205, 214, 234, 0.4);
	}

	.remove {
		flex: none;
		width: 2.4rem;
		height: 2.4rem;
		border: 1px solid var(--panel-edge);
		border-radius: 9px;
		background: #1a1413;
		color: var(--muted);
		font-size: 0.85rem;
		cursor: pointer;
	}

	.remove:active {
		color: var(--blood-bright);
		border-color: rgba(226, 59, 52, 0.4);
	}

	.evicted {
		margin-bottom: 0.9rem;
		padding: 0.7rem 0.85rem;
		border: 1px solid rgba(226, 59, 52, 0.5);
		border-radius: 11px;
		background: linear-gradient(180deg, #2a1413, #1c0f0e);
		color: #f3d9d4;
		font-size: 0.9rem;
		line-height: 1.35;
	}

	.import {
		width: 100%;
		min-height: 3.5rem;
		border: none;
		border-radius: 13px;
		background: linear-gradient(180deg, var(--blood-bright), var(--blood) 60%, #7d0f0f);
		color: #fff5ef;
		font-family: var(--display);
		font-weight: 700;
		font-size: 1.1rem;
		letter-spacing: 0.06em;
		cursor: pointer;
		box-shadow: 0 12px 26px -12px rgba(192, 31, 31, 0.75);
	}

	.import:disabled {
		opacity: 0.55;
		cursor: default;
	}

	.hidden-input {
		display: none;
	}

	.transport {
		--accent: #e3c177;
		--accent-rgb: 227, 193, 119;
		flex: none;
		display: flex;
		align-items: center;
		gap: 0.7rem;
		padding: 0.6rem 0.85rem;
		border-top: 1px solid var(--line);
		background: linear-gradient(0deg, rgba(var(--accent-rgb), 0.05), transparent 80%), var(--ink);
	}

	.transport.night {
		--accent: #cdd6ea;
		--accent-rgb: 205, 214, 234;
	}

	.np {
		flex: 1;
		min-width: 0;
		display: flex;
		flex-direction: column;
		gap: 0.1rem;
	}

	.np-title {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
		font-size: 1rem;
		color: var(--text);
	}

	.np-title.np-empty {
		color: var(--muted);
		font-style: italic;
	}

	.np-artist {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
		font-size: 0.82rem;
		color: var(--dim);
	}

	.np-bar {
		margin-top: 0.3rem;
		height: 3px;
		border-radius: 999px;
		background: rgba(239, 230, 211, 0.12);
		overflow: hidden;
	}

	.np-fill {
		display: block;
		height: 100%;
		background: var(--accent);
		box-shadow: 0 0 8px rgba(var(--accent-rgb), 0.6);
		transition: width 0.26s linear;
	}

	.play {
		flex: none;
		width: 3.4rem;
		height: 3.4rem;
		border-radius: 50%;
		border: 1px solid rgba(var(--accent-rgb), 0.5);
		background: rgba(var(--accent-rgb), 0.12);
		color: var(--accent);
		font-size: 1.1rem;
		cursor: pointer;
	}

	.play:disabled {
		opacity: 0.4;
		cursor: default;
	}

	.play.playing {
		background: rgba(var(--accent-rgb), 0.22);
	}

	.play-glyph {
		line-height: 1;
	}

	.switch {
		flex: none;
		width: 3.4rem;
		height: 3.4rem;
		border-radius: 13px;
		border: 1px solid var(--line);
		background: rgba(0, 0, 0, 0.25);
		color: var(--accent);
		font-size: 1.25rem;
		cursor: pointer;
	}

	.tabbar {
		flex: none;
		display: grid;
		grid-template-columns: repeat(3, 1fr);
		border-top: 1px solid var(--line);
		background: rgba(0, 0, 0, 0.2);
		padding-bottom: env(safe-area-inset-bottom);
	}

	.tab {
		position: relative;
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 0.4rem;
		min-height: 3.2rem;
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

	.toast {
		position: fixed;
		left: 50%;
		bottom: calc(env(safe-area-inset-bottom) + 4.5rem);
		transform: translateX(-50%);
		max-width: calc(100% - 2rem);
		padding: 0.7rem 1.1rem;
		border: 1px solid rgba(226, 59, 52, 0.5);
		border-radius: 11px;
		background: linear-gradient(180deg, #2a1413, #1c0f0e);
		color: #f3d9d4;
		font-size: 0.95rem;
		text-align: center;
	}

	.toast.notice {
		border-color: rgba(227, 193, 119, 0.4);
		background: linear-gradient(180deg, #221a10, #161009);
		color: var(--accent);
	}
</style>
