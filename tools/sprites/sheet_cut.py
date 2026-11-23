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
    """Background = the checker's exactly-grey pixels (R = G = B within 3, light, not the pure white of a sparkle),
    in areas of at least 12 px; then the blend pixels along that edge (grey-ish, light) go too. The art's whites are
    lavender (blue a few levels above green), so they stay."""
    a = rgb.astype(np.int16)
    h, w, _ = a.shape
    L = a.mean(-1)
    chroma = a.max(-1) - a.min(-1)
    grey = (chroma <= 3) & (L >= 212) & (L <= 251)
    n, lab, st, _ = cv2.connectedComponentsWithStats(grey.astype(np.uint8), 4)
    bg = np.zeros((h, w), bool)
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] >= 12: bg |= lab == i
    # close pinholes in the checker (a compression speck inside a cell), never across art
    closed = cv2.morphologyEx(bg.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8)) > 0
    bg |= closed & (chroma <= 6) & (L >= 205)
    # the blend along the edge: two passes of greyish light pixels touching the background
    for _ in range(2):
        near = cv2.dilate(bg.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
        bg |= near & (chroma <= 7) & (L >= 200)
    fg = ~bg
    # what is left of the blend on the outside: light, washed-out pixels on the silhouette's edge (the art's own
    # outline there is dark, so it stays); peeled for up to three pixels
    cool = a[..., 2] >= a[..., 0] - 2                                  # the blend is bluish; the halo's gold is warm
    for _ in range(6):
        edge = fg & (cv2.erode(fg.astype(np.uint8), np.ones((3, 3), np.uint8)) == 0)
        fg &= ~(edge & (L >= 185) & (chroma <= 40) & cool)
    n, lab, st, _ = cv2.connectedComponentsWithStats(fg.astype(np.uint8), 8)
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] < 6: fg &= lab != i
    return np.dstack([rgb, (fg * 255).astype(np.uint8)])


if __name__ == "__main__":
    import argparse
    ap = argparse.ArgumentParser(); ap.add_argument("--model", help="isnet-anime.onnx (guards pale parts of the art)")
    args = ap.parse_args()
    src = np.asarray(Image.open(os.path.join(ROOT, "art/bosses/serena-sheet/sheet.webp")).convert("RGB"))
    Image.fromarray(cut(src)).save(os.path.join(ROOT, "art/bosses/serena-sheet/cut.png"))
