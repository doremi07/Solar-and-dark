# Solar and Dark

Unity game. Pixel art VFX come from **pixelfx** (pinned, vendored in `third_party/pixelfx`).

- For any pixel effect / VFX / sprite-sheet work, use the `pixelfx` skill in `.claude/skills/pixelfx/` — it describes
  the pinned checkout and the Unity pipeline.
- Export to Unity: `node tools/pixelfx/export.mjs <category/name> [--name N] [--opts JSON] [--act]` →
  `Assets/PixelFx/Sheets/<category>/` (the editor auto-imports to a `PixelFxSheet` + prefab).
- Upgrade pixelfx only by editing `tools/pixelfx/PIXELFX_VERSION` and running `tools/pixelfx/vendor.sh`.
- Never add the Patch Pixel font (not MIT). Third-party credits: `THIRD_PARTY_NOTICES.md`.
- Boss skills (Serena, the Hidden Child): data in `Assets/Game/Combat/Editor/BossLibrary.cs`, runtime in
  `Assets/Game/Combat/Runtime`, sheets from `tools/pixelfx/boss-fx.json` via `tools/pixelfx/export-set.mjs`.
  Effects are `sd/*` (library effect + flourishes tuned in `sd/_skills.js`); phase characters are `sd/serena-*`,
  `sd/child-*` (`tools/pixelfx/boss-characters.json`), played by `BossBody`.
  Overview: `docs/bosses/README.md`. Check without Unity: `tools/pixelfx/verify/run.sh` (compile + logic tests);
  see it running: `tools/arena-sim/run.sh` (real scripts on a headless Unity stand-in → MP4 + storyboard).
- Hand-editing effects: `python3 tools/pixelfx/to-aseprite.py <sheet.json|dir> [--manifest ...]` makes .aseprite
  files (origin slice = pivot + pixelfx meta); an Aseprite sheet export imports back as is. See `docs/pixelfx-aseprite.md`.
