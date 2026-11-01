import adapter from '@sveltejs/adapter-static';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

const backend = process.env['services__backend__http__0'] ?? 'http://localhost:5000';
const port = process.env.PORT ? Number(process.env.PORT) : 5174;

export default defineConfig({
	server: {
		port,
		strictPort: !!process.env.PORT,
		// Under Aspire (PORT set) bind every interface so a phone can reach the
		// Remote by LAN IP; Aspire's proxy only listens on localhost.
		host: process.env.PORT ? true : undefined,
		fs: { allow: ['..'] },
		proxy: {
			'/playback': { target: backend, changeOrigin: true },
			'/timer': { target: backend, changeOrigin: true },
			'/library': backend,
			'/playlist': backend
		}
	},
	plugins: [
		sveltekit({
			preprocess: vitePreprocess(),
			adapter: adapter({
				pages: '../Backend/wwwroot/remote',
				assets: '../Backend/wwwroot/remote',
				fallback: 'index.html'
			}),
			paths: {
				// BASE_PATH is the bare segment (e.g. "remote"); the leading slash is added here so the
				// build recipe never passes a POSIX-looking path that Git Bash mangles on Windows.
				base: process.env.BASE_PATH ? `/${process.env.BASE_PATH}` : ''
			}
		})
	]
});
