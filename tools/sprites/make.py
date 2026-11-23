"""Boss body sprites from the concept art: 128 x 128 pixel art per phase, with idle / cast / hurt animations, written as
pixelfx-format sprite sheets (PNG + JSON) that Unity's PixelFxSheet importer reads.

    python3 tools/sprites/make.py [--out Assets/PixelFx/Sheets/characters] [--preview records/sprites]

Input: art/bosses/cutouts/<boss>-p<n>.png (cut from the concept sheet by tools/sprites/cut.py).

Pixel art: every phase of a boss is scaled by the same factor (the largest fits 124 x 124, feet 2 px above the bottom),
area-averaged after a light sharpen, and snapped to one 32-colour palette taken from that boss's own figures, so the
colours and the silhouette are the reference's. Nothing is redrawn.

Animation moves those pixels, it never paints new ones (nearest sampling; a flash maps each colour to a brighter colour
of the same palette):
  idle  16 frames at 10 fps, loop. The figure floats 1 px; hair and hem sway more the further they are from the body's
        axis and the lower they hang; loose pieces (crystals, shadow scraps) drift on their own phases.
  cast  12 frames at 12 fps. A lean back (wind-up), the release on frame 4 (0.33 s: BossBody.castRelease) leaning in
        with a brightening, then it settles back into the idle pose.
  hurt   6 frames at 12 fps. A flash and a recoil, then back.
"""
import argparse, json, math, os
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
N = 128
BOSSES = {"serena": ["serena-p1", "serena-p2", "serena-p3"], "child": ["child-p1", "child-p2", "child-p3"]}
PIVOT = (N // 2, N - 2)   # the feet


# ---------------------------------------------------------------- pixel art

def load(name):
    a = np.asarray(Image.open(os.path.join(ROOT, "art/bosses/cutouts", name + ".png"))).astype(np.float32) / 255
    ys, xs = np.nonzero(a[..., 3] > 0.5)
    return a[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1]


def pixelize(crops, colours=32):
    sc = min(124 / max(c.shape[0] for c in crops.values()), 124 / max(c.shape[1] for c in crops.values()))
    small = {}
    for k, c in crops.items():
        rgb = c[..., :3]
        blur = cv2.GaussianBlur(rgb, (0, 0), 1.0)
        rgb = np.clip(rgb + 0.6 * (rgb - blur), 0, 1)           # the source is soft: a light unsharp mask first
        pm = np.dstack([rgb * c[..., 3:4], c[..., 3]])
        h, w = c.shape[:2]
        r = cv2.resize(pm, (max(1, round(w * sc)), max(1, round(h * sc))), interpolation=cv2.INTER_AREA)
        al = r[..., 3]
        small[k] = (np.where(al[..., None] > 1e-3, r[..., :3] / np.maximum(al[..., None], 1e-3), 0), al > 0.5)
    px = np.concatenate([rgb[m] for rgb, m in small.values()]).astype(np.float32) * 255
    crit = (cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 60, 0.2)
    _, _, pal = cv2.kmeans(px, colours, None, crit, 5, cv2.KMEANS_PP_CENTERS)
    pal = pal[np.argsort(pal @ np.array([0.299, 0.587, 0.114], np.float32))]   # dark to light
    out = {}
    for k, (rgb, m) in small.items():
        idx = np.argmin(((rgb.reshape(-1, 3)[:, None, :] * 255 - pal[None]) ** 2).sum(-1), 1).reshape(m.shape)
        nb = cv2.filter2D(m.astype(np.uint8), -1, np.array([[1, 1, 1], [1, 0, 1], [1, 1, 1]], np.float32))
        m = m & (nb > 0)                                          # no lone pixels
        n, lab, st, _ = cv2.connectedComponentsWithStats(m.astype(np.uint8), 8)
        for i in range(1, n):
            if st[i, cv2.CC_STAT_AREA] < 12: m &= lab != i       # nor specks
        h, w = m.shape
        canvas = np.full((N, N), -1, np.int16)                    # palette index per pixel, -1 = clear
        ox, oy = PIVOT[0] - w // 2, PIVOT[1] - h
        canvas[oy: oy + h, ox: ox + w] = np.where(m, idx, -1)
        out[k] = canvas
    return out, pal.clip(0, 255).astype(np.uint8)


# ---------------------------------------------------------------- animation (moves pixels only)

def parts(ix):
    """The main figure (largest piece, nearby bits joined) and the loose pieces that float on their own."""
    solid = (ix >= 0).astype(np.uint8)
    joined = cv2.dilate(solid, np.ones((3, 3), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(joined, 8)
    main = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
    lab = np.where(solid > 0, lab, 0)
    loose = [i for i in range(1, n) if i != main and st[i, cv2.CC_STAT_AREA] >= 6]
    return lab, main, loose


def brighter(pal, k):
    """Palette index -> index of the palette colour nearest to that colour brightened by k (stays in the palette)."""
    p = pal.astype(np.float32)
    luma = p @ np.array([0.299, 0.587, 0.114], np.float32)
    # the outlines and the darkest cloth stay as they are; light catches the rest
    kk = k * np.clip((luma - 60) / 80, 0, 1)[:, None]
    tgt = np.clip(p + (255 - p) * kk, 0, 255)
    return np.argmin(((tgt[:, None, :] - p[None]) ** 2).sum(-1), 1)


def render(ix, lab, main, loose, pal, *, sway=0.0, phase=0.0, bob=0, lean=0.0, shift=0, drift=0.0, flash=0.0):
    """One frame: destination pixel (x, y) samples the source at (x - dx, y - dy)."""
    h, w = ix.shape
    ys, xs = np.nonzero(ix >= 0)
    top, bottom = ys.min(), ys.max()
    axis = np.median(xs[ys > top + (bottom - top) * 0.3])
    Y, X = np.mgrid[0:h, 0:w].astype(np.float32)
    # sway: grows with distance from the body's axis and with how low it hangs; hair at the top barely moves
    far = np.clip((np.abs(X - axis) - 8) / 30, 0, 1)
    low = np.clip((Y - (top + (bottom - top) * 0.35)) / ((bottom - top) * 0.65), 0, 1)
    wgt = np.clip(0.55 * far + 0.75 * low * (0.4 + 0.6 * far), 0, 1)
    dx = sway * wgt * np.sin(phase - Y * 0.07)
    # lean: a shear about the feet (positive leans forward, to the right)
    dx += lean * (bottom - Y) / max(1, bottom - top)
    dx += shift
    out = np.full((h, w), -1, np.int16)
    srcx = np.rint(X - dx).astype(int)
    srcy = np.rint(Y - bob).astype(int)
    ok = (srcx >= 0) & (srcx < w) & (srcy >= 0) & (srcy < h)
    sample = np.full((h, w), -1, np.int16)
    sample[ok] = ix[srcy[ok], srcx[ok]]
    slab = np.zeros((h, w), np.int32)
    slab[ok] = lab[srcy[ok], srcx[ok]]
    out = np.where((slab == main) | (slab == 0), sample, -1).astype(np.int16)
    # loose pieces drift up and down on their own, each on its own phase, and follow the lean a little
    for j, i in enumerate(loose):
        m = lab == i
        cy, cx = np.argwhere(m).mean(0)
        oy = int(round(bob + drift * math.sin(phase * 1.0 + j * 1.7)))
        ox = int(round(shift + lean * (bottom - cy) / max(1, bottom - top)))
        yy, xx = np.nonzero(m)
        ny, nx = yy + oy, xx + ox
        k = (ny >= 0) & (ny < h) & (nx >= 0) & (nx < w)
        out[ny[k], nx[k]] = ix[yy[k], xx[k]]
    if flash > 0:
        lut = brighter(pal, flash)
        out = np.where(out >= 0, lut[np.maximum(out, 0)], -1).astype(np.int16)
    return out


def idle(ix, pal, frames=16):
    lab, main, loose = parts(ix)
    for f in range(frames):
        ph = 2 * math.pi * f / frames
        yield render(ix, lab, main, loose, pal, sway=1.6, phase=ph, bob=int(round(-0.6 - 0.6 * math.sin(ph))), drift=1.6)


def cast(ix, pal):
    lab, main, loose = parts(ix)
    # frame: lean (px at the head), float, brightening
    keys = [(-1, 0, 0), (-3, -1, 0), (-4, -1, 0), (-4, -2, 0.08), (4, -2, 0.3), (5, -2, 0.18),
            (4, -1, 0.06), (3, -1, 0), (2, -1, 0), (1, -1, 0), (0, -1, 0), (0, -1, 0)]
    for f, (lean, bob, fl) in enumerate(keys):
        yield render(ix, lab, main, loose, pal, sway=1.6, phase=2 * math.pi * f / 16, bob=bob, lean=lean, drift=1.6, flash=fl)


def hurt(ix, pal):
    lab, main, loose = parts(ix)
    keys = [(-3, -2, 0.5), (-4, -2, 0.3), (-3, -1, 0.1), (-2, -1, 0), (-1, -1, 0), (0, -1, 0)]
    for f, (lean, shift, fl) in enumerate(keys):
        yield render(ix, lab, main, loose, pal, sway=1.6, phase=0.4 * f, bob=-1, lean=lean, shift=shift, drift=1.6, flash=fl)


# ---------------------------------------------------------------- sheets

def to_rgba(ix, pal):
    rgba = np.zeros((N, N, 4), np.uint8)
    m = ix >= 0
    rgba[m, :3] = pal[ix[m]]
    rgba[m, 3] = 255
    return rgba


def write_sheet(out_dir, name, frames, pal, fps, loop, extra=None):
    cols = 8
    rows = (len(frames) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * N, rows * N))
    meta_frames = []
    for i, f in enumerate(frames):
        x, y = (i % cols) * N, (i // cols) * N
        sheet.paste(Image.fromarray(to_rgba(f, pal)), (x, y))
        meta_frames.append({"filename": f"{name} {i}", "frame": {"x": x, "y": y, "w": N, "h": N}, "rotated": False, "trimmed": False,
                            "spriteSourceSize": {"x": 0, "y": 0, "w": N, "h": N}, "sourceSize": {"w": N, "h": N},
                            "duration": round(1000 / fps)})
    sheet.save(os.path.join(out_dir, name + ".png"))
    meta = {"app": "solar-and-dark/tools/sprites/make.py", "version": "1", "image": name + ".png", "format": "RGBA8888",
            "size": {"w": sheet.width, "h": sheet.height}, "scale": "1",
            "frameTags": [{"name": name, "from": 0, "to": len(frames) - 1, "direction": "forward"}],
            "pixelfx": {"id": "sd-body/" + name, "name": name, "fps": fps, "loop": loop, "frames": len(frames), "frameSize": [N, N],
                        "pivot": list(PIVOT), "columns": cols, "palette": ["#%02x%02x%02x" % tuple(c) for c in pal], **(extra or {})}}
    with open(os.path.join(out_dir, name + ".json"), "w", encoding="utf-8") as fh:
        json.dump({"frames": meta_frames, "meta": meta}, fh, indent=1)
    return sheet


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(ROOT, "Assets/PixelFx/Sheets/characters"))
    ap.add_argument("--preview", default=os.path.join(ROOT, "records/sprites"))
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True); os.makedirs(a.preview, exist_ok=True)
    stills = []
    for boss, names in BOSSES.items():
        ixs, pal = pixelize({n: load(n) for n in names})
        for n in names:
            ix = ixs[n]
            write_sheet(a.out, n + "-idle", list(idle(ix, pal)), pal, 10, True)
            write_sheet(a.out, n + "-cast", list(cast(ix, pal)), pal, 12, False, {"release": 4 / 12})
            write_sheet(a.out, n + "-hurt", list(hurt(ix, pal)), pal, 12, False)
            stills.append((n, to_rgba(ix, pal)))
            print(f"{n}: idle 16, cast 12, hurt 6 frames, {len(np.unique(ix[ix >= 0]))} colours")
    row = Image.new("RGBA", (len(stills) * (N + 8), N), (90, 88, 104, 255))
    for i, (_, s) in enumerate(stills):
        row.alpha_composite(Image.fromarray(s), (i * (N + 8), 0))
    row.save(os.path.join(a.preview, "stills.png"))


if __name__ == "__main__":
    main()
