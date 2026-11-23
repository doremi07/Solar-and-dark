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
C = {  # phase 2 palette (15)
    "ink": "#120b22",
    "sh0": "#1d1534", "sh1": "#2c2150", "sh2": "#43357a",          # shadow cloth
    "pu1": "#5e4b9c", "pu2": "#8676c4",                               # purple
    "ha0": "#8f83c2", "ha1": "#b9aee6", "ha2": "#e2dcf8",            # hair
    "wh0": "#b7add8", "wh1": "#ece7fb",                               # skirt
    "sk0": "#d8a7b2", "sk1": "#f6dcd8",                               # skin
    "go0": "#b8873f", "go1": "#f4dc98", "gl": "#fff7e2",              # gold, glow
}


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


def draw():
    img = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    hx, hy = 63, 36                       # halo centre (behind the head)

    # ---- halo: two thin rings and its spikes, lit at the top left
    for r, c in ((21, "go0"), (17, "go0")):
        for t in range(0, 720):
            a = math.radians(t / 2)
            lit = math.cos(a + math.radians(135)) > 0.3
            px(img, round(hx + r * math.cos(a)), round(hy + r * math.sin(a)), "go1" if lit and r == 21 else c)
    for ang, L in ((270, 30), (225, 27), (315, 27), (180, 29), (0, 29), (200, 24), (340, 24)):
        a = math.radians(ang)
        for s in range(17, L):
            px(img, round(hx + s * math.cos(a)), round(hy + s * math.sin(a)), "go1" if s > 21 else "go0")
        tx, ty = round(hx + L * math.cos(a)), round(hy + L * math.sin(a))
        if ang in (270, 180, 0):                                       # four-point stars on the long spikes
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)): px(img, tx + dx, ty + dy, "go1")
            px(img, tx, ty, "gl")

    # ---- shadow wings: one ragged mass of shadow behind her, broad at the shoulders, tearing into flame-like tips
    wing = []
    for side in (-1, 1):
        X = lambda dx: 63 + side * dx
        for c1, c2, tip, w in [((22, 50), (36, 54), (46, 66), 16), ((20, 60), (32, 74), (38, 92), 14), ((14, 70), (22, 90), (26, 108), 12),
                               ((26, 48), (40, 46), (50, 52), 9), ((16, 76), (18, 96), (14, 112), 9)]:
            wing.append(lock_shape([(X(10), 54), (X(c1[0]), c1[1]), (X(c2[0]), c2[1]), (X(tip[0]), tip[1])], w, 1))
    mass(img, wing, "sh1", "sh0", "sh2")

    # ---- hair, back: one silver mass waving out to both sides and down past the hips, strand lines inside
    hair = [[(52, 30), (74, 30), (78, 44), (48, 44)]]
    for side in (-1, 1):
        X = lambda dx: 63 + side * dx
        for c1, c2, tip, w in [((14, 40), (10, 56), (20, 74), 14), ((16, 46), (24, 60), (28, 82), 11), ((8, 50), (14, 70), (12, 88), 10),
                               ((18, 38), (28, 46), (32, 58), 9)]:
            hair.append(lock_shape([(X(5), 32), (X(c1[0]), c1[1]), (X(c2[0]), c2[1]), (X(tip[0]), tip[1])], w, 2))
    mass(img, hair, "ha1", "ha0", "ha2")
    for side in (-1, 1):
        X = lambda dx: 63 + side * dx
        for c1, c2, tip in [((12, 44), (10, 58), (18, 72)), ((15, 50), (21, 62), (24, 78)), ((9, 54), (13, 70), (11, 84))]:
            line(d, [(x, y) for x, y, t in bez((X(7), 40), (X(c1[0]), c1[1]), (X(c2[0]), c2[1]), (X(tip[0]), tip[1]), 12)], "ha0")

    # ---- long coat, back: dark, open at the front, falling to the floor behind the dress
    mass(img, [[(55, 52), (71, 52), (80, 70), (88, 96), (92, 116), (82, 112), (76, 118), (70, 108), (56, 108), (50, 118),
                (44, 112), (34, 116), (38, 96), (46, 70)]], "sh1", "sh0", "sh2")

    # ---- dress: white, from a narrow waist flaring to a ruffled lavender hem; folds falling from the waist
    mass(img, [[(59, 64), (67, 64), (72, 78), (78, 96), (80, 106), (74, 104), (70, 108), (64, 105), (58, 108), (52, 104),
                (46, 106), (48, 96), (54, 78)]], "wh1", "wh0")
    for pts in ([(61, 66), (58, 84), (54, 102)], [(65, 66), (67, 84), (70, 102)], [(63, 70), (63, 104)]):
        line(d, pts, "wh0")
    for x0, x1, y in ((47, 80, 105),):
        for x in range(x0, x1): px(img, x, y + (1 if x % 3 == 0 else 0), "pu2")
    for x in range(47, 81, 3): px(img, x, 107, "pu1")

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

    # ---- sleeves and hands: white detached sleeves flaring at the wrist; left hand at her side, right on the grip
    mass(img, [[(56, 52), (52, 56), (49, 64), (46, 72), (50, 74), (52, 66), (56, 58)]], "wh1", "wh0")
    mass(img, [[(70, 52), (75, 56), (78, 64), (80, 70), (76, 72), (74, 64), (70, 58)]], "wh1", "wh0")
    poly(d, [(46, 73), (49, 73), (49, 77), (46, 76)], "sk1", "sk0")
    poly(d, [(75, 71), (78, 71), (78, 75), (75, 75)], "sk1", "sk0")

    # ---- the sword: held low, the blade running down to the right, glowing
    gx, gy, tx, ty = 77, 73, 106, 116
    L = math.hypot(tx - gx, ty - gy); ux, uy = (tx - gx) / L, (ty - gy) / L; nx, ny = -uy, ux
    for s in [i * 0.5 for i in range(int(L * 2))]:
        x, y = gx + ux * s, gy + uy * s
        w = 1 if s < L * 0.85 else 0
        px(img, round(x), round(y), "gl")
        if w:
            px(img, round(x + nx), round(y + ny), "go1")
            px(img, round(x - nx), round(y - ny), "go0")
    for k in range(-3, 4):                                             # the guard
        px(img, round(gx + nx * k), round(gy + ny * k), "go1" if abs(k) < 3 else "go0")
    for s in range(1, 5): px(img, round(gx - ux * s), round(gy - uy * s), "sh0")   # grip
    px(img, round(gx - ux * 5), round(gy - uy * 5), "go1")                       # pommel

    # ---- neck and face
    poly(d, [(60, 46), (66, 46), (66, 52), (60, 52)], "sk0")
    poly(d, [(56, 34), (70, 34), (70, 42), (67, 46), (63, 48), (59, 46), (56, 42)], "sk1", "ink")
    for x, y, c in [  # eyes: lashes, two-tone irises, catch-lights; a small mouth; blush
        (58, 39, "ink"), (59, 39, "ink"), (60, 39, "ink"), (66, 39, "ink"), (67, 39, "ink"), (68, 39, "ink"),
        (59, 40, "pu1"), (60, 40, "sh2"), (66, 40, "pu1"), (67, 40, "sh2"),
        (59, 41, "pu2"), (60, 41, "pu1"), (66, 41, "pu2"), (67, 41, "pu1"),
        (58, 40, "gl"), (65, 40, "gl"),
        (63, 45, "sk0"), (57, 43, "sk0"), (69, 43, "sk0")]:
        px(img, x, y, c)

    # ---- hair, front: crown and bangs over the brow, two locks framing the face
    poly(d, [(55, 30), (60, 27), (66, 27), (71, 30), (73, 36), (70, 38), (68, 34), (65, 37), (62, 34), (59, 37), (57, 34),
             (55, 38), (53, 36)], "ha2", "ink")
    poly(d, [(55, 34), (53, 40), (54, 50), (56, 52), (57, 42)], "ha1", "ink")
    poly(d, [(71, 34), (73, 40), (72, 50), (70, 52), (69, 42)], "ha1", "ink")
    line(d, [(60, 29), (58, 33)], "ha1")
    line(d, [(66, 29), (68, 33)], "ha1")
    return img


if __name__ == "__main__":
    img = draw()
    out = os.path.join(ROOT, "records/sprites")
    os.makedirs(out, exist_ok=True)
    img.save(os.path.join(out, "serena-p2-hand.png"))
    bg = Image.new("RGBA", img.size, (58, 54, 74, 255)); bg.alpha_composite(img)
    bg.convert("RGB").resize((N * 4, N * 4), Image.NEAREST).save(os.path.join(out, "serena-p2-hand-4x.png"))
