import { self } from '$app/service-worker';
import { version } from '$app/env';
import { assets, immutable } from '$app/manifest';
import { resolve } from '$app/paths';

const CACHE = `minstrel-mobile-${version}`;

const SHELL_PAGE = resolve('/');

// The app shell: hashed build artefacts, static assets, and the SPA fallback page so a
// cold launch with no network still boots the app (ADR-0008 offline install).
const SHELL = [
	...immutable.map((file) => resolve(file.path)),
	...assets.map((file) => resolve(file.path)),
	SHELL_PAGE
];

self.addEventListener('install', (event) => {
	event.waitUntil(
		caches.open(CACHE).then((cache) => cache.addAll(SHELL)).then(() => self.skipWaiting())
	);
});

self.addEventListener('activate', (event) => {
	event.waitUntil(
		caches
			.keys()
			.then((keys) => Promise.all(keys.filter((key) => key !== CACHE).map((key) => caches.delete(key))))
			.then(() => self.clients.claim())
	);
});

self.addEventListener('fetch', (event) => {
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
			const shell = await cache.match(SHELL_PAGE);
			if (shell) return shell;
		}
		throw error;
	}
}
