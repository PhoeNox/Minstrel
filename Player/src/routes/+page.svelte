<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import { playbackStore } from '$lib/sseStore';
	import { AudioEngine } from '$lib/audioEngine';
	import { PlaybackSync } from '$lib/playbackSync';
	import { emptyState, type PlaybackState } from '$lib/state';
	import { fetchRemoteUrl } from '$lib/connection';
	import { fetchVersion } from '$lib/version';
	import { qrSvg } from '$lib/qr';
	import { deriveTimeLeft, timerStore, type TimerAnchor } from '$shared/timer';

	const QR_DISMISSED_KEY = 'minstrel.qrDismissed';

	interface Countdown {
		value: string;
		unit: 'min' | 'sec';
	}

	function formatCountdown(seconds: number): Countdown {
		if (seconds >= 60) {
			return { value: String(Math.round(seconds / 60)), unit: 'min' };
		}
		return { value: String(Math.min(59, Math.ceil(seconds))), unit: 'sec' };
	}

	const timer = timerStore((gain) => void engine?.playGong(gain));

	let engine: AudioEngine | null = $state(null);
	let snapshot: PlaybackState = $state(emptyState);
	let connected = $state(false);
	let audioFailed = $state(false);
	let timerAnchor: TimerAnchor | null = $state(null);
	let now = $state(Date.now());
	let qr: string | null = $state(null);
	let remoteUrl: string | null = $state(null);
	let version: string | null = $state(null);
	let qrVisible = $state(true);

	const sync = new PlaybackSync(playbackStore(), {
		onConnection: (connection) => {
			snapshot = connection.state;
			connected = connection.connected;
		},
		onTick: () => {
			now = Date.now();
		},
		onError: (error) => {
			audioFailed = error !== null;
		}
	});
	sync.start();

	const unsubscribeTimer = timer.subscribe((next) => {
		timerAnchor = next;
	});

	onMount(async () => {
		qrVisible = localStorage.getItem(QR_DISMISSED_KEY) !== '1';
		remoteUrl = await fetchRemoteUrl();
		qr = qrSvg(remoteUrl);
		version = await fetchVersion();
	});

	onDestroy(() => {
		sync.stop();
		unsubscribeTimer();
	});

	const timeLeft = $derived(timerAnchor ? deriveTimeLeft(timerAnchor, now) : null);
	const isNight = $derived(snapshot.phase === 'Night');
	const expired = $derived(timeLeft !== null && timeLeft <= 0);
	const urgent = $derived(timeLeft !== null && timeLeft > 0 && timeLeft <= 10);
	const countdown = $derived(timeLeft !== null ? formatCountdown(timeLeft) : null);

	function enableSound(): void {
		if (engine) {
			return;
		}

		engine = new AudioEngine();
		sync.attach(engine);
	}

	function dismissQr(): void {
		qrVisible = false;
		localStorage.setItem(QR_DISMISSED_KEY, '1');
	}

	function revealQr(): void {
		qrVisible = true;
		localStorage.removeItem(QR_DISMISSED_KEY);
	}
</script>

<div class="stage" class:night={isNight} aria-hidden="true">
	<img class="backdrop" src="/artwork.webp" alt="" />
	<div class="vignette"></div>
	<div class="grain"></div>
</div>

{#if !connected}
	<div class="offline" role="status">
		<svg
			class="offline-glyph"
			viewBox="0 0 24 24"
			fill="none"
			stroke="currentColor"
			stroke-width="2.4"
			stroke-linecap="round"
			aria-hidden="true"
		>
			<path d="M21 12a9 9 0 1 1-9-9" />
		</svg>
		<span>Reconnecting…</span>
	</div>
{:else if audioFailed}
	<div class="offline" role="status">
		<svg
			class="offline-glyph offline-glyph--static"
			viewBox="0 0 24 24"
			fill="none"
			stroke="currentColor"
			stroke-width="2.4"
			stroke-linecap="round"
			stroke-linejoin="round"
			aria-hidden="true"
		>
			<path d="M12 9v4" />
			<path d="M12 17h.01" />
			<path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" />
		</svg>
		<span>Audio failed to load</span>
	</div>
{/if}

<main class:night={isNight}>
	<section class="centerpiece">
		{#if countdown !== null}
			<div class="clock" class:urgent class:expired>
				<div class="clock-ring"></div>
				<div class="clock-face">
					<span class="clock-eyebrow">Time remaining</span>
					<span class="clock-time">
						{countdown.value}<span class="clock-unit">{countdown.unit}</span>
					</span>
				</div>
			</div>
		{/if}
	</section>

	{#if qr && qrVisible}
		<aside class="pairing">
			<div class="pairing-frame">
				<div class="qr">{@html qr}</div>
			</div>
			<div class="pairing-caption">
				<p class="pairing-title">Scan to control</p>
				{#if remoteUrl}
					<a class="pairing-link" href={remoteUrl}>{remoteUrl}</a>
				{/if}
			</div>
			<button class="ghost-btn" onclick={dismissQr}>Hide code</button>
		</aside>
	{:else if qr}
		<button class="reveal-btn" onclick={revealQr} title="Show the pairing code">
			<span class="reveal-glyph">⊞</span> Pair remote
		</button>
	{/if}
</main>

{#if !engine}
	<div class="gate">
		<div class="gate-inner">
			<span class="gate-wordmark">Minstrel</span>
			<p class="gate-tagline">A score for Blood on the Clocktower</p>
			<button class="gate-btn" onclick={enableSound}>
				<span>Enter the town</span>
			</button>
			<p class="gate-hint">Sound begins on your command</p>
		</div>
	</div>
{/if}

{#if version}
	<span class="version">{version}</span>
{/if}

<style>
	:global(:root) {
		--blood: #b11616;
		--blood-deep: #6e0d0d;
		--ember: #cd9a3f;
		--ember-soft: #e3c177;
		--parchment: #efe6d3;
		--parchment-dim: #b9ac93;
		--moon: #cdd6ea;
		--moon-dim: #8b95ad;
		--ink: #0a0608;
		--display: 'Cinzel', 'Times New Roman', serif;
		--body: 'EB Garamond', Georgia, serif;
	}

	/* Unobtrusive release marker, pinned to the corner. */
	.version {
		position: fixed;
		right: 0.7rem;
		bottom: 0.5rem;
		z-index: 10;
		font-family: var(--body);
		font-size: 0.72rem;
		letter-spacing: 0.04em;
		font-variant-numeric: tabular-nums;
		color: var(--parchment);
		opacity: 0.25;
		pointer-events: none;
		user-select: none;
	}

	/* ---------- Offline banner (SSE connection down) ---------- */
	.offline {
		position: fixed;
		top: clamp(1rem, 3vmin, 2rem);
		left: 50%;
		transform: translateX(-50%);
		z-index: 9;
		display: inline-flex;
		align-items: center;
		gap: 0.7em;
		padding: 0.7em 1.6em;
		border-radius: 999px;
		font-family: var(--display);
		font-weight: 600;
		font-size: clamp(0.95rem, 1.4vw, 1.25rem);
		letter-spacing: 0.18em;
		text-transform: uppercase;
		color: var(--parchment);
		background: rgba(20, 8, 8, 0.82);
		border: 1px solid rgba(177, 22, 22, 0.7);
		box-shadow:
			0 18px 50px rgba(0, 0, 0, 0.7),
			0 0 40px rgba(177, 22, 22, 0.3);
		backdrop-filter: blur(10px);
		animation: rise 0.6s cubic-bezier(0.16, 1, 0.3, 1) both;
	}

	.offline-glyph {
		width: 1.15em;
		height: 1.15em;
		color: var(--ember-soft);
		transform-origin: center;
		animation: turn 1.1s linear infinite;
	}

	.offline-glyph--static {
		animation: none;
	}

	/* ---------- Full-bleed atmospheric backdrop ---------- */
	.stage {
		position: fixed;
		inset: 0;
		z-index: 0;
		overflow: hidden;
		background: var(--ink);
	}

	.backdrop {
		position: absolute;
		inset: 0;
		width: 100%;
		height: 100%;
		object-fit: cover;
		object-position: center;
		/* Identical filter function lists in both phases so the 5s transition
		   interpolates each value smoothly instead of snapping. */
		filter: grayscale(0) brightness(1) saturate(1.08) contrast(1.04);
		transition: filter 5s ease;
		transform: scale(1.04);
	}

	.stage.night .backdrop {
		filter: grayscale(1) brightness(0.62) saturate(1) contrast(1.05);
	}

	/* Cinematic vignette: dark corners + heavier floor to ground the QR.
	   Kept constant across phases — the grey is driven by the backdrop filter. */
	.vignette {
		position: absolute;
		inset: 0;
		background:
			radial-gradient(120% 90% at 50% 38%, transparent 38%, rgba(10, 6, 8, 0.55) 100%),
			linear-gradient(to bottom, rgba(10, 6, 8, 0.55) 0%, transparent 26%),
			linear-gradient(to top, rgba(10, 6, 8, 0.92) 0%, rgba(10, 6, 8, 0.2) 34%, transparent 56%);
	}

	/* Fine film grain for texture */
	.grain {
		position: absolute;
		inset: -50%;
		opacity: 0.05;
		background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='160' height='160'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.8' numOctaves='2' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)'/%3E%3C/svg%3E");
		animation: grain-drift 7s steps(6) infinite;
		pointer-events: none;
	}

	@keyframes grain-drift {
		0% { transform: translate(0, 0); }
		20% { transform: translate(-6%, 4%); }
		40% { transform: translate(4%, -5%); }
		60% { transform: translate(-3%, 3%); }
		80% { transform: translate(5%, 2%); }
		100% { transform: translate(0, 0); }
	}

	/* ---------- Foreground layout ---------- */
	main {
		position: relative;
		z-index: 1;
		min-height: 100vh;
		min-height: 100dvh;
		margin: 0;
		padding: clamp(2rem, 4vmin, 4rem);
		display: grid;
		place-items: center;
		font-family: var(--body);
	}

	/* ---------- Centerpiece ---------- */
	.centerpiece {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		text-align: center;
	}

	/* The countdown rendered as an engraved clock medallion */
	.clock {
		position: relative;
		display: grid;
		place-items: center;
		width: clamp(20rem, 42vmin, 34rem);
		aspect-ratio: 1;
		animation: rise 1.1s cubic-bezier(0.16, 1, 0.3, 1) both;
	}

	.clock-ring {
		position: absolute;
		inset: 0;
		border-radius: 50%;
		/* engraved tick marks via conic gradient, framed by ember rings */
		background:
			repeating-conic-gradient(
				from 0deg,
				rgba(239, 230, 211, 0.5) 0deg 0.5deg,
				transparent 0.5deg 6deg
			);
		-webkit-mask: radial-gradient(circle, transparent 0 calc(50% - 1.1rem), #000 calc(50% - 1.1rem) calc(50% - 0.55rem), transparent calc(50% - 0.55rem));
		mask: radial-gradient(circle, transparent 0 calc(50% - 1.1rem), #000 calc(50% - 1.1rem) calc(50% - 0.55rem), transparent calc(50% - 0.55rem));
		opacity: 0.65;
		animation: turn 120s linear infinite;
	}

	.clock::before {
		content: '';
		position: absolute;
		inset: 0;
		border-radius: 50%;
		border: 2px solid rgba(205, 154, 63, 0.55);
		box-shadow:
			0 0 0 1px rgba(10, 6, 8, 0.6) inset,
			0 0 80px rgba(177, 22, 22, 0.32),
			0 0 40px rgba(0, 0, 0, 0.7);
		background: radial-gradient(circle at 50% 42%, rgba(10, 6, 8, 0.66), rgba(10, 6, 8, 0.88));
		backdrop-filter: blur(2px);
		transition: box-shadow 0.6s ease;
	}

	.clock-face {
		position: relative;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 0.4em;
	}

	.clock-eyebrow {
		font-family: var(--display);
		font-weight: 500;
		font-size: clamp(0.7rem, 1vw, 0.95rem);
		letter-spacing: 0.42em;
		text-transform: uppercase;
		color: var(--ember-soft);
		transition: color 5s ease;
	}

	main.night .clock-eyebrow {
		color: var(--moon-dim);
	}

	.clock-time {
		font-family: var(--display);
		font-weight: 700;
		font-size: clamp(4.5rem, 13vmin, 10rem);
		line-height: 0.9;
		font-variant-numeric: tabular-nums;
		color: var(--parchment);
		text-shadow:
			0 0 28px rgba(177, 22, 22, 0.55),
			0 4px 24px rgba(0, 0, 0, 0.85);
		transition: color 5s ease;
	}

	.clock-unit {
		margin-left: 0.18em;
		font-size: 0.3em;
		font-weight: 600;
		letter-spacing: 0.2em;
		text-transform: uppercase;
		color: var(--ember-soft);
		text-shadow: none;
		transition: color 5s ease;
	}

	main.night .clock-unit {
		color: var(--moon-dim);
	}

	main.night .clock-time {
		text-shadow:
			0 0 28px rgba(99, 124, 178, 0.5),
			0 4px 24px rgba(0, 0, 0, 0.85);
	}

	/* Final ten seconds: tighten the glow and pulse once per tick */
	.clock.urgent .clock-time {
		color: var(--ember-soft);
		animation: tick 1s ease-in-out infinite;
	}

	.clock.urgent::before {
		border-color: rgba(177, 22, 22, 0.85);
		box-shadow:
			0 0 0 1px rgba(10, 6, 8, 0.6) inset,
			0 0 120px rgba(177, 22, 22, 0.55),
			0 0 40px rgba(0, 0, 0, 0.7);
	}

	.clock.expired .clock-time {
		color: var(--blood);
		text-shadow: 0 0 48px rgba(177, 22, 22, 0.9);
	}

	.clock.expired::before {
		animation: flare 1.4s ease-out infinite;
	}

	@keyframes tick {
		0%, 100% { transform: scale(1); }
		12% { transform: scale(1.06); }
	}

	@keyframes flare {
		0%, 100% {
			box-shadow:
				0 0 0 1px rgba(10, 6, 8, 0.6) inset,
				0 0 60px rgba(177, 22, 22, 0.45),
				0 0 40px rgba(0, 0, 0, 0.7);
		}
		50% {
			box-shadow:
				0 0 0 1px rgba(10, 6, 8, 0.6) inset,
				0 0 160px rgba(177, 22, 22, 0.85),
				0 0 40px rgba(0, 0, 0, 0.7);
		}
	}

	@keyframes turn {
		to { transform: rotate(360deg); }
	}

	/* ---------- Pairing / QR ---------- */
	.pairing {
		position: fixed;
		bottom: clamp(2rem, 4vmin, 3.5rem);
		right: clamp(2rem, 4vmin, 3.5rem);
		z-index: 2;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 1rem;
		padding: clamp(1.25rem, 2vmin, 2rem);
		border-radius: 1.25rem;
		background: rgba(12, 8, 6, 0.72);
		border: 1px solid rgba(205, 154, 63, 0.45);
		box-shadow:
			0 24px 70px rgba(0, 0, 0, 0.7),
			0 0 0 1px rgba(0, 0, 0, 0.4);
		backdrop-filter: blur(14px);
		animation: rise 1.1s cubic-bezier(0.16, 1, 0.3, 1) 0.24s both;
	}

	.pairing-frame {
		padding: clamp(0.75rem, 1.4vmin, 1.1rem);
		border-radius: 0.85rem;
		background: var(--parchment);
		box-shadow: inset 0 0 0 1px rgba(110, 13, 13, 0.18);
	}

	.qr {
		width: clamp(20rem, 40vmin, 34rem);
		height: clamp(20rem, 40vmin, 34rem);
	}

	.qr :global(svg) {
		display: block;
		width: 100%;
		height: 100%;
	}

	.qr :global(path) {
		fill: #14080a;
	}

	.pairing-caption {
		text-align: center;
		line-height: 1.3;
	}

	.pairing-title {
		margin: 0;
		font-family: var(--display);
		font-weight: 600;
		font-size: clamp(1rem, 1.4vw, 1.3rem);
		letter-spacing: 0.22em;
		text-transform: uppercase;
		color: var(--ember-soft);
	}

	.pairing-link {
		display: inline-block;
		margin-top: 0.55em;
		max-width: 100%;
		font-family: var(--body);
		font-size: clamp(0.95rem, 1.2vw, 1.2rem);
		letter-spacing: 0.02em;
		word-break: break-all;
		color: var(--parchment);
		text-decoration: none;
		border-bottom: 1px solid rgba(205, 154, 63, 0.5);
		transition: color 0.2s ease, border-color 0.2s ease;
	}

	.pairing-link:hover {
		color: var(--ember-soft);
		border-color: var(--ember);
	}

	/* ---------- Buttons ---------- */
	.ghost-btn,
	.reveal-btn {
		font-family: var(--display);
		font-weight: 600;
		letter-spacing: 0.22em;
		text-transform: uppercase;
		cursor: pointer;
		color: var(--parchment);
		background: transparent;
		border: 1px solid rgba(205, 154, 63, 0.5);
		border-radius: 999px;
		transition:
			background 0.25s ease,
			border-color 0.25s ease,
			color 0.25s ease;
	}

	.ghost-btn {
		font-size: 0.78rem;
		padding: 0.55em 1.4em;
		color: var(--parchment-dim);
	}

	.ghost-btn:hover {
		color: var(--parchment);
		border-color: var(--ember);
		background: rgba(205, 154, 63, 0.12);
	}

	.reveal-btn {
		position: fixed;
		bottom: clamp(2rem, 4vmin, 3.5rem);
		right: clamp(2rem, 4vmin, 3.5rem);
		z-index: 2;
		display: inline-flex;
		align-items: center;
		gap: 0.55em;
		font-size: clamp(0.8rem, 1vw, 0.95rem);
		padding: 0.85em 1.7em;
		background: rgba(12, 8, 6, 0.7);
		backdrop-filter: blur(10px);
		box-shadow: 0 12px 40px rgba(0, 0, 0, 0.6);
		animation: rise 0.6s cubic-bezier(0.16, 1, 0.3, 1) both;
	}

	.reveal-btn:hover {
		border-color: var(--ember);
		background: rgba(205, 154, 63, 0.16);
		color: var(--ember-soft);
	}

	.reveal-glyph {
		font-size: 1.2em;
		line-height: 1;
	}

	/* ---------- Sound gate ---------- */
	.gate {
		position: fixed;
		inset: 0;
		z-index: 10;
		display: grid;
		place-items: center;
		background: radial-gradient(circle at 50% 48%, rgba(8, 4, 6, 0.93), rgba(4, 2, 4, 0.985));
		backdrop-filter: blur(12px) brightness(0.7);
		animation: gate-in 0.8s ease both;
	}

	.gate-inner {
		display: flex;
		flex-direction: column;
		align-items: center;
		text-align: center;
		gap: 1.1rem;
		padding: 2rem;
	}

	.gate-wordmark {
		font-family: var(--display);
		font-weight: 900;
		font-size: clamp(3rem, 9vmin, 7rem);
		letter-spacing: 0.28em;
		padding-left: 0.28em;
		text-transform: uppercase;
		color: var(--parchment);
		text-shadow:
			0 0 60px rgba(177, 22, 22, 0.6),
			0 2px 30px rgba(0, 0, 0, 0.9);
	}

	.gate-tagline {
		margin: 0;
		font-size: clamp(1rem, 1.6vw, 1.4rem);
		font-style: italic;
		letter-spacing: 0.14em;
		color: var(--ember-soft);
		opacity: 0.85;
	}

	.gate-btn {
		margin-top: 1.5rem;
		font-family: var(--display);
		font-weight: 700;
		font-size: clamp(1.05rem, 1.6vw, 1.4rem);
		letter-spacing: 0.28em;
		text-transform: uppercase;
		cursor: pointer;
		color: var(--parchment);
		padding: 1em 2.6em;
		border: 1px solid rgba(205, 154, 63, 0.6);
		border-radius: 999px;
		background: rgba(177, 22, 22, 0.14);
		box-shadow: 0 0 0 0 rgba(177, 22, 22, 0.5);
		transition:
			background 0.3s ease,
			border-color 0.3s ease,
			box-shadow 0.3s ease,
			transform 0.2s ease;
		animation: pulse-glow 2.6s ease-in-out infinite;
	}

	.gate-btn:hover {
		background: rgba(177, 22, 22, 0.28);
		border-color: var(--ember);
		transform: translateY(-2px);
	}

	.gate-hint {
		margin: 0.4rem 0 0;
		font-size: 0.85rem;
		letter-spacing: 0.18em;
		text-transform: uppercase;
		color: var(--parchment-dim);
		opacity: 0.7;
	}

	@keyframes pulse-glow {
		0%, 100% { box-shadow: 0 0 0 0 rgba(177, 22, 22, 0); }
		50% { box-shadow: 0 0 46px 2px rgba(177, 22, 22, 0.4); }
	}

	@keyframes gate-in {
		from { opacity: 0; }
		to { opacity: 1; }
	}

	@keyframes rise {
		from {
			opacity: 0;
			transform: translateY(28px);
		}
		to {
			opacity: 1;
			transform: translateY(0);
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.grain,
		.clock,
		.clock-ring,
		.pairing,
		.reveal-btn,
		.gate,
		.gate-btn,
		.offline,
		.offline-glyph,
		.clock.urgent .clock-time,
		.clock.expired::before {
			animation: none !important;
		}
	}
</style>
