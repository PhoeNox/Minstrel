# Minstrel

**Minstrel** is a music player for the Storyteller running in-person games of the social deduction game *Blood on the Clocktower*.

It plays atmospheric music for your sessions, switches automatically between separate day and night playlists, and provides an integrated countdown timer to mark the end of each day.

Minstrel runs as a single app on one device — your laptop or a TV-connected machine — and splits into two screens you open in a browser:

* the **Player** is the screen that actually makes sound (put it on the device wired to your speakers), and
* the **Remote** is a phone control surface that drives the Player over your local Wi-Fi.

<p align="center">
  <img src="E2E.Tests/CrossSurfaceTests.Timer_StartedFromRemote_player.verified.png" alt="The Player screen showing the clocktower scene with a five-minute timer counting down" width="62%">
  <img src="E2E.Tests/CrossSurfaceTests.Timer_StartedFromRemote_remote.verified.png" alt="The Remote screen on a phone: now playing, the day playlist, and the Switch and timer controls" width="33%">
</p>

## Features

* **Two playlists, one game** — keep separate playlists for the Day and Night phases and switch between them with a single tap. Songs fade out and fade in smoothly so the transition never feels abrupt.
* **Day timer** — start a countdown for any number of minutes. The Player shows it over the scene, and when it expires a gong sounds to tell the table that the day is over.
* **Phone remote** — control playback from your phone while the music plays from another device. A QR code on the Player screen takes you straight to the Remote.
* **Song library** — browse all the music you have available, search by artist, title, or album, and add songs to either playlist with one tap.
* **Independent volume per playlist** — set a different volume for the day and night playlists so quiet ambient tracks and louder pieces sit at the right level.

## Setup

### 1. Get the app

Download the version of Minstrel for your operating system and extract it to a folder.

### 2. Add your music

Point the app to your music by editing `appsettings.json`: set `Music` → `Directory` to your music folder.

Minstrel supports common formats such as MP3, FLAC, OGG, and M4A. Subfolders are fine.

### 3. Start the app

Double-click the Minstrel file. A console window opens, and your default web browser opens automatically with the **Player** screen.

That is the whole setup.

## Using Minstrel

### Open the Player

The Player opens to an entry screen with an **Enter the town** button. Click it once to begin — browsers only allow sound after a click, so this first tap is what lets Minstrel play audio. After that the Player shows the scene and plays whatever the Remote tells it to.

Run the Player on the device connected to your speakers, and leave it open for the whole session.

### Connect your phone

A QR code sits on the Player screen. Scan it with your phone's camera to open the **Remote**. If you would rather type it, the pairing address is printed under the code.

Use **Hide code** on the Player to tuck the QR away once everyone is connected, and **Pair remote** to bring it back for a latecomer.

**Important:** Your phone must be on the same Wi-Fi network as the device running Minstrel.

### Building your playlists

The Remote has three tabs along the bottom: **Day**, **Night**, and **Library**.

* Open the **Library** tab to see all your music. Tap the sun icon to add a song to the day playlist or the moon icon to add it to the night playlist. The search box filters by artist, title, or album.
* Open the **Day** or **Night** tab to see that playlist. Drag songs by the handle on the right to reorder them, tap the **×** to remove one, and use **Shuffle** at the top to randomise the order.
* Each playlist tab has its own volume slider at the top, so you can balance day and night audio independently.

Your playlists and volumes are saved automatically.

### Running a game

1. Tap the **play** button (the square on the left of the bottom bar) to start the current playlist.
2. When the day ends, tap the big **Switch to Night** button. The current song fades out and a song from the night playlist fades in. Tap **Switch to Day** to come back for the next round.
3. To start the day timer, tap the **timer** button on the right of the bottom bar, set the number of minutes with **−** / **+**, and tap **Start timer**. The Player counts it down over the scene, and a gong plays when it reaches zero. Tap the timer button again to stop it early.

## Acknowledgements and Copyrights

* [Blood on the Clocktower](https://bloodontheclocktower.com/) is a trademark of Steven Medway and [The Pandemonium Institute](https://www.thepandemoniuminstitute.com/)
* The demo songs can be found on [Jamendo](https://jamendo.com/start):
  * [Clocktower](https://www.jamendo.com/track/1206097/clocktower) by [Cloud Seed](https://www.jamendo.com/artist/463887/cloud-seed)
  * [Bloodlust](https://www.jamendo.com/track/786901/bloodlust) by [Deflate](https://www.jamendo.com/artist/369102/deflate)
  * [Graveyard](https://www.jamendo.com/track/1623977/graveyard) by [The Liquid Kitchen](https://www.jamendo.com/artist/515695/the-liquid-kitchen)
  * [I'm Growing Fangs](https://www.jamendo.com/track/1198383/i-m-growing-fangs) by [Great White Buffalo](https://www.jamendo.com/artist/433617/great-white-buffalo)
* The demo wallpaper has been generated using AI.
</content>
</invoke>
