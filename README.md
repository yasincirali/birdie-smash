# Birdie Smash

Badminton mobile game, a standalone project. The game is a single-file canvas game in
`assets/game/index.html`; the Flutter shell adds a WebView, a haptic bridge and
the TestFlight pipeline.

```bash
flutter pub get && flutter analyze && flutter test && node tool/oyun_sozdizimi.js
```

## Character & animation tooling

- `.claude/skills/3d-animator/` — rigging, mocap, retarget, locomotion and QA know-how (MIT, studioigor).
- `.claude/skills/birdie-karakter/` — this project's Blender → sprite-sheet → canvas pipeline and its rules.
- `.mcp.json` — `blender` MCP (`uvx mcp-for-blender`) for driving Blender live on a desktop; not available in cloud sessions.
- `tool/bulut_kurulum.sh` — cloud session setup: bpy, scipy, Mesh2Motion library.
