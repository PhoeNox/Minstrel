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
			'/sse': { target: backend, changeOrigin: true },
			'/library': backend,
			'/audio': backend,
			'/commands': backend
		}
	},
	plugins: [sveltekit()]
});
