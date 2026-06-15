# Minstrel

The **Minstrel** app assists the Storyteller for running in-person games of the social deduction game *Blood on the Clocktower*.

It plays atmospheric music for your sessions, switches automatically between separate day and night playlists, and provides an integrated countdown timer to mark the end of each day.

## Features

* **Two playlists, one game** — keep separate playlists for the Day and Night phases and switch between them with a single click. Songs fade out and fade in smoothly so the transition never feels abrupt.
* **Day timer** — start a countdown for any number of minutes. When it expires, a gong sounds to tell the table that the day is over.
* **Phone remote** — control playback from your phone while the music plays from another device (like your TV). A QR code on the player screen takes you directly to the remote.
* **Song library** — browse all the music you have available, search by artist, title, or album, and add songs to either playlist with one tap.
* **Independent volume per playlist** — set a different volume for the day and night playlists so quiet ambient tracks and louder pieces sit at the right level.

## Setup

### 1. Get the app

Download the version of Minstrel for your operating system and extract it to a folder.

### 2. Add your music

Point the app to your music by modifying the `appsettings.json`:
There is an entry `Music => Directory`.

Minstrel supports common formats such as MP3, FLAC, OGG, and M4A. Subfolders are fine.

### 3. Start the app

Double-click the Minstrel file.
A console window opens and your default web browser opens automatically with the player screen.

That is the whole setup.

## Using Minstrel

### Connect your phone

Scan the QR code on the player screen with your phone's camera to connect it to the remote.

**Important:** Your phone must be on the same Wi-Fi network as the device running Minstrel.

### Building your playlists

On your phone, open the side menu and choose **Manage playlists**.

* The screen shows your **Day Playlist** at the top, your **Night Playlist** below it, and the **Song Library** at the bottom.
* In the song library, tap the sun icon to add a song to the day playlist or the moon icon to add it to the night playlist.
* Drag songs by their handle on the left to reorder them within a playlist.
* Use the shuffle button next to each playlist heading to randomise its order.
* The search box filters the library by artist, title, or album.

Your playlists are saved automatically.

### Running a game

1. Tap **Play** to start the day playlist.
2. When the day ends, tap **Switch**. The current song fades out and a song from the night playlist fades in.
3. Tap **Switch** again to return to the day playlist for the next round.
4. To start the day timer, open the side menu, choose **Start Timer**, enter the number of minutes, and confirm. When the timer reaches zero, a gong plays.
5. Use the volume sliders above and below the playlists on the remote to balance day and night audio independently.

### Showing the QR code

The QR code on the player screen lets anyone at the table connect their phone to the remote.
Open the side menu and choose **Show QR Code** or **Hide QR Code** to toggle it.

For the QR code to work, your phone must be on the same Wi-Fi network as the device running Minstrel.

## Acknowledgements and Copyrights

* [Blood on the Clocktower](https://bloodontheclocktower.com/) is a trademark of Steven Medway and [The Pandemonium Institute](https://www.thepandemoniuminstitute.com/)
* The demo songs can be found on [Jamendo](https://jamendo.com/start):
  * [Clocktower](https://www.jamendo.com/track/1206097/clocktower) by [Cloud Seed](https://www.jamendo.com/artist/463887/cloud-seed)
  * [Bloodlust](https://www.jamendo.com/track/786901/bloodlust) by [Deflate](https://www.jamendo.com/artist/369102/deflate)
  * [Graveyard](https://www.jamendo.com/track/1623977/graveyard) by [The Liquid Kitchen](https://www.jamendo.com/artist/515695/the-liquid-kitchen)
  * [I'm Growing Fangs](https://www.jamendo.com/track/1198383/i-m-growing-fangs) by [Great White Buffalo](https://www.jamendo.com/artist/433617/great-white-buffalo)
* The demo wallpaper has been generated using AI.
