# Third-Party Notices

This project includes third-party software. Each component is listed with its license.

---

## pixelfx 0.3.1

- Source: https://github.com/patch-gg/pixelfx (commit `5294518ce7c72e48b6100a744596e0acf57f2826`)
- Location in this repo: `third_party/pixelfx/` (code and tools), `.claude/skills/pixelfx/SKILL.md` (adapted skill),
  and sprite sheets rendered from it under `Assets/PixelFx/Sheets/`
- License: MIT

```
MIT License

Copyright (c) 2026 Patch
Copyright (c) 2026 Hugo Duprez

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

pixelfx in turn adapts the following MIT-licensed work (from its `NOTICE.md`):

- **Sprite Fusion pixel art VFX** by Hugo Duprez — `src/core/sim.js` and the effects that say so at the top of
  their file. https://www.spritefusion.com/pixel-art-vfx — Copyright (c) 2026 Hugo Duprez, MIT License.
  The effect-making method follows his article "How to make (good) pixel art effects with Claude":
  https://www.spritefusion.com/blog/how-to-make-good-pixel-art-effects-with-claude
- **Hollow Crown** by rehan-remade — `src/core/act.js` (poses on 12 fps steps, hit-stops, the springy target).
  https://github.com/rehan-remade/hollow-crown — Copyright (c) 2026 rehan-remade, MIT License.

### Not included

The **Patch Pixel** typeface (`gallery/fonts/PatchPixel-Regular.woff2` upstream) is © Patch, all rights reserved,
and not covered by the MIT license. It is deliberately **not** included in this project and must not be added:
`tools/pixelfx/vendor.sh` excludes the upstream `gallery/` folder and the tools that use it, and fails if a
reference to the font slips in.
