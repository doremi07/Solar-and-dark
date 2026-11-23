"""Split Serena's sprite sheet (art/bosses/serena-sheet: the art on top, the in-game sprites below) into .aseprite files.

    python3 tools/sprites/sheet_cut.py --model isnet-anime.onnx    # once: the sheet without its checkerboard
    python3 tools/sprites/sheet_to_aseprite.py [--out records/aseprite/serena-sheet]

Each group of the sheet becomes one file, its pieces left to right as frames on one canvas (bottom-centre aligned, the
feet on the same line), with two layers:
  - "sprite": the cut-out piece, to edit
  - "original" (hidden): the same area of the sheet as delivered, checkerboard and all, to check the cut against
An "origin" slice marks the bottom centre (the pivot). Frames are 100 ms; retime them in Aseprite.
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
    ("serena-p1-down", 18, 908, 474, 1018), ("serena-fx-crystals", 528, 916, 772, 1004), ("serena-fx-shadow", 772, 906, 1532, 1012),
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
        else:                                                          # a piece joining bodies: split halfway
            for k in cols: out[k] |= piece & (by_column == k)
    return out


def split_clusters(alpha, gap=3):
    """Effect groups: each cluster of pieces closer than `gap` px is one frame, left to right."""
    import cv2
    near = cv2.dilate(alpha.astype(np.uint8), np.ones((gap, gap), np.uint8))
    cnt, lab, st, cen = cv2.connectedComponentsWithStats(near, 8)
    keep = [i for i in range(1, cnt) if (alpha & (lab == i)).sum() > 40]
    return [alpha & (lab == i) for i in sorted(keep, key=lambda i: st[i, 0])]


def family(name):
    return name.split("-")[1] if name.split("-")[1] in ("p1", "p2", "p3") else "fx"


def make_palette(pixels, n=16):
    """n colours for a family, picked in Lab (perceptual), dark to light. Each distinct colour (binned) counts once,
    not by area, so small accents (the gold halo, the pink of a cheek, a glowing edge) get a colour of their own."""
    import cv2
    binned = np.unique((pixels // 8).astype(np.int16), axis=0) * 8 + 4
    lab = cv2.cvtColor(binned[None].astype(np.uint8), cv2.COLOR_RGB2LAB)[0].astype(np.float32)
    crit = (cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 80, 0.2)
    _, _, cen = cv2.kmeans(lab, n, None, crit, 6, cv2.KMEANS_PP_CENTERS)
    cen = cen[np.argsort(cen[:, 0])]
    return cen, cv2.cvtColor(cen[None].clip(0, 255).astype(np.uint8), cv2.COLOR_LAB2RGB)[0]


def paint(rgba, cen, pal):
    """Repaint a cut-out with the family's palette only, then a cleanup: a pixel none of its 8 neighbours shares takes
    the colour most of them agree on, unless it stands out (a line, an eye, a glint)."""
    import cv2
    m = rgba[..., 3] > 0
    lab = cv2.cvtColor(np.ascontiguousarray(rgba[..., :3]), cv2.COLOR_RGB2LAB).astype(np.float32)
    idx = np.argmin(((lab.reshape(-1, 3)[:, None] - cen[None]) ** 2).sum(-1), 1).reshape(m.shape)
    idx = np.where(m, idx, -1)
    L = cen[:, 0]
    h, w = idx.shape
    for _ in range(2):
        src = idx.copy()
        for y in range(1, h - 1):
            for x in range(1, w - 1):
                c = src[y, x]
                if c < 0: continue
                nb = np.delete(src[y - 1:y + 2, x - 1:x + 2].ravel(), 4); nb = nb[nb >= 0]
                if not len(nb) or (nb == c).sum() >= 1: continue
                v, k = np.unique(nb, return_counts=True)
                t = v[np.argmax(k)]
                if k.max() >= 4 and abs(L[c] - L[t]) < 60: idx[y, x] = t
    out = np.zeros_like(rgba)
    out[m, :3] = pal[idx[m]]; out[m, 3] = 255
    return out


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
            fc.append(np.ascontiguousarray(paint(piece, *pals[family(name)])))
            fo.append(np.ascontiguousarray(o[t:b_, l:r]))
        n, W, H = write(os.path.join(a.out, name + ".aseprite"), fc, fo, pals[family(name)][1])
        index.append({"file": name + ".aseprite", "frames": n, "size": [W, H], "sheet_box": [x0, y0, x1, y1]})
        print(f"{name:22s} {n:2d} frames  {W}x{H}")
    json.dump(index, open(os.path.join(a.out, "index.json"), "w"), indent=1)


if __name__ == "__main__":
    main()
