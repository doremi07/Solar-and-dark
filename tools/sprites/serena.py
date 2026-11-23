"""Serena's 128 x 128 body, designed after the high-resolution art (art/bosses/cutouts/serena-hires.png).

The art is the reference, not the pixels: it gives the layout (which part is hair, cloth, skin, gold, line, and how
light or dark it is there); the sprite is painted from designed colour ramps, one per material, and the parts a shrink
cannot carry are drawn on top: the halo as a clean ring with its spikes, gold chains and stars as 1 px lines and star
shapes, the face (eyes with their light, mouth) and a dark rim on light edges.

Each pixel is a (material, step) pair, so a phase is the same drawing with its own ramps: phase 1 sinks into shadow
(silver halo), phase 2 is the art's own colours with the concept's pale halo (the halo warms
from silver to gold across the phases: gold arrives with the awakening), phase 3 is purified as the concept sheet draws it
(white and cream cloth, hair and ribbons with lavender shade, bright gold).

    python3 tools/sprites/serena.py      # writes records/sprites/serena-design.png (the three phases side by side)
"""
import math, os
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
N, FIT = 128, 118   # 118 px tall: room in the frame for the body's motions (make.py)
PURPLE, WHITE, SKIN, GOLD, LINE = range(5)

H = lambda h: [int(h[i:i + 2], 16) for i in (1, 3, 5)]
# designed ramps, dark to light; a phase keeps the step and swaps the ramp, so every ramp of a material has one length
RAMPS = {
    2: {PURPLE: ["#1a1330", "#2c204a", "#40296f", "#5c4094", "#7b57b9", "#9a72d4", "#b796e8", "#d3bdf5", "#ebe1fc"],
        WHITE: ["#c2b6dc", "#e2daf2", "#f8f5ff"], SKIN: ["#c99aa6", "#ebc7c8", "#fde9e4"],
        GOLD: ["#6f6280", "#b9adc6", "#f3eef6"], LINE: ["#0e0918"]},   # pale silver-lilac: gold is for the awakening
    1: {PURPLE: ["#0f0b1c", "#1a1430", "#261c44", "#352659", "#47336f", "#5d4688", "#7a64a5", "#9a88c0", "#bbb0d8"],
        WHITE: ["#6f6890", "#938cb2", "#b9b3d2"], SKIN: ["#a98d9e", "#cdb4bd", "#e7d8db"],
        GOLD: ["#5c586f", "#9894b3", "#dcd9ee"], LINE: ["#07050e"]},
    3: {PURPLE: ["#4a3d63", "#6a5a86", "#8e80a8", "#b0a5c6", "#cbc2db", "#e2dbea", "#f2ecf3", "#fbf7f6", "#ffffff"],
        WHITE: ["#d9d1ea", "#efeaf8", "#fffdfb"], SKIN: ["#d8a9b1", "#f1d1cf", "#fff1ea"],
        GOLD: ["#9a7334", "#d9ad5e", "#fbe8a8"], LINE: ["#3d3152"]},
}


def palette(phase):
    pal, base = [], {}
    for k in (PURPLE, WHITE, SKIN, GOLD, LINE):
        base[k] = len(pal)
        pal += [H(c) for c in RAMPS[phase][k]]
    return np.array(pal, np.uint8), base


def _art():
    a = np.asarray(Image.open(os.path.join(ROOT, "art/bosses/cutouts/serena-hires.png")))
    ys, xs = np.nonzero(a[..., 3] > 127)
    return a[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1], (xs.min(), ys.min())


# Her face, measured on the art (art pixels): the left eye's centre; the rest is designed around it.
LEFT_EYE = (632, 258)
# Designed at sprite size: the whole face block, bangs included (12 x 10). '.' keeps what is there.
# H/h/g hair (light, mid, shade), s/m skin (lit, shade), b blush, d mouth, L lashes and jaw line,
# e iris (dark), i iris (light), w catch-light. Calm, half-lidded eyes; the right one a pixel lower (her head tilts).
# Row 3, column 2 is the left eye's catch-light.
FACE = [
    "HHHHHhhHHHg.",
    "hHssssHhHsgL",
    "gsLLLssssshL",
    "gswesssLLLmL",
    "gsiisssweimL",
    "gmsssssiismg",
    "gmbssssssbm.",
    ".gmsssdssm..",
    "..gmsssmmg..",
    "...gLmmLg...",
]


def _materials(a):
    lab = cv2.cvtColor(a[..., :3], cv2.COLOR_RGB2LAB).astype(np.float32)
    L, A, B = lab[..., 0] * 100 / 255, lab[..., 1] - 128, lab[..., 2] - 128
    chroma = np.hypot(A, B)
    mat = np.full(L.shape, PURPLE, np.int8)
    mat[(chroma < 16) & (L > 76) & (B <= 3)] = WHITE                  # light and cool: the dress
    mat[(B > 3) & (L > 66) & (B < 20) & (chroma < 30)] = SKIN         # light and warm
    mat[(B > 12) & (B >= A * 1.4) & (chroma > 16)] = GOLD             # yellow
    mat[L < 13] = LINE
    mat[a[..., 3] <= 127] = -1
    return mat, L


def layout():
    """The drawing as (material, lightness) per sprite pixel, plus where the halo, eyes and stars are."""
    a, (ox, oy) = _art()
    mat, L = _materials(a)
    Hh, Ww = mat.shape
    sc = FIT / max(Hh, Ww)
    h, w = round(Hh * sc), round(Ww * sc)
    m = np.full((h, w), -1, np.int8)
    light = np.zeros((h, w), np.float32)
    gold_cover = np.zeros((h, w), np.float32)
    for y in range(h):
        y0, y1 = int(y / sc), max(int(y / sc) + 1, int((y + 1) / sc))
        for x in range(w):
            x0, x1 = int(x / sc), max(int(x / sc) + 1, int((x + 1) / sc))
            f = mat[y0:y1, x0:x1].ravel()
            if (f >= 0).sum() * 2 < f.size: continue
            cnt = np.bincount(f[f >= 0], minlength=5)
            gold_cover[y, x] = cnt[GOLD] / f.size
            k = LINE if cnt[LINE] >= 0.22 * f.size else int(np.argmax(cnt[:4]))
            m[y, x] = k
            light[y, x] = L[y0:y1, x0:x1].ravel()[f == k].mean()
    # a lone pixel of a material none of its neighbours have takes theirs (lines excepted)
    src = m.copy()
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            c = src[y, x]
            if c < 0 or c == LINE: continue
            nb = np.delete(src[y - 1:y + 2, x - 1:x + 2].ravel(), 4)
            nb = nb[(nb >= 0) & (nb != LINE)]
            if not len(nb) or (nb == c).any(): continue
            v, n = np.unique(nb, return_counts=True)
            m[y, x] = v[np.argmax(n)]
    # geometry the overpaint needs, measured on the art
    lab = cv2.cvtColor(a[..., :3], cv2.COLOR_RGB2LAB).astype(np.float32)
    A, B = lab[..., 1] - 128, lab[..., 2] - 128
    gold = (mat == GOLD)
    yy, xx = np.nonzero(gold[: int(Hh * 0.32)])
    c = np.linalg.lstsq(np.stack([xx, yy, np.ones_like(xx)], 1).astype(float), -(xx ** 2 + yy ** 2).astype(float), rcond=None)[0]
    hx, hy = -c[0] / 2, -c[1] / 2
    hr = math.sqrt(hx * hx + hy * hy - c[2])
    # spikes: directions where the halo's gold reaches well past the ring
    Yg, Xg = np.mgrid[0:Hh, 0:Ww]
    dist_all = np.hypot(Xg - hx, Yg - hy)
    ang_all = np.degrees(np.arctan2(Yg - hy, Xg - hx)) % 360
    # spikes: thin gold reaching past the ring in the upper arc (150 deg through the top round to 30 deg)
    upper = (ang_all >= 150) | (ang_all < 30)
    far = gold & upper & (dist_all > hr * 1.12) & (dist_all < hr * 2.2)
    ang, dist = ang_all[far], dist_all[far]
    hist = np.bincount((ang // 10).astype(int), minlength=36)
    spikes = []
    for b in range(36):
        if hist[b] > 60 and hist[b] >= hist[(b - 1) % 36] and hist[b] > hist[(b + 1) % 36]:
            sel = (ang // 10).astype(int) == b
            spikes.append((float(np.median(ang[sel])), float(np.percentile(dist[sel], 97))))
    # gold stars: the bigger blobs of gold away from the halo
    n, lab_, st, cen = cv2.connectedComponentsWithStats(gold.astype(np.uint8), 8)
    stars = [(cen[i][0], cen[i][1]) for i in range(1, n) if 250 < st[i, 4] < 6000 and math.hypot(cen[i][0] - hx, cen[i][1] - hy) > hr * 1.3]
    geo = {"scale": sc, "halo": (hx * sc, hy * sc, hr * sc), "spikes": [(a_, d * sc) for a_, d in spikes],
           "stars": [(x * sc, y * sc) for x, y in stars], "left_eye": ((LEFT_EYE[0] - ox) * sc, (LEFT_EYE[1] - oy) * sc)}
    return m, light, gold_cover, geo


def paint(phase, m, light, gold_cover, geo):
    pal, base = palette(phase)
    palL = cv2.cvtColor(pal[None], cv2.COLOR_RGB2LAB)[0][:, 0].astype(np.float32) * 100 / 255
    ref, _ = palette(2)                                               # steps are chosen on the art's own colours
    refL = cv2.cvtColor(ref[None], cv2.COLOR_RGB2LAB)[0][:, 0].astype(np.float32) * 100 / 255
    h, w = m.shape
    ix = np.full((h, w), -1, np.int16)
    for k in (PURPLE, WHITE, SKIN, GOLD, LINE):
        sel = m == k
        steps = np.arange(base[k], base[k] + len(RAMPS[phase][k]))
        if sel.any(): ix[sel] = steps[np.argmin(np.abs(light[sel][:, None] - refL[steps][None]), 1)]
    # gold chains: any pixel gold runs through, a step of gold by its light
    g = (gold_cover >= 0.14) & (m >= 0) & (m != LINE)
    ix[g] = base[GOLD] + np.clip(((light[g] - 45) / 20).astype(int), 0, 2)

    def put(x, y, idx):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < w and 0 <= y < h: ix[y, x] = idx

    def behind(x, y, idx):
        """Halo: drawn behind the figure, so only over empty space or the art's own halo gold."""
        x, y = int(round(x)), int(round(y))
        if 0 <= x < w and 0 <= y < h and (m[y, x] < 0 or m[y, x] == GOLD or m[y, x] == LINE): ix[y, x] = idx

    gd, gm, gl = base[GOLD], base[GOLD] + 1, base[GOLD] + 2
    # halo: a clean 1 px ring, lit at the top left, with its spikes (tapered, a star at each long one's tip)
    hx, hy, hr = geo["halo"]
    for t in range(0, 720):
        a = math.radians(t / 2)
        lit = math.cos(a + math.radians(135))                         # +1 at the top left
        behind(hx + hr * math.cos(a), hy + hr * math.sin(a), gl if lit > 0.35 else gm if lit > -0.5 else gd)
    for ang, length in geo["spikes"]:
        a = math.radians(ang)
        long = length > hr * 1.25
        for s in np.arange(hr, length, 0.5):
            u = (s - hr) / max(1e-3, length - hr)
            px, py = hx + s * math.cos(a), hy + s * math.sin(a)
            behind(px, py, gl if u > 0.5 else gm)
            if u < 0.35 and long:                                     # a thicker base
                behind(px - math.sin(a) * 0.8, py + math.cos(a) * 0.8, gd)
        if long:
            tx, ty = hx + length * math.cos(a), hy + length * math.sin(a)
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): behind(tx + dx, ty + dy, gm)
            behind(tx, ty, gl)
    # gold stars: a small four-point star at each
    for sx, sy in geo["stars"]:
        for d, idx in ((2, gd), (1, gm)):
            for dx, dy in ((d, 0), (-d, 0), (0, d), (0, -d)): put(sx + dx, sy + dy, idx)
        put(sx, sy, gl)
    # face: the designed grid, its left eye's catch-light on the art's left eye
    lx, ly = geo["left_eye"]
    fx, fy = int(round(lx)) - 2, int(round(ly)) - 3
    key = {"H": base[PURPLE] + 8, "h": base[PURPLE] + 7, "g": base[PURPLE] + 5, "s": base[SKIN] + 2, "m": base[SKIN] + 1,
           "b": base[SKIN], "d": base[SKIN], "L": base[LINE], "e": base[PURPLE] + 2, "i": base[PURPLE] + 4,
           "w": base[WHITE] + 2}
    for r, row in enumerate(FACE):
        for c, ch in enumerate(row):
            if ch != ".": put(fx + c, fy + r, key[ch])
    # rim: a light pixel on the outside edge of a solid area takes its material's darkest step
    solid = (ix >= 0).astype(np.uint8)
    edge = (solid > 0) & (cv2.erode(solid, np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]], np.uint8)) == 0)
    nb = cv2.filter2D(solid, -1, np.ones((3, 3), np.float32))
    for k in (PURPLE, WHITE, SKIN):
        lo, hi = base[k], base[k] + len(RAMPS[phase][k])
        sel = edge & (nb >= 6) & (ix >= lo + 2) & (ix < hi)
        ix[sel] = base[PURPLE] if k == PURPLE else lo
    canvas = np.full((N, N), -1, np.int16)
    ox, oy = N // 2 - w // 2, N - 2 - h
    canvas[oy: oy + h, ox: ox + w] = ix
    return canvas, pal


def design():
    """{phase: (index canvas, palette)} for phases 1-3."""
    lay = layout()
    return {p: paint(p, *lay) for p in (1, 2, 3)}


if __name__ == "__main__":
    out = design()
    row = Image.new("RGBA", (3 * (N + 8), N), (58, 54, 74, 255))
    for i, p in enumerate((1, 2, 3)):
        ix, pal = out[p]
        rgba = np.zeros((N, N, 4), np.uint8); m = ix >= 0
        rgba[m, :3] = pal[ix[m]]; rgba[m, 3] = 255
        row.alpha_composite(Image.fromarray(rgba), (i * (N + 8), 0))
    os.makedirs(os.path.join(ROOT, "records/sprites"), exist_ok=True)
    row.save(os.path.join(ROOT, "records/sprites/serena-design.png"))
