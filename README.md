# Hypernex.Godot

The Hypernex Godot Client

## Status

This client is very Work-in-Progress.

### Features

- Login, Home, Worlds and Instance UI
- Networking
- PC/Flatscreen Movement
- Proximity Voice Chat (broken as of current)
- Proximity Text To Speech
- GIF/Video playback in UI

### In Progress

- Godot Editor CCK (second iteration since May of 2024)
- - Missing avatar export buttons (workaround: Project -> Tools -> Export World, rename to end in `.hna` instead of `.hnw`)
- - Missing server scripts and server metadata
- - Sandboxing is primitive, and can be possibly exploited via `PackedScene` class
- - All builtin nodes are accepted (which isn't good)
- - Signals aren't supported yet
- Avatars (since May of 2024)
- - Missing animations and scripting
- VR (since August of 2024)
- - IK system is partially incorrect
- - Avatar doesnt move and rotate with player head/root
- - Menus require use of the desktop
- Scripting
- - Missing a ton of APIs

### Planned

- Stream/RTMP playback
- In-Game CCK(?)

And (probably) more.

## License

All non-external code (found in `/Hypernex.Godot/scripts/`) is GNU GPL-3.0

All external code (not found in `/Hypernex.Godot/scripts/`) is licensed under their own respective licenses
