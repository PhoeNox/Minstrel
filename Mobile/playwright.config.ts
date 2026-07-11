import { defineConfig } from '@playwright/test';

const port = 4175;

// Image-snapshot suite over the built static bundle (ADR-0009): sirv serves `build/`
// directly, so run it via `just e2e`, which rebuilds the bundle first. Local-only and
// excluded from CI — baselines are environment-specific (font rendering).
export default defineConfig({
	testDir: 'e2e',
	// Stable, README-embeddable baseline paths: e2e/snapshots/<name>.png, no
	// platform/project suffixes.
	snapshotPathTemplate: '{testDir}/snapshots/{arg}{ext}',
	reporter: 'list',
	use: {
		baseURL: `http://localhost:${port}`,
		browserName: 'chromium',
		viewport: { width: 390, height: 844 },
		deviceScaleFactor: 2,
		isMobile: true,
		hasTouch: true,
		colorScheme: 'dark',
		// The service worker would cache-serve a previous bundle behind the test's back.
		serviceWorkers: 'block'
	},
	webServer: {
		command: `npx sirv build --port ${port} --single --quiet`,
		url: `http://localhost:${port}`
	}
});
