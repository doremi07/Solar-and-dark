"""Serena, phase 2, drawn by hand at 128 x 128 after the concept sheet (sheet-2, the fusion: silver hair, dark bodice
with a gold cross, white skirt, ragged shadow wings, a glowing sword held low to the right, the halo behind her head).

Every shape is chosen and placed here: flat areas of a few designed colours, one shade step on the side away from the
light (top left), 1 px lines only where a form needs one (sel-out: an area's outline is its own darkest tone, the
silhouette is the darkest line). Coordinates are sprite pixels, y down; the feet stand at y = 126.

    python3 tools/sprites/serena_hand.py   # records/sprites/serena-p2-hand.png (+ 4x preview)
"""
import math, os
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
N = 128
PALETTES = {
    1: {  # the shadow: near-black cloth, dim lilac hair, silver for gold, a black blade
        "ink": "#07040e", "sh0": "#0e0a1a", "sh1": "#181228", "sh2": "#2a2046", "pu1": "#3a2d62", "pu2": "#5b4b8e",
        "ha0": "#6c6296", "ha1": "#9086bc", "ha2": "#bcb4dc", "wh0": "#7a7298", "wh1": "#a8a1c4",
        "sk0": "#b496a6", "sk1": "#d9c4cb", "go0": "#7c7890", "go1": "#c4c0d8", "gl": "#e9e4ff"},
    2: {  # the fusion: the concept's own colours
        "ink": "#120b22", "sh0": "#1d1534", "sh1": "#2c2150", "sh2": "#43357a", "pu1": "#5e4b9c", "pu2": "#8676c4",
        "ha0": "#8f83c2", "ha1": "#b9aee6", "ha2": "#e2dcf8", "wh0": "#b7add8", "wh1": "#ece7fb",
        "sk0": "#d8a7b2", "sk1": "#f6dcd8", "go0": "#b8873f", "go1": "#f4dc98", "gl": "#fff7e2"},
    3: {  # purified: the shadow turned to pale ribbons of light, white and cream, bright gold
        "ink": "#3a2e58", "sh0": "#8e82b8", "sh1": "#c8bfe6", "sh2": "#e8e2f8", "pu1": "#7f6db8", "pu2": "#a596d8",
        "ha0": "#c7bfe6", "ha1": "#e6e1f7", "ha2": "#ffffff", "wh0": "#d6cdea", "wh1": "#fffcf6",
        "sk0": "#dcaab4", "sk1": "#fbe3de", "go0": "#c8953e", "go1": "#ffe08c", "gl": "#fffbe8"},
}
C = dict(PALETTES[2])


def px(img, x, y, c):
    if 0 <= x < N and 0 <= y < N: img.putpixel((int(x), int(y)), Image.new("RGB", (1, 1), C[c]).getpixel((0, 0)) + (255,))


def poly(d, pts, fill, line=None):
    d.polygon([(x, y) for x, y in pts], fill=C[fill], outline=C[line] if line else None)


def line(d, pts, c):
    d.line([(x, y) for x, y in pts], fill=C[c], width=1)


def bez(p0, p1, p2, p3, n=40):
    for i in range(n + 1):
        t = i / n; u = 1 - t
        yield (u ** 3 * p0[0] + 3 * u * u * t * p1[0] + 3 * u * t * t * p2[0] + t ** 3 * p3[0],
               u ** 3 * p0[1] + 3 * u * u * t * p1[1] + 3 * u * t * t * p2[1] + t ** 3 * p3[1], t)


def strand(d, ctrl, w0, w1, fill, edge="ink", lit=None):
    """A tapered, curving band (a lock of hair, a shadow tendril): width w0 at the root to w1 at the tip."""
    pts = list(bez(*ctrl))
    left, right = [], []
    for i, (x, y, t) in enumerate(pts):
        a, b = pts[max(0, i - 1)], pts[min(len(pts) - 1, i + 1)]
        dx, dy = b[0] - a[0], b[1] - a[1]; L = math.hypot(dx, dy) or 1
        w = (w0 + (w1 - w0) * t) / 2
        left.append((x - dy / L * w, y + dx / L * w)); right.append((x + dy / L * w, y - dx / L * w))
    poly(d, left + right[::-1], fill, edge)
    if lit:                                                            # a light line along the lit edge
        line(d, [(x - (y - y) , y) for x, y in left[2:-6:3]], lit)


def mass(img, shapes, base, shade, light=None, edge="ink"):
    """Several overlapping shapes merged into one volume: filled in its base colour, its lower right (away from the
    light) in the shade colour, its upper left rim lit, and a single outline round the whole silhouette."""
    import numpy as np, cv2
    m = Image.new("L", (N, N), 0); dm = ImageDraw.Draw(m)
    for pts in shapes: dm.polygon(pts, fill=255)
    a = np.asarray(m) > 0
    ker = np.ones((3, 3), np.uint8)
    inner = cv2.erode(a.astype(np.uint8), ker) > 0
    edgepx = a & ~inner
    # shade: pixels whose up-left neighbour 3 px away is inside, but whose down-right is near the edge
    sh = np.zeros_like(a)
    sh[:-2, :-2] = a[:-2, :-2] & ~cv2.erode(a.astype(np.uint8), np.ones((5, 5), np.uint8))[2:, 2:].astype(bool)
    lit = np.zeros_like(a)
    lit[2:, 2:] = a[2:, 2:] & ~cv2.erode(a.astype(np.uint8), np.ones((3, 3), np.uint8))[:-2, :-2].astype(bool)
    for (y, x) in zip(*np.nonzero(a)):
        c = edge if edgepx[y, x] else (light if (light and lit[y, x] and not sh[y, x]) else (shade if sh[y, x] else base))
        px(img, x, y, c)
    return a


def lock_shape(ctrl, w0, w1):
    pts = list(bez(*ctrl)); left, right = [], []
    for i, (x, y, t) in enumerate(pts):
        a, b = pts[max(0, i - 1)], pts[min(len(pts) - 1, i + 1)]
        dx, dy = b[0] - a[0], b[1] - a[1]; L = math.hypot(dx, dy) or 1
        w = (w0 + (w1 - w0) * t) / 2
        left.append((x - dy / L * w, y + dx / L * w)); right.append((x + dy / L * w, y - dx / L * w))
    return left + right[::-1]


def draw(phase=2):
    C.clear(); C.update(PALETTES[phase])
    img = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    hx, hy = 63, 36                       # halo centre (behind the head)

    # ---- halo: two thin rings and its spikes, lit at the top left
    for r, c in ((21, "go0"), (17, "go0")):
        for t in range(0, 720):
            a = math.radians(t / 2)
            lit = math.cos(a + math.radians(135)) > 0.3
            px(img, round(hx + r * math.cos(a)), round(hy + r * math.sin(a)), "go1" if lit and r == 21 else c)
    spikes = {1: ((270, 28), (225, 25), (315, 25), (180, 27), (0, 27)),
              2: ((270, 28), (225, 27), (315, 27), (180, 29), (0, 29), (200, 24), (340, 24)),
              3: ((270, 28), (240, 27), (300, 27), (210, 27), (330, 27), (180, 32), (0, 32), (250, 24), (290, 24))}[phase]
    for ang, L in spikes:
        a = math.radians(ang)
        for s in range(17, L):
            px(img, round(hx + s * math.cos(a)), round(hy + s * math.sin(a)), "go1" if s > 21 else "go0")
        tx, ty = round(hx + L * math.cos(a)), round(hy + L * math.sin(a))
        if ang in (270, 180, 0):                                       # four-point stars on the long spikes
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): px(img, tx + dx, ty + dy, "go1")
            px(img, tx, ty, "gl")

    for t in range(0, 360, 12):                                        # dotted inner ring
        a = math.radians(t); px(img, round(hx + 13 * math.cos(a)), round(hy + 13 * math.sin(a)), "go0")
    for t in range(0, 360, 20):                                        # thorns on the outer ring
        a = math.radians(t + 10); px(img, round(hx + 23 * math.cos(a)), round(hy + 23 * math.sin(a)), "go0")
    for ang in (225, 315, 200, 340):                                   # small stars where the short spikes leave the ring
        a = math.radians(ang); sx, sy = round(hx + 21 * math.cos(a)), round(hy + 21 * math.sin(a))
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): px(img, sx + dx, sy + dy, "go1")
        px(img, sx, sy, "gl")

    # ---- shadow wings: one ragged mass of shadow behind her, broad at the shoulders, tearing into flame-like tips
    wing = []
    for side in (-1, 1):
        X = lambda dx: 63 + side * dx
        for c1, c2, tip, w in [((22, 50), (36, 54), (46, 66), 16), ((20, 60), (32, 74), (38, 92), 14), ((14, 70), (22, 90), (26, 108), 12),
                               ((26, 48), (40, 46), (50, 52), 9), ((16, 76), (18, 96), (14, 112), 9)]:
            wing.append(lock_shape([(X(10), 54), (X(c1[0]), c1[1]), (X(c2[0]), c2[1]), (X(tip[0]), tip[1])], w, 1))
    mass(img, wing, "sh1", "sh0", "sh2")

    for side in (-1, 1):
        X = lambda dx: 63 + side * dx
        for c1, c2, tip in [((22, 52), (34, 58), (42, 66)), ((20, 62), (30, 76), (34, 90)), ((14, 72), (20, 90), (22, 104)),
                            ((26, 50), (38, 49), (46, 53)), ((16, 78), (17, 94), (14, 108))]:
            pts = [(x, y) for x, y, t in bez((X(12), 56), (X(c1[0]), c1[1]), (X(c2[0]), c2[1]), (X(tip[0]), tip[1]), 16)]
            line(d, pts[:-3], "sh2")                                   # a lit rib along each tendril
            line(d, [(x + side, y + 1) for x, y in pts[2:-5]], "sh0")  # its shadow side
        for (cx, cy) in ((44, 64), (36, 88), (22, 108), (46, 52), (14, 110)):   # curls at the tips
            for k in range(5):
                a = math.radians(k * 60); px(img, round(X(cx) + side * 2 * math.cos(a)), round(cy + 2 * math.sin(a)), "sh2")

    # ---- hair, back: one silver mass waving out to both sides and down past the hips, strand lines inside
    hair = [[(52, 30), (74, 30), (78, 44), (48, 44)]]
    for side in (-1, 1):
        X = lambda dx: 63 + side * dx
        for c1, c2, tip, w in [((14, 40), (10, 56), (20, 74), 14), ((16, 46), (24, 60), (28, 82), 11), ((8, 50), (14, 70), (12, 88), 10),
                               ((18, 38), (28, 46), (32, 58), 9)]:
            hair.append(lock_shape([(X(5), 32), (X(c1[0]), c1[1]), (X(c2[0]), c2[1]), (X(tip[0]), tip[1])], w, 4))
    mass(img, hair, "ha1", "ha0", "ha2")
    for side in (-1, 1):
        X = lambda dx: 63 + side * dx
        for c1, c2, tip in [((12, 44), (10, 58), (18, 72)), ((15, 50), (21, 62), (24, 78)), ((9, 54), (13, 70), (11, 84))]:
            line(d, [(x, y) for x, y, t in bez((X(7), 40), (X(c1[0]), c1[1]), (X(c2[0]), c2[1]), (X(tip[0]), tip[1]), 12)], "ha0")

    for side in (-1, 1):
        X = lambda dx: 63 + side * dx
        for c1, c2, tip in [((13, 46), (12, 60), (21, 74)), ((17, 46), (24, 58), (27, 76)), ((11, 50), (15, 66), (14, 82)),
                            ((19, 42), (27, 48), (30, 58)), ((14, 48), (18, 62), (19, 72))]:
            line(d, [(x, y) for x, y, t in bez((X(8), 38), (X(c1[0]), c1[1]), (X(c2[0]), c2[1]), (X(tip[0]), tip[1]), 14)][2:], "ha2")
        for k, (x, y) in enumerate([(9, 36), (11, 38), (13, 41), (15, 44)]):   # the sheen band over the crown
            px(img, X(x), y, "ha2")
        for (x, y) in ((30, 84), (31, 86), (32, 88), (16, 90), (15, 92), (35, 62), (37, 63)):   # loose wisps
            px(img, X(x), y, "ha0")
    for (x, y) in ((70, 34), (71, 35), (72, 37), (56, 34)):            # star hair clip
        px(img, x, y, "go1")
    px(img, 71, 36, "gl")

    # ---- long coat, back: dark, open at the front, falling to the floor behind the dress
    mass(img, [[(55, 52), (71, 52), (80, 70), (88, 96), (92, 116), (82, 112), (76, 118), (70, 108), (56, 108), (50, 118),
                (44, 112), (34, 116), (38, 96), (46, 70)]], "sh1", "sh0", "sh2")

    for pts in ([(56, 54), (48, 70), (42, 92), (38, 112)], [(70, 54), (78, 70), (84, 92), (88, 112)]):
        line(d, pts, "go0")                                            # gold trim down the coat's open edges
        for x, y, t in bez(pts[0], pts[1], pts[2], pts[3], 10):
            px(img, round(x), round(y), "go1")
    for pts in ([(52, 70), (46, 96), (44, 114)], [(74, 70), (80, 96), (82, 114)]):
        line(d, pts, "pu1")                                            # the purple lining showing in the folds
    for pts in ([(48, 80), (42, 104)], [(78, 80), (84, 104)]):
        line(d, pts, "sh0")

    # ---- dress: white, from a narrow waist flaring to a ruffled lavender hem; folds falling from the waist
    mass(img, [[(59, 64), (67, 64), (72, 78), (78, 96), (80, 106), (74, 104), (70, 108), (64, 105), (58, 108), (52, 104),
                (46, 106), (48, 96), (54, 78)]], "wh1", "wh0")
    for pts in ([(61, 66), (58, 84), (54, 102)], [(65, 66), (67, 84), (70, 102)], [(63, 70), (63, 104)]):
        line(d, pts, "wh0")
    for x0, x1, y in ((47, 80, 105),):
        for x in range(x0, x1): px(img, x, y + (1 if x % 3 == 0 else 0), "pu2")
    for x in range(47, 81, 3): px(img, x, 107, "pu1")

    for x in range(53, 76, 5):                                          # pleats
        line(d, [(63 + (x - 63) * 0.4, 70), (x, 100)], "wh0")
    for x in range(49, 79):                                            # an upper ruffle tier
        px(img, x, 92 + (x % 4 == 0), "wh0")
    for (x, y) in ((56, 80), (69, 84), (61, 96), (73, 98), (52, 94), (66, 76)):   # gold star embroidery
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): px(img, x + dx, y + dy, "go0")
        px(img, x, y, "go1")
    for x, y, t in bez((55, 70), (60, 76), (68, 76), (73, 70), 20):    # a gold chain across the hips
        px(img, round(x), round(y), "go1" if int(t * 20) % 2 else "go0")

    # ---- legs and boots: slender dark stockings, pointed boots with a gold trim
    for x0 in (59, 65):
        for y in range(107, 119):
            px(img, x0, y, "ink"); px(img, x0 + 1, y, "sh2" if x0 == 65 else "sh1"); px(img, x0 + 2, y, "ink")
    for (x0, toe) in ((58, -1), (64, 1)):
        for y in range(119, 125):
            for x in range(x0, x0 + 5): px(img, x, y, "sh0")
        for x in range(x0, x0 + 5): px(img, x, 125, "ink")
        px(img, x0 + (5 if toe > 0 else -1), 124, "ink")
        for x in range(x0, x0 + 5): px(img, x, 119, "go0")

    # ---- bodice: fitted and dark, tapering to the waist; a gold cross, collar and waistband
    mass(img, [[(56, 52), (70, 52), (69, 58), (67, 65), (59, 65), (57, 58)]], "pu1", "sh2", "pu2")
    line(d, [(63, 54), (63, 62)], "go1")
    line(d, [(61, 56), (65, 56)], "go1")
    line(d, [(59, 64), (67, 64)], "go0")
    line(d, [(58, 52), (68, 52)], "go0")
    px(img, 63, 52, "go1")

    for y in range(57, 64, 2):                                          # lacing
        px(img, 62, y, "go0"); px(img, 64, y, "go0")
    line(d, [(57, 55), (60, 58), (63, 57), (66, 58), (69, 55)], "go0")  # filigree along the bust line
    for x in (57, 69):                                                 # shoulder gems
        px(img, x, 53, "pu2"); px(img, x, 54, "go1")

    # ---- sleeves: white, detached, flaring at the wrist, gold cuffs
    mass(img, [[(56, 52), (52, 56), (49, 64), (46, 72), (50, 74), (52, 66), (56, 58)]], "wh1", "wh0")
    mass(img, [[(70, 52), (75, 56), (78, 64), (80, 70), (76, 72), (74, 64), (70, 58)]], "wh1", "wh0")
    line(d, [(46, 72), (50, 74)], "go0"); line(d, [(76, 72), (80, 70)], "go0")
    line(d, [(53, 58), (49, 68)], "wh0"); line(d, [(73, 58), (77, 66)], "wh0")

    # ---- the sword, held low in the sword hand, blade down and out (phase 1 black, 2 glowing gold, 3 bright steel,
    # in her other hand). Drawn before the hand, so the fist closes over the grip.
    side = -1 if phase == 3 else 1
    hand = (77, 73) if side > 0 else (47, 74)
    gx, gy = hand[0] + side * 1, hand[1] + 3
    tx, ty = (gx + side * 29, 117)
    blade = {1: ("ink", "sh2", "pu2", "gl"), 2: ("go0", "go1", "gl", "gl"), 3: ("pu1", "sh2", "gl", "gl")}[phase]
    L = math.hypot(tx - gx, ty - gy); ux, uy = (tx - gx) / L, (ty - gy) / L; nx, ny = -uy, ux
    for s in [i * 0.5 for i in range(int(L * 2))]:
        x, y = gx + ux * s, gy + uy * s
        px(img, round(x), round(y), blade[2])                         # the core
        if s < L * 0.85:
            px(img, round(x + nx), round(y + ny), blade[1])           # the lit edge
            px(img, round(x - nx), round(y - ny), blade[0])           # the shade edge
    for s_ in range(4, int(L * 0.8), 6):                               # light running down the fuller
        px(img, round(gx + ux * s_), round(gy + uy * s_), blade[3])
    for k in range(-3, 4):                                             # the guard
        px(img, round(gx + nx * k), round(gy + ny * k), "go1" if abs(k) < 3 else "go0")
    for s_ in range(1, 6): px(img, round(gx - ux * s_), round(gy - uy * s_), "sh0")   # grip, up into the fist
    px(img, round(gx - ux * 6), round(gy - uy * 6), "go1")                         # pommel above the fist
    if phase == 2:                                                     # light gathering on the blade
        for (x, y) in ((96, 98), (101, 106), (90, 92)):
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): px(img, x + dx, y + dy, "go1")
            px(img, x, y, "gl")

    # ---- hands: a fist round the grip (knuckles lit), the other hand open at her side
    for (hx_, hy_), fist in (((77, 73), side > 0), ((47, 74), side < 0)):
        poly(d, [(hx_ - 2, hy_ - 1), (hx_ + 2, hy_ - 1), (hx_ + 2, hy_ + 3), (hx_ - 2, hy_ + 3)], "sk1", "sk0")
        if fist:
            line(d, [(hx_ - 1, hy_ + 1), (hx_ + 1, hy_ + 1)], "sk0")  # fingers round the grip
        else:
            px(img, hx_ - 1, hy_ + 4, "sk0"); px(img, hx_ + 1, hy_ + 4, "sk0")   # fingertips

    # ---- neck and face
    poly(d, [(60, 46), (66, 46), (66, 52), (60, 52)], "sk0")
    poly(d, [(56, 34), (70, 34), (70, 42), (67, 46), (63, 48), (59, 46), (56, 42)], "sk1", "ink")
    eyes = [(58, 39, "ink"), (59, 39, "ink"), (60, 39, "ink"), (66, 39, "ink"), (67, 39, "ink"), (68, 39, "ink")]
    if phase == 1:   # veiled below the eyes; the eyes two cold points of light
        poly(d, [(56, 42), (70, 42), (68, 46), (63, 49), (58, 46)], "sh1", "ink")
        line(d, [(57, 43), (69, 43)], "sh2")
        eyes += [(59, 40, "sh2"), (60, 40, "gl"), (66, 40, "sh2"), (67, 40, "gl"), (59, 41, "pu2"), (67, 41, "pu2")]
    else:
        eyes += [(59, 40, "pu1"), (60, 40, "sh2"), (66, 40, "pu1"), (67, 40, "sh2"),     # irises, dark at the top
                 (59, 41, "pu2"), (60, 41, "pu1"), (66, 41, "pu2"), (67, 41, "pu1"),
                 (58, 40, "gl"), (65, 40, "gl"),                                          # catch-lights
                 (57, 43, "sk0"), (69, 43, "sk0"), (63, 43, "sk0")]                       # blush, the nose's shade
        eyes += [(62, 45, "sk0"), (63, 45, "sk0"), (64, 45, "sk0")] if phase == 3 else [(63, 45, "sk0")]   # a smile
        if phase == 3:
            eyes += [(60, 42, "pu2"), (67, 42, "pu2")]                                    # eyes fully open
    for x, y, c in eyes:
        px(img, x, y, c)

    # ---- hair, front: crown and bangs over the brow, two locks framing the face
    poly(d, [(55, 30), (60, 27), (66, 27), (71, 30), (73, 36), (70, 38), (68, 34), (65, 37), (62, 34), (59, 37), (57, 34),
             (55, 38), (53, 36)], "ha2", "ink")
    poly(d, [(55, 34), (53, 40), (54, 50), (56, 52), (57, 42)], "ha1", "ink")
    poly(d, [(71, 34), (73, 40), (72, 50), (70, 52), (69, 42)], "ha1", "ink")
    line(d, [(60, 29), (58, 33)], "ha1")
    line(d, [(66, 29), (68, 33)], "ha1")
    return img


def design():
    """{phase: (index canvas, palette)} in the form make.py animates."""
    import numpy as np
    out = {}
    for p in (1, 2, 3):
        a = np.asarray(draw(p))
        m = a[..., 3] > 0
        cols, inv = np.unique(a[m][:, :3], axis=0, return_inverse=True)
        ix = np.full(m.shape, -1, np.int16); ix[m] = inv.ravel()
        out[p] = (ix, cols.astype(np.uint8))
    return out


if __name__ == "__main__":
    out = os.path.join(ROOT, "records/sprites")
    os.makedirs(out, exist_ok=True)
    row = Image.new("RGBA", (3 * (N + 8), N), (58, 54, 74, 255))
    for i, p in enumerate((1, 2, 3)):
        row.alpha_composite(draw(p), (i * (N + 8), 0))
    row.save(os.path.join(out, "serena-hand.png"))
    row.convert("RGB").resize((row.width * 4, N * 4), Image.NEAREST).save(os.path.join(out, "serena-hand-4x.png"))
