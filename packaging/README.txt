Minstrel - music player for Blood on the Clocktower
===================================================

Minstrel plays atmospheric music for in-person games: separate Day and Night
playlists that fade into each other on a single click, plus a countdown timer
with a gong. A phone on the same wifi can control playback by scanning a QR code.

This folder is everything you need. Nothing else to install - the program
already contains its own runtime.


1. Point Minstrel at your music
-------------------------------

Open "appsettings.json" in a text editor. Find this line:

    "Directory": "Music"

By default Minstrel plays the songs in the "Music" folder next to the program.
Either drop your songs (and your "day.m3u" / "night.m3u" playlists) into that
folder, or change "Music" to the full path of the folder where your music lives,
for example:

    Windows:  "Directory": "C:\\Users\\you\\Music\\Clocktower"
    macOS:    "Directory": "/Users/you/Music/Clocktower"
    Linux:    "Directory": "/home/you/Music/Clocktower"

(On Windows, write each backslash twice, as shown.)

Supported formats: MP3, FLAC, OGG, M4A. Subfolders are fine.

The two sample playlists in the "Music" folder, "day.m3u" and "night.m3u", show
the format: one song file name per line. Edit them to list the songs you want in
each phase, or build them from the app's Remote screen later.


2. Start Minstrel
-----------------

    Windows:        double-click  Minstrel.exe
    macOS / Linux:  double-click  Minstrel

A console window opens and your web browser opens automatically on the Player
screen. That is the whole setup.

To connect a phone as a remote, scan the QR code on the Player screen. The phone
must be on the same wifi network as the computer running Minstrel.


First-launch warnings (these are expected)
==========================================

Minstrel is a small free program without a paid signing certificate, so each
operating system shows a warning the first time. None of them mean anything is
wrong.

Windows - SmartScreen
    A blue "Windows protected your PC" box may appear. Click "More info", then
    "Run anyway".

Windows - Firewall (IMPORTANT for the phone remote)
    A "Windows Defender Firewall" box asks whether to allow Minstrel to
    communicate on the network. You MUST click "Allow access" - otherwise your
    phone cannot reach the remote. If you click "Cancel", the player still works
    but the QR code will not connect.

macOS - Gatekeeper
    macOS may say Minstrel "cannot be opened because it is from an unidentified
    developer" or "could not be verified". Two ways past it:

      - Right-click (or Control-click) the Minstrel file, choose "Open", then
        "Open" again in the dialog. You only do this once.

      - Or, in Terminal, run this once in the extracted folder:
            xattr -dr com.apple.quarantine .


Tips
====

- The phone remote only works when the phone and computer share the same wifi.
- If the QR code shows the wrong address (for example because of a VPN or
  VirtualBox), set "Host" in appsettings.json to the computer's wifi IP address.
- The default port is 5757. Change "Port" in appsettings.json if something else
  is using it.
