<script lang="ts">
	import { moveSong, removeSong, selectSong, setGain, shuffle } from './commandClient';
	import type { Phase, PlaylistDto, SongDto } from './state';

	let {
		phase,
		playlist,
		currentSongId,
		progress
	}: { phase: Phase; playlist: PlaylistDto; currentSongId: string | null; progress: number } =
		$props();

	interface Row {
		key: string;
		song: SongDto;
	}

	// Progress fraction for the cued row: live position when this playlist's
	// song is the one playing, otherwise the offset it was paused at on switch.
	const cueFraction = $derived.by(() => {
		const index = playlist.currentIndex;
		if (index === null) {
			return 0;
		}
		const song = playlist.songs[index];
		if (!song || song.length <= 0) {
			return 0;
		}
		return song.id === currentSongId
			? progress
			: Math.min(1, Math.max(0, playlist.resumeOffset / song.length));
	});

	let listEl: HTMLUListElement | undefined = $state();
	let rows: Row[] = $state([]);
	let lastSongs: SongDto[] | null = null;

	// Drag state
	let dragging = $state(false);
	let dragIndex = $state(-1); // current slot of the dragged row in `rows`
	let startIndex = -1; // slot where the drag began
	let grabOffset = 0; // pointer offset within the grabbed row
	let rowStep = 0; // row height including the flex gap
	let dragY = $state(0); // translateY applied to the lifted row

	// Sync from server state, but never clobber an in-progress drag or the
	// optimistic order we keep right after a drop until the server echoes it.
	$effect(() => {
		const songs = playlist.songs;
		if (dragging || songs === lastSongs) {
			return;
		}
		lastSongs = songs;
		rows = songs.map((song, i) => ({ key: `${song.id}-${i}`, song }));
	});

	function formatLength(seconds: number): string {
		const minutes = Math.floor(seconds / 60);
		const remainder = Math.floor(seconds % 60);
		return `${minutes}:${remainder.toString().padStart(2, '0')}`;
	}

	function onGrab(event: PointerEvent, index: number): void {
		if (!listEl) {
			return;
		}
		event.preventDefault();
		const items = listEl.querySelectorAll('li');
		const rect = items[index].getBoundingClientRect();
		rowStep = items.length > 1 ? items[1].getBoundingClientRect().top - items[0].getBoundingClientRect().top : rect.height;
		grabOffset = event.clientY - rect.top;

		dragging = true;
		dragIndex = index;
		startIndex = index;
		dragY = 0;
		(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
	}

	function onDrag(event: PointerEvent): void {
		if (!dragging || !listEl) {
			return;
		}
		const listTop = listEl.getBoundingClientRect().top;
		const liftedTop = event.clientY - grabOffset - listTop;

		let target = Math.round(liftedTop / rowStep);
		target = Math.max(0, Math.min(rows.length - 1, target));
		if (target !== dragIndex) {
			const [moved] = rows.splice(dragIndex, 1);
			rows.splice(target, 0, moved);
			rows = rows;
			dragIndex = target;
		}
		dragY = liftedTop - dragIndex * rowStep;
	}

	function onRelease(event: PointerEvent): void {
		if (!dragging) {
			return;
		}
		(event.currentTarget as HTMLElement).releasePointerCapture?.(event.pointerId);
		dragging = false;
		dragY = 0;
		if (dragIndex !== startIndex) {
			void moveSong(phase, startIndex, dragIndex);
		}
		dragIndex = -1;
	}
</script>

<section class:night={phase === 'Night'}>
	<div class="header">
		<h2>
			<span class="glyph">{phase === 'Night' ? '☾' : '☀'}</span>
			{phase}
			<span class="count">{playlist.songs.length}</span>
		</h2>
		<button class="shuffle" title="Shuffle {phase} playlist" onclick={() => shuffle(phase)}>
			<span>⇄</span> Shuffle
		</button>
	</div>

	<label class="volume">
		<span class="vol-glyph">♪</span>
		<input
			type="range"
			min="0"
			max="1"
			step="0.01"
			style="--val: {playlist.gain}"
			value={playlist.gain}
			oninput={(event) => setGain(phase, event.currentTarget.valueAsNumber)}
		/>
		<span class="vol-pct">{Math.round(playlist.gain * 100)}</span>
	</label>

	{#if rows.length === 0}
		<p class="empty">No songs yet. Add some from the Library.</p>
	{:else}
		<ul bind:this={listEl} class:dragging>
			{#each rows as row, index (row.key)}
				<li
					class:current={index === playlist.currentIndex}
					class:lifted={dragging && index === dragIndex}
					style={dragging && index === dragIndex ? `transform: translateY(${dragY}px)` : ''}
				>
					{#if index === playlist.currentIndex && cueFraction > 0}
						<span class="fill" style="width: {cueFraction * 100}%"></span>
					{/if}
					<button class="select" onclick={() => selectSong(phase, index)}>
						{#if index === playlist.currentIndex}
							<span class="bars" aria-hidden="true"><i></i><i></i><i></i></span>
						{/if}
						<span class="details">
							<span class="title">{row.song.title}</span>
							<span class="artist">{row.song.artist}</span>
						</span>
					</button>
					<span class="length">{formatLength(row.song.length)}</span>
					<button
						class="remove"
						title="Remove from playlist"
						onclick={() => removeSong(phase, index)}>✕</button
					>
					<button
						class="grip"
						aria-label="Drag to reorder"
						onpointerdown={(event) => onGrab(event, index)}
						onpointermove={onDrag}
						onpointerup={onRelease}
						onpointercancel={onRelease}>⠿</button
					>
				</li>
			{/each}
		</ul>
	{/if}
</section>

<style>
	section {
		/* Day = ember; overridden for Night below */
		--accent: #e3c177;
		--accent-deep: #cd9a3f;
		--accent-rgb: 227, 193, 119;
	}

	section.night {
		--accent: #cdd6ea;
		--accent-deep: #9aa6c4;
		--accent-rgb: 205, 214, 234;
	}

	.header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 0.6rem;
	}

	h2 {
		display: flex;
		align-items: center;
		gap: 0.45rem;
		margin: 0;
		font-family: var(--display);
		font-size: 0.95rem;
		font-weight: 600;
		letter-spacing: 0.2em;
		text-transform: uppercase;
		color: var(--accent);
	}

	.glyph {
		font-size: 1.1rem;
	}

	.count {
		font-family: var(--body);
		font-size: 0.8rem;
		letter-spacing: 0.05em;
		color: var(--muted);
		background: rgba(var(--accent-rgb), 0.1);
		padding: 0.05rem 0.5rem;
		border-radius: 999px;
	}

	.shuffle {
		display: inline-flex;
		align-items: center;
		gap: 0.4rem;
		font-family: var(--body);
		font-size: 0.82rem;
		letter-spacing: 0.08em;
		text-transform: uppercase;
		padding: 0.45rem 0.8rem;
		border: 1px solid var(--line);
		border-radius: 9px;
		background: var(--row);
		color: var(--dim);
		cursor: pointer;
	}

	.shuffle span {
		font-size: 1rem;
		color: var(--accent);
	}

	.shuffle:active {
		background: rgba(var(--accent-rgb), 0.14);
	}

	/* ---------- Volume slider ---------- */
	.volume {
		display: flex;
		align-items: center;
		gap: 0.65rem;
		padding: 0.6rem 0.85rem;
		margin-bottom: 0.75rem;
		border: 1px solid var(--line);
		border-radius: 11px;
		background: rgba(0, 0, 0, 0.22);
	}

	.vol-glyph {
		color: var(--accent);
		font-size: 1.05rem;
	}

	.vol-pct {
		min-width: 2.5ch;
		text-align: right;
		font-size: 0.85rem;
		font-variant-numeric: tabular-nums;
		color: var(--dim);
	}

	input[type='range'] {
		flex: 1;
		appearance: none;
		-webkit-appearance: none;
		height: 5px;
		border-radius: 999px;
		background: linear-gradient(
			to right,
			var(--accent) 0%,
			var(--accent) calc(var(--val, 1) * 100%),
			rgba(239, 230, 211, 0.14) calc(var(--val, 1) * 100%)
		);
	}

	input[type='range']::-webkit-slider-thumb {
		-webkit-appearance: none;
		width: 1.5rem;
		height: 1.5rem;
		border-radius: 50%;
		background: var(--accent);
		border: 3px solid var(--ink);
		box-shadow: 0 0 10px rgba(var(--accent-rgb), 0.5);
		cursor: pointer;
	}

	input[type='range']::-moz-range-thumb {
		width: 1.5rem;
		height: 1.5rem;
		border-radius: 50%;
		background: var(--accent);
		border: 3px solid var(--ink);
		cursor: pointer;
	}

	.empty {
		margin: 0;
		padding: 1.4rem 0.5rem;
		text-align: center;
		font-style: italic;
		color: var(--muted);
		font-size: 0.95rem;
	}

	ul {
		list-style: none;
		margin: 0;
		padding: 0;
		display: flex;
		flex-direction: column;
		gap: 0.4rem;
	}

	li {
		position: relative;
		overflow: hidden;
		display: flex;
		align-items: center;
		gap: 0.3rem;
		padding: 0.5rem 0.4rem 0.5rem 0.7rem;
		border: 1px solid var(--row-edge);
		border-radius: 11px;
		background: var(--row);
	}

	/* Playback progress fill — behind the row content */
	.fill {
		position: absolute;
		top: 0;
		left: 0;
		bottom: 0;
		width: 0;
		background: linear-gradient(
			90deg,
			rgba(var(--accent-rgb), 0.08),
			rgba(var(--accent-rgb), 0.26)
		);
		border-right: 2px solid rgba(var(--accent-rgb), 0.7);
		box-shadow: 0 0 12px rgba(var(--accent-rgb), 0.35);
		transition: width 0.26s linear;
		pointer-events: none;
	}

	li > :not(.fill) {
		position: relative;
	}

	/* Rows glide into place as the lifted row passes over them */
	ul.dragging li:not(.lifted) {
		transition: transform 0.18s ease;
	}

	li.current {
		border-color: rgba(var(--accent-rgb), 0.45);
		background: linear-gradient(90deg, rgba(var(--accent-rgb), 0.12), var(--row) 70%);
	}

	li.lifted {
		position: relative;
		z-index: 5;
		border-color: rgba(var(--accent-rgb), 0.6);
		background: #241d1b;
		box-shadow: 0 16px 30px -10px rgba(0, 0, 0, 0.7);
		cursor: grabbing;
	}

	.select {
		flex: 1;
		min-width: 0;
		display: flex;
		align-items: center;
		gap: 0.6rem;
		min-height: 2.7rem;
		background: none;
		border: none;
		padding: 0;
		font: inherit;
		text-align: left;
		color: inherit;
		cursor: pointer;
	}

	.details {
		min-width: 0;
		display: flex;
		flex-direction: column;
		gap: 0.12rem;
	}

	.title {
		font-size: 1.1rem;
		font-weight: 600;
		color: var(--text);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	li.current .title {
		color: var(--accent);
	}

	.artist {
		font-size: 0.9rem;
		color: var(--dim);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	/* Equalizer bars on the playing row */
	.bars {
		display: inline-flex;
		align-items: flex-end;
		gap: 2px;
		height: 1rem;
		flex: none;
	}

	.bars i {
		width: 3px;
		background: var(--accent);
		border-radius: 1px;
		animation: eq 0.9s ease-in-out infinite;
	}

	.bars i:nth-child(1) {
		height: 40%;
		animation-delay: -0.2s;
	}
	.bars i:nth-child(2) {
		height: 90%;
		animation-delay: -0.5s;
	}
	.bars i:nth-child(3) {
		height: 60%;
	}

	@keyframes eq {
		0%,
		100% {
			transform: scaleY(0.4);
		}
		50% {
			transform: scaleY(1);
		}
	}

	.length {
		flex: none;
		font-size: 0.85rem;
		font-variant-numeric: tabular-nums;
		color: var(--muted);
	}

	.remove {
		flex: none;
		width: 2.5rem;
		height: 2.5rem;
		border: none;
		border-radius: 8px;
		background: none;
		color: var(--muted);
		font-size: 0.95rem;
		cursor: pointer;
	}

	.remove:active {
		background: rgba(192, 31, 31, 0.18);
		color: var(--blood-bright);
	}

	/* Drag handle — touch-action:none keeps the page from scrolling mid-drag */
	.grip {
		flex: none;
		width: 2.6rem;
		height: 2.7rem;
		border: none;
		border-radius: 8px;
		background: none;
		color: var(--muted);
		font-size: 1.3rem;
		line-height: 1;
		cursor: grab;
		touch-action: none;
	}

	.grip:active {
		cursor: grabbing;
		color: var(--accent);
	}
</style>
