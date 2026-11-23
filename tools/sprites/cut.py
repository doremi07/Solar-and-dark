"""Cut the boss characters out of the concept sheets (art/bosses/concept) onto transparency (art/bosses/cutouts).

    pip install numpy opencv-python-headless onnxruntime pillow
    python3 tools/sprites/cut.py --model <isnet-anime.onnx>

Two signals, combined:
  - a flood fill from the panel's edges through the smooth background gradient (it stops at the pixel outlines);
  - an anime-character segmentation model (isnet-anime, from the rembg project; a local tool, not shipped), whose
    confident pixels are kept even where the fill leaked into dark cloth close to the background colour.
Then, in the halo zone (top 36 % of a panel), what shows through the ring is cleared: background, the faint shadow aura
and glow discs are dim and have nothing bright beside them, unlike the hair's outline. The cutouts are committed, so
the model is only needed to redo this step.
"""
import argparse, os
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
# panel boxes in sheet-1 (1500 x 1000): the phase-by-phase figures
PANELS = {
    "serena-p1": (400, 75, 565, 310), "serena-p2": (570, 75, 755, 310), "serena-p3": (760, 75, 950, 310),
    "child-p1": (400, 555, 570, 795), "child-p2": (565, 555, 765, 795), "child-p3": (765, 555, 990, 800),
}
NO_HALO_CLEAR = {"child-p3"}   # its dark tendrils fill the halo zone


def model_mask(sess, im):
    w, h = im.size; side = max(w, h)
    sq = Image.new("RGB", (side, side), im.getpixel((2, 2))); sq.paste(im, ((side - w) // 2, (side - h) // 2))
    x = (np.asarray(sq.resize((1024, 1024), Image.BICUBIC), dtype=np.float32) / 255.0).transpose(2, 0, 1)[None]
    m = sess.run(None, {sess.get_inputs()[0].name: x})[0][0, 0]
    m = (m - m.min()) / (m.max() - m.min() + 1e-6)
    mm = Image.fromarray((m * 255).astype(np.uint8)).resize((side, side), Image.BILINEAR)
    return np.asarray(mm.crop(((side - w) // 2, (side - h) // 2, (side - w) // 2 + w, (side - h) // 2 + h))) / 255.0


def cut(name, im, model, tol=5):
    a = np.asarray(im).astype(np.uint8); h, w, _ = a.shape
    sm = cv2.GaussianBlur(a, (3, 3), 0)
    mask = np.zeros((h + 2, w + 2), np.uint8); flags = 4 | (255 << 8) | cv2.FLOODFILL_MASK_ONLY
    seeds = [(x, y) for x in range(0, w, 4) for y in (0, h - 1)] + [(x, y) for y in range(0, h, 4) for x in (0, w - 1)]
    for x, y in seeds:
        if mask[y + 1, x + 1] == 0: cv2.floodFill(sm.copy(), mask, (x, y), 0, (tol,) * 3, (tol,) * 3, flags)
    bg = mask[1:-1, 1:-1] > 0
    # the background as a smooth quadratic field, to tell plain background from content
    ys, xs = np.nonzero(bg)
    A = np.stack([np.ones_like(xs), xs, ys, xs * xs, ys * ys, xs * ys], 1).astype(np.float64)
    X, Y = np.meshgrid(np.arange(w), np.arange(h))
    F = np.stack([np.ones_like(X), X, Y, X * X, Y * Y, X * Y], -1).reshape(-1, 6).astype(np.float64)
    fit = np.stack([F @ np.linalg.lstsq(A, a[ys, xs, c].astype(np.float64), rcond=None)[0] for c in range(3)], -1).reshape(h, w, 3)
    g = cv2.cvtColor(a, cv2.COLOR_RGB2GRAY).astype(np.float32)
    std = np.sqrt(np.maximum(cv2.blur(g * g, (5, 5)) - cv2.blur(g, (5, 5)) ** 2, 0))
    diff = np.abs(a.astype(np.float64) - fit).max(-1)
    fg = (~bg) | ((model > 0.5) & ~((diff < 14) & (std < 4)))
    if name not in NO_HALO_CLEAR:
        zone = np.zeros((h, w), bool); zone[: int(h * 0.36)] = True
        near7 = cv2.dilate(g, np.ones((7, 7), np.uint8)); near5 = cv2.dilate(g, np.ones((5, 5), np.uint8))
        fg &= ~(zone & (((g < 62) & (g > 12) & (near7 < 110)) | ((g >= 45) & (g < 112) & (near5 < 150))))
    n, lab, st, _ = cv2.connectedComponentsWithStats(fg.astype(np.uint8), 8)
    keep = np.zeros_like(fg)
    for i in range(1, n):
        x0, y0, ww, hh, ar = st[i]
        if ar < 25: continue
        if (x0 + ww >= w - 14 or x0 <= 14) and ww < 16 and ar < 160: continue   # the arrows between panels
        if ar < 60 and hh < 12 and y0 + hh > h - 40: continue                    # specks at the panel's foot
        keep |= lab == i
    return Image.fromarray(np.dstack([a, (keep * 255).astype(np.uint8)]))


def main():
    ap = argparse.ArgumentParser(); ap.add_argument("--model", required=True)
    ap.add_argument("--sheet", default=os.path.join(ROOT, "art/bosses/concept/sheet-1.webp"))
    ap.add_argument("--out", default=os.path.join(ROOT, "art/bosses/cutouts"))
    args = ap.parse_args()
    import onnxruntime as ort
    sess = ort.InferenceSession(args.model, providers=["CPUExecutionProvider"])
    sheet = Image.open(args.sheet).convert("RGB")
    os.makedirs(args.out, exist_ok=True)
    for name, box in PANELS.items():
        im = sheet.crop(box)
        cut(name, im, model_mask(sess, im)).save(os.path.join(args.out, name + ".png"))
        print(name)


if __name__ == "__main__":
    main()
