import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

const port = process.env.PORT ? Number(process.env.PORT) : 5175;

export default defineConfig({
	server: {
		port,
		strictPort: !!process.env.PORT,
		// Bind every interface when a port is pinned so a phone on the LAN can reach
		// the dev server for real-device testing.
		host: process.env.PORT ? true : undefined,
		fs: { allow: ['..'] }
	},
	plugins: [sveltekit()]
});
