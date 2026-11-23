"""Serena's 128 x 128 bodies, one design per phase, as the concept sheet plans them (art/bosses/concept/sheet-2.webp:
phase 1 the shadow, a dark sword; phase 2 the fusion, a glowing blade; phase 3 purified, white and gold). The high-
resolution art (serena-hires.jpg) is a reference for her look, not something to copy.

Each phase's figure on the sheet gives only its layout (cutouts art/bosses/cutouts/serena-p<n>.png): which part is
cloth, hair, skin, gold or line and how light it is there. Everything else is designed here:
  - colour: designed ramps per material and per phase; a pixel is (material, step), so nothing is sampled
  - the halo: a clean ring behind the head with the phase's spikes, its colour warming across the phases
    (silver, pale lilac, gold at the awakening)
  - the sword: a clean blade (edge light, core, edge shade), guard, grip and pommel along the sheet's line
  - the face: hand-placed per phase (veiled with glowing eyes / calm / gentle and open)
  - a dark rim where a light colour meets the outside

    python3 tools/sprites/serena.py      # writes records/sprites/serena-design.png (the three phases side by side)
"""
import math, os
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
N, FIT = 128, 118   # 118 px tall: room in the frame for the body's motions (make.py)
PURPLE, WHITE, SKIN, GOLD, LINE, BLADE = range(6)

H = lambda h: [int(h[i:i + 2], 16) for i in (1, 3, 5)]
# designed ramps, dark to light; one length per material across phases (the step is what a phase keeps)
RAMPS = {
    1: {PURPLE: ["#0f0b1c", "#1a1430", "#261c44", "#352659", "#47336f", "#5d4688", "#7a64a5", "#9a88c0", "#bbb0d8"],
        WHITE: ["#6f6890", "#938cb2", "#b9b3d2"], SKIN: ["#a98d9e", "#cdb4bd", "#e7d8db"],
        GOLD: ["#6d6984", "#aaa6c4", "#e8e6f4"], LINE: ["#07050e"],
        BLADE: ["#09060f", "#1d1530", "#3b2a5e", "#8d6fd0"]},           # a black blade, violet at its edge
    2: {PURPLE: ["#1a1330", "#2c204a", "#40296f", "#5c4094", "#7b57b9", "#9a72d4", "#b796e8", "#d3bdf5", "#ebe1fc"],
        WHITE: ["#c2b6dc", "#e2daf2", "#f8f5ff"], SKIN: ["#c99aa6", "#ebc7c8", "#fde9e4"],
        GOLD: ["#6f6280", "#b9adc6", "#f3eef6"], LINE: ["#0e0918"],
        BLADE: ["#8a6a3a", "#e2bd72", "#fbe6b0", "#fffaf0"]},           # light gathering in the blade
    3: {PURPLE: ["#2b2350", "#3e3270", "#5a4c8c", "#7c6eaa", "#9d92c4", "#bdb4da", "#d9d2ec", "#efeaf7", "#ffffff"],
        WHITE: ["#b8acd6", "#e6e0f4", "#fffdfb"], SKIN: ["#d8a9b1", "#f1d1cf", "#fff1ea"],
        GOLD: ["#9a7334", "#d9ad5e", "#fbe8a8"], LINE: ["#2a2240"],
        BLADE: ["#6d6a86", "#b8b6cc", "#e9e8f4", "#ffffff"]},           # bright steel
}
MATERIALS = (PURPLE, WHITE, SKIN, GOLD, LINE, BLADE)

# per phase, in sheet-2 pixels (the cutout is the panel's crop): the panel's corner, the halo (centre, radius, spike
# angles: 270 is up), the left eye, the sword (hand, tip)
SHEET = {
    1: {"corner": (468, 85), "halo": (557, 150, 43, [270, 225, 315, 180, 0]), "eye": (551, 160),
        "sword": ((606, 262), (667, 330))},
    2: {"corner": (650, 75), "halo": (765, 150, 50, [270, 240, 300, 210, 330, 180, 0]), "eye": (760, 158),
        "sword": ((733, 262), (885, 347))},
    3: {"corner": (884, 70), "halo": (1018, 155, 58, [270, 247, 293, 225, 315, 200, 340, 180, 0]), "eye": (1009, 160),
        "sword": ((1008, 255), (906, 350))},
}

# Faces, designed at sprite size (12 x 10, bangs included). '.' keeps what is there.
# H/h/g hair, s/m skin (lit, shade), b blush, d mouth, L line, e iris dark, i iris light, w catch-light,
# v veil (cloth over the lower face), o glowing eye. Row 3, column 2 is the left eye.
FACES = {
    1: [  # the shadow: hair over the brow, a dark veil over mouth and chin, two violet points of light for eyes
        "hhhhhgghhhg.",
        "ghhsssghhsgL",
        "gsLLLssLLLhL",
        "gsooLsssoo.L",
        "gsLsssssLsgL",
        "gvvvvvvvvvvg",
        "gvvvvvvvvvv.",
        ".gvvvvvvvv..",
        "..gvvvvvvg..",
        "...gLvvLg...",
    ],
    2: [  # the fusion: calm, half-lidded, the hint of a smile
        "HHHHHhhHHHg.",
        "hHssssHhHsgL",
        "gsLLLssssshL",
        "gswesssLLLmL",
        "gsiisssweimL",
        "gmsssssiismg",
        "gmbssssssbm.",
        ".gmssddssm..",
        "..gmsssmmg..",
        "...gLmmLg...",
    ],
    3: [  # purified: eyes open and gentle, light in them, a soft smile
        "HHHHHHHHHHh.",
        "HHssssHHHshL",
        "hsLLLssLLLsL",
        "hswesssweisL",
        "hsiisssiismL",
        "hssssssssssh",
        "hmbssssssbm.",
        ".hmssddssm..",
        "..hmsssmmh..",
        "...hLmmLh...",
    ],
}


def palette(phase):
    pal, base = [], {}
    for k in MATERIALS:
        base[k] = len(pal)
        pal += [H(c) for c in RAMPS[phase][k]]
    return np.array(pal, np.uint8), base


def _art(phase):
    a = np.asarray(Image.open(os.path.join(ROOT, f"art/bosses/cutouts/serena-p{phase}.png")))
    ys, xs = np.nonzero(a[..., 3] > 127)
    return a[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1], (xs.min(), ys.min())


def _materials(a):
    lab = cv2.cvtColor(a[..., :3], cv2.COLOR_RGB2LAB).astype(np.float32)
    L, A, B = lab[..., 0] * 100 / 255, lab[..., 1] - 128, lab[..., 2] - 128
    chroma = np.hypot(A, B)
    mat = np.full(L.shape, PURPLE, np.int8)
    mat[(chroma < 16) & (L > 74) & (B <= 3)] = WHITE                  # light and cool: the dress
    mat[(B > 3) & (L > 66) & (B < 20) & (chroma < 30)] = SKIN         # light and warm
    mat[(B > 12) & (B >= A * 1.4) & (chroma > 16)] = GOLD             # yellow
    mat[L < 14] = LINE
    mat[a[..., 3] <= 127] = -1
    return mat, L


def layout(phase):
    """The phase's figure as (material, lightness) per sprite pixel, and its geometry at sprite scale."""
    a, (bx, by) = _art(phase)
    mat, L = _materials(a)
    Hh, Ww = mat.shape
    sc = min(FIT / Hh, (N - 12) / Ww)
    h, w = round(Hh * sc), round(Ww * sc)
    m = np.full((h, w), -1, np.int8)
    light = np.zeros((h, w), np.float32)
    for y in range(h):
        y0, y1 = int(y / sc), max(int(y / sc) + 1, int((y + 1) / sc))
        for x in range(w):
            x0, x1 = int(x / sc), max(int(x / sc) + 1, int((x + 1) / sc))
            f = mat[y0:y1, x0:x1].ravel()
            if (f >= 0).sum() * 2 < f.size: continue
            cnt = np.bincount(f[f >= 0], minlength=6)
            k = LINE if cnt[LINE] >= 0.3 * f.size else int(np.argmax(cnt[:4]))
            m[y, x] = k
            light[y, x] = L[y0:y1, x0:x1].ravel()[f == k].mean()
    # a lone pixel of a material none of its neighbours have takes theirs (lines excepted)
    for _ in range(2):
        src = m.copy()
        for y in range(1, h - 1):
            for x in range(1, w - 1):
                c = src[y, x]
                if c < 0 or c == LINE: continue
                nb = np.delete(src[y - 1:y + 2, x - 1:x + 2].ravel(), 4)
                nb = nb[(nb >= 0) & (nb != LINE)]
                if not len(nb) or (nb == c).sum() >= 2: continue
                v, n = np.unique(nb, return_counts=True)
                m[y, x] = v[np.argmax(n)]
    cx, cy = SHEET[phase]["corner"]
    to = lambda p: ((p[0] - cx - bx) * sc, (p[1] - cy - by) * sc)    # sheet px -> sprite px
    hx, hy, hr, spikes = SHEET[phase]["halo"]
    # the sheet's ring, measured to its spikes' roots, reads too wide at sprite size: the halo frames the head
    geo = {"halo": (*to((hx, hy)), hr * sc * 0.72), "sheet_ring": hr * sc, "spikes": spikes, "eye": to(SHEET[phase]["eye"]),
           "sword": tuple(to(p) for p in SHEET[phase]["sword"])}
    return m, light, geo


def paint(phase, m, light, geo):
    pal, base = palette(phase)
    h, w = m.shape
    # skin is the face, neck, shoulders and arms: warm cream lower down or far from the face is cloth (phase 3's ribbons)
    ex0, ey0 = geo["eye"]
    Yy, Xx = np.mgrid[0:h, 0:w]
    m = m.copy()
    m[(m == SKIN) & ((Yy > h * 0.55) | (np.hypot(Xx - ex0, Yy - ey0) > w * 0.38))] = WHITE
    ix = np.full((h, w), -1, np.int16)
    for k in (PURPLE, WHITE, SKIN, GOLD, LINE):
        sel = m == k
        n = len(RAMPS[phase][k])
        if sel.any():
            # the step is the pixel's brightness rank within its material: every phase uses its whole ramp, shadow to
            # highlight, however light or dark its figure is on the sheet
            q = np.argsort(np.argsort(light[sel])) / max(1, sel.sum() - 1)
            ix[sel] = base[k] + np.minimum((q * n).astype(int), n - 1)

    def put(x, y, idx, behind=False):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < w and 0 <= y < h and (not behind or ix[y, x] < 0): ix[y, x] = idx

    gd, gm, gl = base[GOLD], base[GOLD] + 1, base[GOLD] + 2
    hx, hy, hr = geo["halo"]
    ex, ey = geo["eye"]
    # halo: clear what the sheet shows of it (soft, half lost in the cut), then a clean ring behind the head, lit at the
    # top left, and the phase's spikes, the long ones tipped with a star
    Y, X = np.mgrid[0:h, 0:w]
    d = np.hypot(X - hx, Y - hy)
    r0 = geo["sheet_ring"]                                            # where the sheet's own ring and spikes are
    # above the chin only: lower down the sheet's ring passes behind her neck and shoulders, which stay
    old = ((np.abs(d - r0) < 3.5) & (Y < ey + 3)) | ((d > r0) & (d < r0 * 1.9) & (Y < ey - 3))
    ix[old & (m != SKIN)] = -1
    # the sheet's floating crystals and sparks above her are effects, not her body (the skills draw their own);
    # cleared before the halo is drawn, which goes behind what is left
    ix[Y < hy - hr * 0.6] = -1
    for t in range(720):
        a = math.radians(t / 2)
        lit = math.cos(a + math.radians(135))
        put(hx + hr * math.cos(a), hy + hr * math.sin(a), gl if lit > 0.35 else gm if lit > -0.5 else gd, behind=True)
    for i, ang in enumerate(geo["spikes"]):
        a = math.radians(ang)
        long = i == 0 or ang in (180, 0)
        length = hr * (1.75 if i == 0 else 1.45 if long else 1.3)
        for s in np.arange(hr * 0.8, length, 0.5):
            u = (s - hr * 0.8) / (length - hr * 0.8)
            put(hx + s * math.cos(a), hy + s * math.sin(a), gl if u > 0.45 else gm, behind=True)
        if long:
            tx, ty = hx + length * math.cos(a), hy + length * math.sin(a)
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): put(tx + dx, ty + dy, gm, behind=True)
            put(tx, ty, gl)
    # sword: from the hand to the tip; a two-tone blade with a lit edge, a guard across it at the hand, a short grip
    # and pommel behind the hand
    (sx, sy), (tx, ty) = geo["sword"]
    L_ = math.hypot(tx - sx, ty - sy)
    ux, uy = (tx - sx) / L_, (ty - sy) / L_
    nx, ny = -uy, ux                                                  # across the blade
    b = base[BLADE]
    for s in np.arange(1.5, L_, 0.4):
        u = s / L_
        half = 1.2 if u < 0.8 else 0.6
        put(sx + ux * s, sy + uy * s, b + 2)
        put(sx + ux * s + nx * half, sy + uy * s + ny * half, b + 1)
        put(sx + ux * s - nx * half, sy + uy * s - ny * half, b + 3 if u < 0.85 else b + 2)
        if u < 0.8: put(sx + ux * s + nx * 1.8, sy + uy * s + ny * 1.8, b)
    put(tx, ty, b + 3)
    for k in np.arange(-3, 3.5, 0.5):                                 # the guard
        put(sx + ux * 1.5 + nx * k, sy + uy * 1.5 + ny * k, gm if abs(k) < 2.5 else gl)
    for s in np.arange(-3.5, 1, 0.5): put(sx + ux * s, sy + uy * s, base[LINE] if phase == 1 else gd)   # grip
    put(sx - ux * 4, sy - uy * 4, gl)                                 # pommel
    # face: the phase's designed grid, its left eye on the sheet's
    fx, fy = int(round(ex)) - 2, int(round(ey)) - 3
    key = {"H": base[PURPLE] + 8, "h": base[PURPLE] + 7, "g": base[PURPLE] + 5, "s": base[SKIN] + 2, "m": base[SKIN] + 1,
           "b": base[SKIN], "d": base[SKIN], "L": base[LINE], "e": base[PURPLE] + 2, "i": base[PURPLE] + 4,
           "w": base[WHITE] + 2, "v": base[PURPLE] + 1, "o": base[PURPLE] + 8}
    for r, row in enumerate(FACES[phase]):
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
    # no specks left over from the cut
    n, lab, st, _ = cv2.connectedComponentsWithStats((ix >= 0).astype(np.uint8), 8)
    for i in range(1, n):
        x0, y0, ww, hh, ar = st[i]
        if ar < 15 or (ar < 40 and ww <= 3 and y0 + hh >= h - 2): ix[lab == i] = -1   # specks; thin slivers at the floor
    canvas = np.full((N, N), -1, np.int16)
    ox, oy = N // 2 - w // 2, N - 2 - h
    canvas[oy: oy + h, ox: ox + w] = ix
    return canvas, pal


def design():
    """{phase: (index canvas, palette)} for phases 1-3."""
    return {p: paint(p, *layout(p)) for p in (1, 2, 3)}


if __name__ == "__main__":
    out = design()
    row = Image.new("RGBA", (3 * (N + 8), N), (58, 54, 74, 255))
    for i, p in enumerate((1, 2, 3)):
        ix, pal = out[p]
        rgba = np.zeros((N, N, 4), np.uint8); mm = ix >= 0
        rgba[mm, :3] = pal[ix[mm]]; rgba[mm, 3] = 255
        row.alpha_composite(Image.fromarray(rgba), (i * (N + 8), 0))
    os.makedirs(os.path.join(ROOT, "records/sprites"), exist_ok=True)
    row.save(os.path.join(ROOT, "records/sprites/serena-design.png"))
    row.convert("RGB").resize((row.width * 3, row.height * 3), Image.NEAREST).save(os.path.join(ROOT, "records/sprites/serena-design-3x.png"))
