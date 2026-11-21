# pixelfx → Unity

Sprite sheets rendered from the pinned pixelfx library (`third_party/pixelfx`, see `tools/pixelfx/PIXELFX_VERSION`).

## Get an effect in

- **Editor:** `Tools > pixelfx > Render & Import Effect…` → search, pick an id, optional palette/seed JSON, *Render & Import*.
  Needs Node 18+ (set its path in the window if Unity can't find it).
- **Command line / Claude:** `node tools/pixelfx/export.mjs explosions/small-explosion` from the repo root.
- **A sheet you already have:** `Tools > pixelfx > Import Sheet From File…` (any pixelfx PNG + JSON pair).

Anything in `Assets/PixelFx/Sheets/` is imported automatically: point filter, uncompressed, no mipmaps, max size
16384, then `<name>.asset` (`PixelFxSheet`) and `<name>.prefab` (SpriteRenderer + `PixelFxPlayer`) are created
next to the PNG. `Tools > pixelfx > Reimport All Sheets` rebuilds them.

## Use it

```csharp
public PixelFxSheet explosion;
PixelFxPlayer.Spawn(explosion, enemy.position, flipLeft: facing < 0);   // destroys itself when done
```

- The pivot is the effect's origin from `meta.pixelfx.pivot` (feet / impact point), so put the object there.
- `pixelsPerUnit` on the sheet asset (default 16, set the default for new imports in the window) must match your sprites.
- Loops play until `Stop()`. One-shots fire `onFinished`.
- Effects with an act report each contact via `onHit` (`PixelFxHit.freeze` seconds): freeze your characters there.
  The player applies its own hit-stop when `applyHitStop` is on. Start your attacker's wind-up `sheet.data.windup`
  seconds before spawning.
- Glow is baked as colour + alpha and reads best on dark backgrounds; for bright scenes render a recoloured variant.

## Check it

- `Tools > pixelfx > Create Demo Scene` (uses the selected or first sheet) → Play. Click to spawn more.
- `Window > General > Test Runner > EditMode > Run All` (`Assets/PixelFx/Tests`, needs the Test Framework package).
