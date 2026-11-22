"""Convert pixelfx sprite sheets (PNG + JSON) into .aseprite files an artist can edit.

    python3 tools/pixelfx/to-aseprite.py <sheet.json|dir> [...] [--out DIR] [--manifest tools/pixelfx/boss-fx.json]

Each file gets:
  - one frame per sheet frame, with its own duration (pixelfx sheets are 30 fps → 33 ms)
  - one RGBA layer named after the effect, cels trimmed to their pixels
  - a tag over all frames (forward; a loop effect is marked in the slice data)
  - a slice "origin" covering the canvas whose pivot is the effect's origin (feet / impact point), and whose user
    data holds the sheet's meta.pixelfx as JSON (id, fps, loop, act timing...), so an Aseprite export of the edited
    file goes straight back into Unity (see docs/pixelfx-aseprite.md)
  - the effect's palette ramp as the sprite palette, when the manifest names one

Format: https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md (written as version 1.3).
Uses only the standard library plus Pillow for reading the PNG.
"""
import json
import os
import struct
import sys
import zlib

from PIL import Image


def string(s):
    b = s.encode("utf-8")
    return struct.pack("<H", len(b)) + b


def chunk(kind, data):
    return struct.pack("<IH", len(data) + 6, kind) + data


def color_profile():
    # sRGB, no fixed gamma
    return chunk(0x2007, struct.pack("<HHI", 1, 0, 0) + bytes(8))


def palette(colors):
    data = struct.pack("<III", len(colors), 0, len(colors) - 1) + bytes(8)
    for (r, g, b, a) in colors:
        data += struct.pack("<HBBBB", 0, r, g, b, a)
    return chunk(0x2019, data)


def layer(name):
    # visible + editable, normal layer, child level 0, normal blend, opaque
    return chunk(0x2004, struct.pack("<HHHHHHB", 3, 0, 0, 0, 0, 0, 255) + bytes(3) + string(name))


def cel(x, y, w, h, rgba):
    head = struct.pack("<HhhBHh", 0, x, y, 255, 2, 0) + bytes(5) + struct.pack("<HH", w, h)
    return chunk(0x2005, head + zlib.compress(rgba, 9))


def tags(items):
    data = struct.pack("<H", len(items)) + bytes(8)
    for (frm, to, direction, name) in items:
        data += struct.pack("<HHBH", frm, to, direction, 0) + bytes(6) + bytes([0, 0, 0]) + b"\x00" + string(name)
    return chunk(0x2018, data)


def slice_with_pivot(name, w, h, px, py):
    data = struct.pack("<III", 1, 2, 0) + string(name)   # one key, flag 2 = has pivot
    data += struct.pack("<IiiIIii", 0, 0, 0, w, h, px, py)
    return chunk(0x2022, data)


def user_data_text(text):
    return chunk(0x2020, struct.pack("<I", 1) + string(text))


def frame(duration_ms, chunks):
    body = b"".join(chunks)
    n = len(chunks)
    head = struct.pack("<IHHH", 16 + len(body), 0xF1FA, min(n, 0xFFFF), duration_ms) + bytes(2) + struct.pack("<I", n)
    return head + body


def header(size, frames, w, h, ncolors):
    h_ = struct.pack("<IHHHHHIH", size, 0xA5E0, frames, w, h, 32, 1, 100)
    h_ += struct.pack("<II", 0, 0) + bytes([0]) + bytes(3) + struct.pack("<H", ncolors)
    h_ += bytes([1, 1]) + struct.pack("<hhHH", 0, 0, 16, 16) + bytes(84)
    assert len(h_) == 128
    return h_


def hex_rgba(s):
    s = s.lstrip("#")
    if len(s) == 3:
        s = "".join(c * 2 for c in s)
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), 255)


def convert(json_path, out_dir, ramp=None):
    meta_all = json.load(open(json_path, encoding="utf-8"))
    meta = meta_all["meta"]
    pfx = meta.get("pixelfx", {})
    sheet = Image.open(os.path.join(os.path.dirname(json_path), meta.get("image") or os.path.basename(json_path)[:-5] + ".png")).convert("RGBA")
    frames = meta_all["frames"]
    if isinstance(frames, dict):
        frames = list(frames.values())
    fw, fh = frames[0]["frame"]["w"], frames[0]["frame"]["h"]
    px, py = (pfx.get("pivot") or [fw // 2, fh])[:2]
    name = os.path.splitext(os.path.basename(json_path))[0]

    first = [color_profile()]
    colors = [hex_rgba(c) for c in ramp] if ramp else []
    if colors:
        first.append(palette(colors))
    first.append(layer(name))
    out_frames = []
    for i, f in enumerate(frames):
        r = f["frame"]
        img = sheet.crop((r["x"], r["y"], r["x"] + r["w"], r["y"] + r["h"]))
        chunks = list(first) if i == 0 else []
        box = img.getchannel("A").getbbox()
        if box:   # an empty frame has no cel at all
            c = img.crop(box)
            chunks.append(cel(box[0], box[1], c.width, c.height, c.tobytes()))
        if i == 0:
            chunks.append(tags([(0, len(frames) - 1, 0, name)]))
            chunks.append(slice_with_pivot("origin", fw, fh, int(px), int(py)))
            chunks.append(user_data_text(json.dumps({"pixelfx": pfx}, ensure_ascii=False)))
        out_frames.append(frame(int(f.get("duration") or round(1000 / (pfx.get("fps") or 30))), chunks))

    body = b"".join(out_frames)
    data = header(128 + len(body), len(frames), fw, fh, len(colors)) + body
    os.makedirs(out_dir, exist_ok=True)
    out = os.path.join(out_dir, name + ".aseprite")
    with open(out, "wb") as fh_:
        fh_.write(data)
    return out, len(frames), fw, fh


def main(argv):
    out_dir, manifest, inputs = "records/aseprite", None, []
    it = iter(argv)
    for a in it:
        if a == "--out":
            out_dir = next(it)
        elif a == "--manifest":
            manifest = next(it)
        else:
            inputs.append(a)
    ramps = {}
    if manifest:
        m = json.load(open(manifest, encoding="utf-8"))
        for s in m["sheets"]:
            p = s.get("palette")
            ramps[s["name"]] = m["palettes"][p] if isinstance(p, str) else p
    files = []
    for p in inputs:
        if os.path.isdir(p):
            files += sorted(os.path.join(p, f) for f in os.listdir(p) if f.endswith(".json"))
        else:
            files.append(p)
    if not files:
        print(__doc__)
        return 1
    for f in files:
        name = os.path.splitext(os.path.basename(f))[0]
        out, n, w, h = convert(f, out_dir, ramps.get(name))
        print(f"{name:28s} {n:4d} frames {w}x{h} -> {out} ({os.path.getsize(out) // 1024} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
