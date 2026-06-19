/// <reference types="@sveltejs/kit" />
/// <reference no-default-lib="true"/>
/// <reference lib="esnext" />
/// <reference lib="webworker" />

import { base, build, files, version } from '$service-worker';

const sw = self as unknown as ServiceWorkerGlobalScope;

const CACHE = `minstrel-mobile-${version}`;

// The app shell: hashed build artefacts, static assets, and the SPA fallback page so a
// cold launch with no network still boots the app (ADR-0008 offline install).
const SHELL = [...build, ...files, `${base}/`];

sw.addEventListener('install', (event) => {
	event.waitUntil(
		caches.open(CACHE).then((cache) => cache.addAll(SHELL)).then(() => sw.skipWaiting())
	);
});

sw.addEventListener('activate', (event) => {
	event.waitUntil(
		caches
			.keys()
			.then((keys) => Promise.all(keys.filter((key) => key !== CACHE).map((key) => caches.delete(key))))
			.then(() => sw.clients.claim())
	);
});

sw.addEventListener('fetch', (event) => {
	const { request } = event;
	if (request.method !== 'GET') return;
	if (new URL(request.url).origin !== location.origin) return;

	event.respondWith(serve(request));
});

async function serve(request: Request): Promise<Response> {
	const cache = await caches.open(CACHE);

	const cached = await cache.match(request);
	if (cached) return cached;

	try {
		return await fetch(request);
	} catch (error) {
		// Offline: fall back to the cached SPA shell for navigations so the app still loads.
		if (request.mode === 'navigate') {
			const shell = await cache.match(`${base}/`);
			if (shell) return shell;
		}
		throw error;
	}
}
