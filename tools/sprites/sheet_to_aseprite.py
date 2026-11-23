"""Split Serena's sprite sheet (art/bosses/serena-sheet: the art on top, the in-game sprites below) into .aseprite files.

    python3 tools/sprites/sheet_cut.py --model isnet-anime.onnx    # once: the sheet without its checkerboard
    python3 tools/sprites/sheet_to_aseprite.py [--out records/aseprite/serena-sheet]

Each group of the sheet becomes one file, its pieces left to right as frames on one canvas (bottom-centre aligned, the
feet on the same line), with two layers:
  - "sprite": the cut-out piece, to edit
  - "original" (hidden): the same area of the sheet as delivered, checkerboard and all, to check the cut against
An "origin" slice marks the bottom centre (the pivot). Frames keep the sheet's own resolution; durations per kind (DURATIONS).
"""
import argparse, importlib.util, json, os, struct
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
spec = importlib.util.spec_from_file_location("toase", os.path.join(ROOT, "tools/pixelfx/to-aseprite.py"))
A = importlib.util.module_from_spec(spec); spec.loader.exec_module(A)

# (name, box x0, y0, x1, y1) in sheet pixels; read off the sheet's layout
GROUPS = [
    ("serena-p1-art", 18, 0, 538, 448), ("serena-p2-art", 562, 0, 996, 448), ("serena-p3-art", 1030, 0, 1504, 448),
    ("serena-p1-idle", 14, 450, 514, 586), ("serena-p2-idle", 544, 450, 1002, 586), ("serena-p3-idle", 1036, 450, 1532, 586),
    ("serena-p1-attack", 4, 588, 524, 724), ("serena-p2-attack", 536, 596, 1028, 738), ("serena-p3-attack", 1028, 590, 1532, 724),
    ("serena-p1-skill", 4, 736, 490, 888), ("serena-p2-skill", 524, 736, 1030, 892), ("serena-p3-skill", 1050, 722, 1532, 904),
    ("serena-p1-down", 18, 908, 474, 1018), ("serena-fx-crystals", 528, 916, 694, 1004), ("serena-mini", 694, 916, 772, 1004), ("serena-fx-shadow", 772, 906, 1532, 1012),
]


def cel(layer, x, y, w, h, rgba):
    head = struct.pack("<HhhBHh", layer, x, y, 255, 2, 0) + bytes(5) + struct.pack("<HH", w, h)
    import zlib
    return A.chunk(0x2005, head + zlib.compress(rgba, 9))


def layer(name, visible):
    flags = (1 if visible else 0) | 2
    return A.chunk(0x2004, struct.pack("<HHHHHHB", flags, 0, 0, 0, 0, 0, 255) + bytes(3) + A.string(name))


def pieces(alpha, x0, x1, gap=3):
    """Split a group into pieces at empty column runs at least `gap` wide."""
    cols = alpha.sum(0) > 3                                            # a few stray pixels do not join two frames
    out, start, empty = [], None, 0
    for x in range(alpha.shape[1]):
        if cols[x]:
            if start is None: start = x
            empty = 0
        elif start is not None:
            empty += 1
            if empty >= gap:
                out.append((start, x - empty + 1)); start = None; empty = 0
    if start is not None: out.append((start, alpha.shape[1]))
    return [(a, b) for a, b in out if alpha[:, a:b].sum() > 40]


# frames in each character animation, counted on the sheet (their hair and cloth overlap, so empty columns are not enough)
COUNTS = {"serena-p1-idle": 5, "serena-p2-idle": 4, "serena-p3-idle": 5,
          "serena-p1-attack": 5, "serena-p2-attack": 4, "serena-p3-attack": 4}


def split_count(alpha, n):
    """n pieces: cut at the n-1 columns with the fewest pixels, each cut near where an even split would put it."""
    dens = alpha.sum(0).astype(float)
    xs = np.nonzero(dens > 3)[0]; a, b = xs.min(), xs.max() + 1
    cuts = []
    for k in range(1, n):
        c = a + (b - a) * k / n
        lo, hi = int(c - (b - a) / n * 0.4), int(c + (b - a) / n * 0.4)
        cuts.append(lo + int(np.argmin(dens[lo:hi])))
    edges = [a] + cuts + [b]
    return [(edges[i], edges[i + 1]) for i in range(n)]


def split_bodies(rgb, alpha, n):
    """n frames whose hair and cloth overlap. Each body's centre is the middle of the thickest part of its slot (the
    slots are cut at the sparsest columns); a separate piece (a halo, a strand, a spark) goes whole to the body whose
    centre is nearest; a piece joining two bodies is split on the line halfway between their centres.
    Returns n masks, left to right."""
    import cv2
    m = alpha.astype(np.uint8)
    dist = cv2.distanceTransform(m, cv2.DIST_L2, 5)
    centres = []
    for a_, b_ in split_count(alpha, n):
        d = np.zeros_like(dist); d[:, a_:b_] = dist[:, a_:b_]
        ys, xs = np.nonzero(d >= d.max() * 0.5)
        centres.append(float(xs.mean()))
    mids = [(centres[k] + centres[k + 1]) / 2 for k in range(n - 1)]
    X = np.arange(alpha.shape[1])[None, :].repeat(alpha.shape[0], 0)
    by_column = np.searchsorted(mids, X)                               # which body each column belongs to
    out = [np.zeros_like(alpha) for _ in range(n)]
    cnt, lab, st, cen = cv2.connectedComponentsWithStats(m, 8)
    for i in range(1, cnt):
        piece = lab == i
        cols = np.unique(by_column[piece])
        if len(cols) == 1 or st[i, 4] < 400:                           # whole, to the nearest body
            k = int(np.argmin([abs(cen[i][0] - c) for c in centres])); out[k] |= piece
        else:                                                          # a piece joining bodies: split along its own
            markers = np.zeros(alpha.shape, np.int32)                  # gaps and edges, seeded from each body's core
            for k in cols:
                core = piece & (by_column == k) & (dist >= max(2.0, dist[piece & (by_column == k)].max() * 0.5))
                if not core.any(): core = piece & (by_column == k) & (np.abs(X - centres[k]) < 2)
                markers[core] = int(k) + 1
            ws = cv2.watershed(np.ascontiguousarray(rgb[..., :3]), markers)
            bounds = [-1e9] + mids + [1e9]
            for k in cols:
                lo, hi = bounds[k], bounds[k + 1]
                pad = (centres[min(k + 1, n - 1)] - centres[max(k - 1, 0)]) / 6
                near = (X >= lo - pad) & (X <= hi + pad)               # a body reaches a little past its own line, no more
                out[k] |= piece & near & ((ws == int(k) + 1) | ((ws == -1) & (by_column == k)))
            rest = piece & ~np.any([o for o in out], axis=0)
            for k in cols: out[k] |= rest & (by_column == k)
    return out


def split_clusters(alpha, gap=3):
    """Effect groups: each cluster of pieces closer than `gap` px is one frame, left to right."""
    import cv2
    near = cv2.dilate(alpha.astype(np.uint8), np.ones((gap, gap), np.uint8))
    cnt, lab, st, cen = cv2.connectedComponentsWithStats(near, 8)
    size = {i: int((alpha & (lab == i)).sum()) for i in range(1, cnt)}
    top = max(size.values()) if size else 0
    big = [i for i in size if size[i] >= max(150, 0.08 * top)]       # a frame of its own only if it is a real effect
    groups = {i: alpha & (lab == i) for i in big}
    for i in size:                                                     # a lone spark joins the nearest real piece
        if i in big or size[i] < 6: continue
        j = min(big, key=lambda b: (cen[b][0] - cen[i][0]) ** 2 + (cen[b][1] - cen[i][1]) ** 2)
        groups[j] |= alpha & (lab == i)
    return [groups[i] for i in sorted(big, key=lambda i: st[i, 0])]


def family(name):
    if name == "serena-mini": return "p2"                              # the small Serena: a character, not an effect
    return name.split("-")[1] if name.split("-")[1] in ("p1", "p2", "p3") else "fx"


MERGE = 34   # colours closer than this in Lab (OpenCV's 0-255 scale, about 13 dE) become one


def merge_similar(cen, limit=MERGE):
    """Colours that read as the same are one colour: repeatedly join the closest pair (their middle) until no two are
    closer than `limit`."""
    cen = [c.astype(np.float32) for c in cen]
    while len(cen) > 2:
        warm = [c[2] - 128 > 6 for c in cen]                          # the halo's gold never merges into the lavenders
        d = np.array([[np.linalg.norm(a - b) if i != j and warm[i] == warm[j] else 1e9 for j, b in enumerate(cen)]
                      for i, a in enumerate(cen)])
        i, j = np.unravel_index(np.argmin(d), d.shape)
        if d[i, j] >= limit: break
        cen[i] = (cen[i] + cen[j]) / 2; cen.pop(j)
    return np.array(cen, np.float32)


def make_palette(pixels, n=16):
    """n colours for a family, picked in Lab (perceptual), dark to light. Each distinct colour (binned) counts once,
    not by area, so small accents get a colour of their own; the halo's gold, if the family has it, is kept as two
    colours (its light and its shade) instead of being averaged into the pale lavender."""
    import cv2
    p = pixels.astype(np.int16)
    warm = (p[:, 0] > p[:, 2] + 12) & (p[:, 0] > p[:, 1] + 4) & (p.mean(1) > 120)
    accents = []
    if warm.mean() > 0.002:
        w = p[warm]; Lw = w.mean(1)
        accents = [w[Lw >= np.median(Lw)].mean(0), w[Lw < np.median(Lw)].mean(0)]
    binned = np.unique((p[~warm] // 8), axis=0) * 8 + 4
    lab = cv2.cvtColor(binned[None].astype(np.uint8), cv2.COLOR_RGB2LAB)[0].astype(np.float32)
    crit = (cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 80, 0.2)
    _, _, cen = cv2.kmeans(lab, n - len(accents), None, crit, 6, cv2.KMEANS_PP_CENTERS)
    if accents:
        acc = cv2.cvtColor(np.array([accents]).clip(0, 255).astype(np.uint8), cv2.COLOR_RGB2LAB)[0].astype(np.float32)
        cen = np.concatenate([cen, acc])
    cen = merge_similar(cen)                                          # similar colours become one (warm with warm only)
    cen = cen[np.argsort(cen[:, 0])]
    return cen, cv2.cvtColor(cen[None].clip(0, 255).astype(np.uint8), cv2.COLOR_LAB2RGB)[0]


def paint(rgba, cen, pal):
    """Repaint a cut-out at the sheet's own resolution in the family's palette. Shrinking it (it looks 2x but is not on
    a clean grid) broke thin lines, eyes and the sword, so nothing is resampled. Each pixel takes its nearest palette
    colour; then a cleanup removes only compression speckle: a pixel whose colour no 4-neighbour shares takes the
    colour of the neighbours if they all agree and are close in lightness. A line, an eye or a glint differs strongly
    from what is around it, or continues in a neighbour, so it is never touched."""
    import cv2
    m = rgba[..., 3] > 0
    lab = cv2.cvtColor(np.ascontiguousarray(rgba[..., :3]), cv2.COLOR_RGB2LAB).astype(np.float32)
    idx = np.argmin(((lab.reshape(-1, 3)[:, None] - cen[None]) ** 2).sum(-1), 1).reshape(m.shape)
    idx = np.where(m, idx, -1).astype(np.int16)
    L = cen[:, 0]
    h, w = idx.shape
    for _ in range(2):
        src = idx.copy()
        for y in range(1, h - 1):
            for x in range(1, w - 1):
                c = src[y, x]
                if c < 0: continue
                four = np.array([src[y - 1, x], src[y + 1, x], src[y, x - 1], src[y, x + 1]])
                if (four == c).any() or (four < 0).any(): continue     # continues somewhere, or on the edge: keep
                diag = np.array([src[y - 1, x - 1], src[y - 1, x + 1], src[y + 1, x - 1], src[y + 1, x + 1]])
                if (diag == c).any(): continue                         # a diagonal line: keep
                v, k = np.unique(four, return_counts=True)
                t = v[np.argmax(k)]
                if k.max() >= 3 and abs(L[c] - L[t]) < 22: idx[y, x] = t
    out = np.zeros((h, w, 4), np.uint8)
    mm = idx >= 0
    out[mm, :3] = pal[idx[mm]]; out[mm, 3] = 255
    return out


DURATIONS = {"idle": 120, "attack": 90, "skill": 80, "down": 150, "crystals": 80, "shadow": 80, "mini": 120, "art": 100}


def write(path, frames_cut, frames_orig, pal, duration=100):
    h = max(f.shape[0] for f in frames_cut); w = max(f.shape[1] for f in frames_cut)
    W, H = w + 4, h + 4
    colors = [(int(r), int(g), int(b), 255) for r, g, b in pal]
    out = []
    for i, (fc, fo) in enumerate(zip(frames_cut, frames_orig)):
        ox, oy = (W - fc.shape[1]) // 2, H - 2 - fc.shape[0]           # bottom-centre aligned
        chunks = []
        if i == 0:
            chunks += [A.color_profile(), A.palette(colors), layer("sprite", True), layer("original", False)]
        for li, img in ((0, fc), (1, fo)):
            ys, xs = np.nonzero(img[..., 3] > 0)
            if not len(ys): continue
            c = np.ascontiguousarray(img[ys.min(): ys.max() + 1, xs.min(): xs.max() + 1])
            chunks.append(cel(li, ox + xs.min(), oy + ys.min(), c.shape[1], c.shape[0], c.tobytes()))
        if i == 0:
            name = os.path.splitext(os.path.basename(path))[0]
            chunks.append(A.tags([(0, len(frames_cut) - 1, 0, name)]))
            chunks.append(A.slice_with_pivot("origin", W, H, W // 2, H - 2))
        out.append(A.frame(duration, chunks))
    body = b"".join(out)
    with open(path, "wb") as fh:
        fh.write(A.header(128 + len(body), len(frames_cut), W, H, len(colors)) + body)
    return len(frames_cut), W, H


def main():
    ap = argparse.ArgumentParser(); ap.add_argument("--out", default=os.path.join(ROOT, "records/aseprite/serena-sheet"))
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    cut = np.asarray(Image.open(os.path.join(ROOT, "art/bosses/serena-sheet/cut.png")).convert("RGBA"))
    orig = np.asarray(Image.open(os.path.join(ROOT, "art/bosses/serena-sheet/sheet.webp")).convert("RGBA"))
    index = []
    fam_px = {}
    for name, x0, y0, x1, y1 in GROUPS:
        g = cut[y0:y1, x0:x1]
        fam_px.setdefault(family(name), []).append(g[g[..., 3] > 0][:, :3])
    pals = {f: make_palette(np.concatenate(v)) for f, v in fam_px.items()}
    for name, x0, y0, x1, y1 in GROUPS:
        g = cut[y0:y1, x0:x1]
        al = g[..., 3] > 0
        if name.endswith("-art"): masks = [al]
        elif name in COUNTS: masks = split_bodies(g, al, COUNTS[name])
        else: masks = split_clusters(al)
        o = orig[y0:y1, x0:x1]
        fc, fo = [], []
        for mk in masks:
            ys, xs = np.nonzero(mk)
            t, b_, l, r = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
            piece = g[t:b_, l:r].copy(); piece[..., 3] = np.where(mk[t:b_, l:r], 255, 0)
            # crop on the 2x2 grid so the blocks are the sheet's own pixels
            piece = g[t:b_, l:r].copy(); piece[..., 3] = np.where(mk[t:b_, l:r], 255, 0)
            fc.append(np.ascontiguousarray(paint(piece, *pals[family(name)])))
            fo.append(np.ascontiguousarray(o[t:b_, l:r]))
        fc = [f for f in fc if (f[..., 3] > 0).any()]
        dur = DURATIONS[name.split("-")[-1]]
        n, W, H = write(os.path.join(a.out, name + ".aseprite"), fc, fo[:len(fc)], pals[family(name)][1], dur)
        index.append({"file": name + ".aseprite", "frames": n, "size": [W, H], "sheet_box": [x0, y0, x1, y1]})
        print(f"{name:22s} {n:2d} frames  {W}x{H}")
    json.dump(index, open(os.path.join(a.out, "index.json"), "w"), indent=1)


if __name__ == "__main__":
    main()
