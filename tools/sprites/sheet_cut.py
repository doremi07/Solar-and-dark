"""Cut the sprites out of a sheet drawn on a fake transparency checkerboard (art/bosses/serena-sheet/sheet.webp).

The checkerboard is two light greys (~229 and ~244) in cells that drift (the sheet was resampled), so it is told from
the art per connected light-neutral area: the checker averages ~236 and varies (two tones), the art's whites are
brighter (~246+) and even. Areas touching the sheet's edge are background.

    python3 tools/sprites/sheet_cut.py --model isnet-anime.onnx     # -> art/bosses/serena-sheet/cut.png
"""
import os
import numpy as np, cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def protect_mask(rgb, model):
    """Where the anime-segmentation model (isnet-anime, a local tool) sees a character, per sprite region."""
    import onnxruntime as ort, sys
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    from cut import model_mask
    sess = ort.InferenceSession(model, providers=["CPUExecutionProvider"])
    a = rgb.astype(np.int16)
    rough = ~((a.max(-1) - a.min(-1) <= 10) & (a.min(-1) >= 215))
    m = cv2.dilate(rough.astype(np.uint8), np.ones((15, 15), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(m, 8)
    out = np.zeros(rough.shape, np.float32)
    for i in range(1, n):
        x, y, w, h, ar = st[i]
        if ar < 400: continue
        crop = Image.fromarray(rgb[y:y + h, x:x + w])
        out[y:y + h, x:x + w] = np.maximum(out[y:y + h, x:x + w], model_mask(sess, crop))
    return out


def cut(rgb, protect=None):
    a = rgb.astype(np.int16)
    h, w, _ = a.shape
    L = a.mean(-1)
    neutral = (a.max(-1) - a.min(-1) <= 10) & (a.min(-1) >= 215)
    n, lab, st, _ = cv2.connectedComponentsWithStats(neutral.astype(np.uint8), 4)
    bg = np.zeros((h, w), bool)
    for i in range(1, n):
        x, y, ww, hh, ar = st[i]
        sel = lab == i
        v = L[sel]
        edge = x == 0 or y == 0 or x + ww >= w or y + hh >= h
        guarded = protect is not None and float(protect[sel].mean()) >= 0.7
        if edge or (not guarded and (v.mean() < 243.5 or (v.mean() < 246.5 and v.std() >= 6.5 and ar >= 20))): bg |= sel
    # the soft fringe where the art meets the checker: neutral, light, beside the background
    near = cv2.dilate(bg.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
    fringe = near & ~bg & (a.max(-1) - a.min(-1) <= 14) & (a.min(-1) >= 205)
    fg = ~(bg | fringe)
    n, lab, st, _ = cv2.connectedComponentsWithStats(fg.astype(np.uint8), 8)
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] < 4: fg &= lab != i
    return np.dstack([rgb, (fg * 255).astype(np.uint8)])


if __name__ == "__main__":
    import argparse
    ap = argparse.ArgumentParser(); ap.add_argument("--model", help="isnet-anime.onnx (guards pale parts of the art)")
    args = ap.parse_args()
    src = np.asarray(Image.open(os.path.join(ROOT, "art/bosses/serena-sheet/sheet.webp")).convert("RGB"))
    prot = protect_mask(src, args.model) if args.model else None
    Image.fromarray(cut(src, prot)).save(os.path.join(ROOT, "art/bosses/serena-sheet/cut.png"))
