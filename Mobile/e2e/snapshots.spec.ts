import { expect, test } from '@playwright/test';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// The Mobile image snapshot (ADR-0009), doubling as the README's Mobile screenshot.
// The README embeds the baseline by path — renaming it silently breaks the README image:
//   Mobile/e2e/snapshots/day-playlist-playing-with-timer.png
// A first run writes the missing baseline and reports a failure; rerun to verify.
// To accept a changed baseline, run with --update-snapshots.

const MUSIC_DIRECTORY = fileURLToPath(new URL('../../Backend/Music', import.meta.url));
const DEMO_SONGS = [
	'Bloodlust - Deflate.mp3',
	'Clocktower - Cloud Seed.mp3',
	'Graveyard - The Liquid Kitchen.mp3',
	"I'm Growing Fangs - Great White Buffalo.mp3"
].map((name) => path.join(MUSIC_DIRECTORY, name));

// The clock is installed paused at this instant before the app boots, so every
// Date.now() anchor and every app timer moves only through the explicit runFor
// call below — the progress fills and the countdown render one fixed value.
const SESSION_START = new Date('2026-01-01T20:00:00');

test('day playlist playing with the timer running', async ({ page }) => {
	await page.clock.install({ time: SESSION_START });
	await page.clock.pauseAt(SESSION_START);
	await page.goto('/');

	// Import the demo songs through the real file-input flow and fill the Day playlist.
	await page.locator('.tab', { hasText: 'Library' }).click();
	await page.locator('input[type="file"][accept="audio/*"]').setInputFiles(DEMO_SONGS);
	await expect(page.locator('.song')).toHaveCount(DEMO_SONGS.length);
	for (let index = 0; index < DEMO_SONGS.length; index++) {
		await page.locator('.song .add.day').nth(index).click();
	}

	// Start the first song playing.
	await page.locator('.tab', { hasText: 'Day' }).click();
	await expect(page.locator('.deck li')).toHaveCount(DEMO_SONGS.length);
	await page.locator('.deck li .select').first().click();
	await expect(page.locator('.play.playing')).toBeVisible();

	// Start the default 5-minute timer; the dialog closes onto the transport chip.
	await page.locator('.timerbtn').click();
	await page.locator('.timer-start').click();

	// 30s in: the song's progress fills render one fixed width, the chip shows a
	// mid-run 4:30, and the import notice (4s timeout) has expired.
	await page.clock.runFor(30_000);
	await expect(page.locator('.timerbtn-clock')).toHaveText('4:30');
	await expect(page.locator('.toast')).toHaveCount(0);

	await page.evaluate(async () => {
		await document.fonts.ready;
	});
	await expect(page).toHaveScreenshot('day-playlist-playing-with-timer.png');
});
