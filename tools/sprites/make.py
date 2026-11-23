"""Boss body sprites from the concept art: 128 x 128 pixel art per phase, with idle / cast / hurt animations, written as
pixelfx-format sprite sheets (PNG + JSON) that Unity's PixelFxSheet importer reads.

    python3 tools/sprites/make.py [--out Assets/PixelFx/Sheets/characters] [--preview records/sprites]

Input: art/bosses/cutouts/<boss>-p<n>.png (cut from the concept sheet by tools/sprites/cut.py).

Pixel art: every phase of a boss is scaled by the same factor (the largest is 118 px tall and 116 wide, feet 2 px above the bottom: the rest is room for the motions),
denoised (edge-keeping), given back its local contrast, area-averaged, snapped to a 12-colour palette per phase taken from its own figure (in Lab, so accents keep their hue), then
cleaned of single-pixel noise (mode filter), so the colours and the silhouette are the reference's. Nothing is redrawn.

Animation moves those pixels, it never paints new ones (nearest sampling; a flash maps each colour to a brighter colour
of the same palette):
  idle  16 frames at 10 fps, loop. The figure floats 1 px as one piece (nothing shears, so nothing tears); loose pieces
        (crystals, shadow scraps) drift on their own phases.
  strike / slam / channel  12 frames at 12 fps, one per kind of attack (MOTIONS below): the release on frame 4
        (0.33 s: BossBody.castRelease) is when the skill's effect bursts.
  hurt   6 frames at 12 fps. A flash and a recoil, then back.
"""
import argparse, json, math, os
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
N = 128
BOSSES = {"serena": ["serena-p1", "serena-p2", "serena-p3"], "child": ["child-p1", "child-p2", "child-p3"]}
PIVOT = (N // 2, N - 2)   # the feet
FIT = 118                 # figure height: the rest of the frame is room for the motions (never cut at the edges)


# ---------------------------------------------------------------- pixel art

def load(name):
    a = np.asarray(Image.open(os.path.join(ROOT, "art/bosses/cutouts", name + ".png"))).astype(np.float32) / 255
    ys, xs = np.nonzero(a[..., 3] > 0.5)
    return a[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1]


def mode_filter(idx, pal, passes=3, need=4, keep=28):
    """Noise: a pixel that shares its colour with fewer than two neighbours takes the colour most of them agree on,
    unless it stands out from that colour by more than `keep` in lightness (an eye, a line, a glint: detail, kept)."""
    L = cv2.cvtColor(pal[None].astype(np.uint8), cv2.COLOR_RGB2LAB)[0][:, 0].astype(np.float32) * 100 / 255
    h, w = idx.shape
    for _ in range(passes):
        src = idx.copy()
        for y in range(1, h - 1):
            for x in range(1, w - 1):
                c = src[y, x]
                if c < 0: continue
                nb = np.delete(src[y - 1:y + 2, x - 1:x + 2].ravel(), 4)
                nb = nb[nb >= 0]
                if (nb == c).sum() >= 2: continue
                v, n = np.unique(nb, return_counts=True)
                if len(v) and n.max() >= need:
                    t = v[np.argmax(n)]
                    if abs(L[c] - L[t]) <= keep: idx[y, x] = t
    return idx


def pixelize(crops, colours=12):
    sc = min(FIT / max(c.shape[0] for c in crops.values()), (N - 12) / max(c.shape[1] for c in crops.values()))
    small = {}
    for k, c in crops.items():
        # the source is soft and carries compression noise: an edge-keeping denoise, then local contrast back
        rgb = cv2.bilateralFilter((c[..., :3] * 255).astype(np.uint8), 7, 40, 5).astype(np.float32) / 255
        rgb = np.clip(rgb + 2.2 * (rgb - cv2.GaussianBlur(rgb, (0, 0), 1.2)), 0, 1)
        pm = np.dstack([rgb * c[..., 3:4], c[..., 3]])
        h, w = c.shape[:2]
        r = cv2.resize(pm, (max(1, round(w * sc)), max(1, round(h * sc))), interpolation=cv2.INTER_AREA)
        al = r[..., 3]
        small[k] = (np.where(al[..., None] > 1e-3, r[..., :3] / np.maximum(al[..., None], 1e-3), 0), al > 0.5)
    crit = (cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 60, 0.2)
    out, pals = {}, {}
    for k, (rgb, m) in small.items():
        # a palette per phase, few colours, chosen in Lab (perceptual) so a small accent (gold, a violet glow) keeps its
        # own colour instead of averaging into the cloth
        lab = cv2.cvtColor(rgb.astype(np.float32), cv2.COLOR_RGB2LAB)
        _, _, cen = cv2.kmeans(lab[m].astype(np.float32), colours, None, crit, 5, cv2.KMEANS_PP_CENTERS)
        cen = cen[np.argsort(cen[:, 0])]                          # dark to light
        pal = (cv2.cvtColor(cen[None].astype(np.float32), cv2.COLOR_LAB2RGB)[0] * 255).clip(0, 255)
        pals[k] = pal.astype(np.uint8)
        idx = np.argmin(((lab.reshape(-1, 3)[:, None, :] - cen[None]) ** 2).sum(-1), 1).reshape(m.shape)
        idx = mode_filter(np.where(m, idx, -1).astype(np.int16), pals[k])
        m = idx >= 0
        nb = cv2.filter2D(m.astype(np.uint8), -1, np.array([[1, 1, 1], [1, 0, 1], [1, 1, 1]], np.float32))
        m &= nb > 0                                               # no lone pixels
        n, lab, st, _ = cv2.connectedComponentsWithStats(m.astype(np.uint8), 8)
        for i in range(1, n):
            if st[i, cv2.CC_STAT_AREA] < 12: m &= lab != i       # nor specks
        h, w = m.shape
        canvas = np.full((N, N), -1, np.int16)                    # palette index per pixel, -1 = clear
        ox, oy = PIVOT[0] - w // 2, PIVOT[1] - h
        canvas[oy: oy + h, ox: ox + w] = np.where(m, idx, -1)
        out[k] = canvas
    return out, pals


HALO_ZONE = {"serena-p1": 0.34, "serena-p2": 0.34, "serena-p3": 0.34, "child-p1": 0.3, "child-p2": 0.3}   # child-p3's halo zone is all tendril


def crisp(name, ix, pal):
    """The pixel artist's pass the source never had: the halo as clean light lines (the muddy shadow seen through the
    ring goes, the lines keep a 1 px dark edge), and a dark rim where a light colour meets the outside, so the figure
    reads sharp on any background. The silhouette does not grow."""
    luma = pal.astype(np.float32) @ np.array([0.299, 0.587, 0.114], np.float32)
    L = np.where(ix >= 0, luma[np.maximum(ix, 0)], -1)
    ys = np.nonzero((ix >= 0).any(1))[0]
    top, bottom = ys.min(), ys.max()
    out = ix.copy()
    zone = np.zeros_like(ix, bool)
    if name in HALO_ZONE:
        zone[top: top + int((bottom - top) * HALO_ZONE[name])] = True
        # the shadow seen through the ring is a thick dark mass; the ring and its spokes are 1-2 px lines
        dark = (zone & (ix >= 0) & (L < 72)).astype(np.uint8)
        thick = cv2.dilate(cv2.erode(dark, np.ones((3, 3), np.uint8)), np.ones((3, 3), np.uint8)) > 0
        bright = cv2.dilate((L > 140).astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
        out[thick & ~bright] = -1
        if name.startswith("serena"):
            # the halo as clean light lines: away from the head (a thick light mass: the halo's lines are too thin to
            # survive an opening), dark pixels in the zone are the shadow and glow seen through the ring
            head = cv2.morphologyEx((L > 110).astype(np.uint8), cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
            head = cv2.dilate(head, np.ones((5, 5), np.uint8)) > 0
            out[zone & (out >= 0) & (L < 90) & ~head] = -1
    solid = (out >= 0).astype(np.uint8)
    edge = (solid > 0) & (cv2.erode(solid, np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]], np.uint8)) == 0)
    L2 = np.where(out >= 0, luma[np.maximum(out, 0)], -1)
    out[edge & (L2 > 90) & ~zone] = 0                            # darkest colour of the boss's palette; thin halo lines keep theirs
    return out


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


def render(ix, lab, main, loose, pal, *, phase=0.0, bob=0, lean=0.0, shift=0, drift=0.0, flash=0.0):
    """One frame: destination pixel (x, y) samples the source at (x - dx, y - dy)."""
    h, w = ix.shape
    Y, X = np.mgrid[0:h, 0:w].astype(np.float32)
    # the figure moves as one piece: shifting rows by different amounts (a sway, a leaning shear) slides them past each
    # other and the sprite tears like a game without v-sync. A lean is a sideways move of the whole figure.
    dx = np.full((h, w), shift + 0.5 * lean, np.float32)
    out = np.full((h, w), -1, np.int16)
    srcx = np.rint(X - dx).astype(int)
    srcy = np.rint(Y - bob).astype(int)
    ok = (srcx >= 0) & (srcx < w) & (srcy >= 0) & (srcy < h)
    sample = np.full((h, w), -1, np.int16)
    sample[ok] = ix[srcy[ok], srcx[ok]]
    slab = np.zeros((h, w), np.int32)
    slab[ok] = lab[srcy[ok], srcx[ok]]
    out = np.where((slab == main) | (slab == 0), sample, -1).astype(np.int16)
    # loose pieces drift up and down on their own, each on its own phase, and move with the figure
    for j, i in enumerate(loose):
        m = lab == i
        oy = int(round(bob + drift * math.sin(phase * 1.0 + j * 1.7)))
        ox = int(round(shift + 0.5 * lean))
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
        yield render(ix, lab, main, loose, pal, phase=ph, bob=int(round(-0.6 - 0.6 * math.sin(ph))), drift=1.6)


# The body's part in each attack. Frame 4 (0.33 s at 12 fps) is the release in all three; BossBody times it to the end of
# the skill's wind-up, which is when the effect bursts. Keys: (lean px at the head, float px, shift px, brightening).
MOTIONS = {
    # slashes, the dash: wind up drawing back, then lunge in with the blow, the light caught on it, and settle
    "strike": [(0, 0, -1, 0), (0, -1, -2, 0), (0, -2, -3, 0), (0, -2, -4, 0.06), (0, -1, 5, 0.3), (0, -1, 6, 0.2),
               (0, -1, 5, 0.08), (0, -1, 4, 0), (0, -1, 3, 0), (0, -1, 2, 0), (0, -1, 1, 0), (0, -1, 0, 0)],
    # ground eruptions: rise as the power gathers, then come down hard on the release, as if driving it into the floor
    "slam": [(0, -1, 0, 0), (0, -2, 0, 0.03), (0, -4, 0, 0.06), (0, -6, 0, 0.1), (0, 1, 0, 0.32), (0, 1, 0, 0.18),
             (0, 0, 0, 0.06), (0, -1, 0, 0), (0, -1, 0, 0), (0, -1, 0, 0), (0, -1, 0, 0), (0, -1, 0, 0)],
    # spells that last: rise with the glow building, flare on the release, then hold up and lit while the spell runs
    # (frames 5-10 loop for as long as it lasts: BossBody), and come down on 11
    "channel": [(0, -1, 0, 0), (0, -2, 0, 0.05), (0, -3, 0, 0.1), (0, -4, 0, 0.16), (0, -5, 0, 0.32), (0, -5, 0, 0.22),
                (0, -5, 0, 0.18), (0, -6, 0, 0.16), (0, -6, 0, 0.18), (0, -5, 0, 0.2), (0, -5, 0, 0.18), (0, -2, 0, 0.05)],
}
CHANNEL_HOLD = (5, 10)


def cast(ix, pal, motion):
    lab, main, loose = parts(ix)
    for f, (lean, bob, shift, fl) in enumerate(MOTIONS[motion]):
        # in the held stretch the drift keeps its own 6-frame loop so it can repeat seamlessly
        ph = 2 * math.pi * ((f - CHANNEL_HOLD[0]) / 6 if motion == "channel" and CHANNEL_HOLD[0] <= f <= CHANNEL_HOLD[1] else f / 16)
        yield render(ix, lab, main, loose, pal, phase=ph, bob=bob, lean=lean, shift=shift, drift=1.6, flash=fl)


def hurt(ix, pal):
    lab, main, loose = parts(ix)
    keys = [(0, -2, 0.5), (0, -3, 0.3), (0, -2, 0.1), (0, -1, 0), (0, -1, 0), (0, 0, 0)]
    for f, (lean, shift, fl) in enumerate(keys):
        yield render(ix, lab, main, loose, pal, phase=0.4 * f, bob=-1, lean=lean, shift=shift, drift=1.6, flash=fl)


# ---------------------------------------------------------------- sheets

def check_frames(name, base, frames):
    """Self-check: no frame of an animation may lose pixels to the frame's edge, or touch it."""
    n0 = int((base >= 0).sum())
    for i, f in enumerate(frames):
        solid = f >= 0
        assert abs(int(solid.sum()) - n0) <= n0 * 0.02, f"{name} frame {i}: {n0 - int(solid.sum())} px lost (cut at an edge?)"
        assert not (solid[0].any() or solid[:, 0].any() or solid[:, -1].any()), f"{name} frame {i} touches the frame's edge"


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
        ixs, pals = pixelize({n: load(n) for n in names})
        for n in names:
            pal = pals[n]
            ix = crisp(n, ixs[n], pal)
            anims = {"idle": (list(idle(ix, pal)), 10, True, None)}
            for motion in MOTIONS:
                extra = {"release": 4 / 12, **({"hold": list(CHANNEL_HOLD)} if motion == "channel" else {})}
                anims[motion] = (list(cast(ix, pal, motion)), 12, False, extra)
            anims["hurt"] = (list(hurt(ix, pal)), 12, False, None)
            for kind, (frames, fps, loop, extra) in anims.items():
                check_frames(f"{n}-{kind}", ix, frames)
                write_sheet(a.out, f"{n}-{kind}", frames, pal, fps, loop, extra)
            stills.append((n, to_rgba(ix, pal)))
            print(f"{n}: idle 16, strike/slam/channel 12, hurt 6 frames, {len(np.unique(ix[ix >= 0]))} colours")
    row = Image.new("RGBA", (len(stills) * (N + 8), N), (90, 88, 104, 255))
    for i, (_, s) in enumerate(stills):
        row.alpha_composite(Image.fromarray(s), (i * (N + 8), 0))
    row.save(os.path.join(a.preview, "stills.png"))


if __name__ == "__main__":
    main()
