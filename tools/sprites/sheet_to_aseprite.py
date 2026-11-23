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


def write(path, frames_cut, frames_orig, pivot, duration=100):
    h = max(f.shape[0] for f in frames_cut); w = max(f.shape[1] for f in frames_cut)
    W, H = w + 4, h + 4
    colors = A.auto_palette(Image.fromarray(np.concatenate([f.reshape(-1, 4) for f in frames_cut])[None]))
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
    for name, x0, y0, x1, y1 in GROUPS:
        g = cut[y0:y1, x0:x1]
        al = g[..., 3] > 0
        spans = [(0, g.shape[1])] if name.endswith("-art") else split_count(al, COUNTS[name]) if name in COUNTS else pieces(al, x0, x1)
        fc, fo = [], []
        for a_, b_ in spans:
            piece = g[:, a_:b_]
            ys = np.nonzero(piece[..., 3].any(1))[0]
            top, bot = ys.min(), ys.max() + 1
            fc.append(np.ascontiguousarray(piece[top:bot]))
            o = orig[y0 + top: y0 + bot, x0 + a_: x0 + b_].copy()
            fo.append(np.ascontiguousarray(o))
        n, W, H = write(os.path.join(a.out, name + ".aseprite"), fc, fo, None)
        index.append({"file": name + ".aseprite", "frames": n, "size": [W, H], "sheet_box": [x0, y0, x1, y1]})
        print(f"{name:22s} {n:2d} frames  {W}x{H}")
    json.dump(index, open(os.path.join(a.out, "index.json"), "w"), indent=1)


if __name__ == "__main__":
    main()
