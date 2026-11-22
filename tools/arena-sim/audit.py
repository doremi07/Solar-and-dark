"""Draw each skill's hit areas over its own effect while the hit is live, and measure how well they agree.
    SIM_AUDIT=1 dotnet tools/arena-sim/bin/Release/net8.0/arena-sim.dll serena . 2> cues.jsonl
    python3 tools/arena-sim/audit.py cues.jsonl <out-dir>
Per hit: 'in' = share of the effect's solid pixels (while live) that fall inside the hit area; 'fill' = share of the
hit area the effect actually covers at its busiest live frame. Low 'in' = the effect hurts where nothing hits;
low 'fill' = the hit reaches where nothing is drawn."""
import json, math, os, sys
from PIL import Image, ImageDraw

PPU, K = 16, 2
cues = [json.loads(l) for l in open(sys.argv[1], encoding="utf-8") if l.startswith("{")]
out = sys.argv[2]; os.makedirs(out, exist_ok=True)
SHEETS = "Assets/PixelFx/Sheets/bosses"
cache = {}


def sheet(name):
    if name not in cache:
        m = json.load(open(f"{SHEETS}/{name}.json")); fr = m["frames"]; fr = list(fr.values()) if isinstance(fr, dict) else fr
        cache[name] = (Image.open(f"{SHEETS}/{name}.png").convert("RGBA"), fr, m["meta"]["pixelfx"])
    return cache[name]


def frame_at(name, t, loop):
    img, fr, px = sheet(name)
    total = sum(f.get("duration", 33) for f in fr) / 1000
    if loop or px.get("loop"): t %= total
    if t >= total: return None, px
    acc = 0
    for f in fr:
        acc += f.get("duration", 33) / 1000
        if t < acc:
            r = f["frame"]; return img.crop((r["x"], r["y"], r["x"] + r["w"], r["y"] + r["h"])), px
    return None, px


def inside(c, x, y):
    hx, hy = c["hoff"]; w, h = c["size"]
    if c["shape"] == "Circle": return (x - hx) ** 2 + (y - hy) ** 2 <= w * w
    return abs(x - hx) <= w / 2 and abs(y - hy) <= h / 2


rows, report = {}, []
for c in cues:
    if c["shape"] == "None" or (c["damage"] <= 0 and c["root"] <= 0): continue
    sk = [o for o in cues if o["id"] == c["id"]]
    name = c["sheet"] or next((o["sheet"] for o in sk if o["sheet"] and o["anchor"] == c["anchor"] and o["offset"] == c["offset"] and o["aim"] == c["aim"]), "")
    if not name: report.append(f"{c['id']}/{c['cue']}: no effect to compare"); continue
    ea = c["effectAnchor"]; d, a = c["delay"], c["active"]
    times = [d, d + max(a, 0.1) / 2, d + max(a, 0.1)]
    panels, solid_in, solid_all, best_fill = [], 0, 0, 0
    proj = c["proj"] > 0
    for t in [d + i * max(a, 0.05) / 6 for i in range(7)]:
        im, px = frame_at(name, t, c["loop"] > 0)
        if im is None: continue
        pvx, pvy = px["pivot"][:2]
        A = im.getchannel("A").load(); covered = set()
        for v in range(im.height):
            for u in range(im.width):
                if A[u, v] < 140: continue
                x = (u - pvx) / PPU - ea[0]; y = (pvy - v) / PPU - ea[1]
                solid_all += 1
                if inside(c, x, y): solid_in += 1; covered.add((round(x * 4), round(y * 4)))
        # hit area cells (quarter units)
        w, h = c["size"]; hx, hy = c["hoff"]
        if c["shape"] == "Circle": cells = sum(1 for i in range(-int(w * 4), int(w * 4) + 1) for j in range(-int(w * 4), int(w * 4) + 1) if i * i + j * j <= (w * 4) ** 2)
        else: cells = max(1, int(w * 4 + 1) * int(h * 4 + 1))
        best_fill = max(best_fill, len(covered) / cells)
    for t in times:
        im, px = frame_at(name, t, c["loop"] > 0)
        if im is None: im = Image.new("RGBA", (64, 64)); px = {"pivot": [32, 64]}
        pvx, pvy = px["pivot"][:2]
        # canvas covering both the effect and the hit area, in local units
        w, h = c["size"]; hx, hy = c["hoff"]
        hr = w if c["shape"] == "Circle" else None
        xs = [-pvx / PPU - ea[0], (im.width - pvx) / PPU - ea[0], hx - (hr or w / 2), hx + (hr or w / 2), 0]
        ys = [(pvy - im.height) / PPU - ea[1], pvy / PPU - ea[1], hy - (hr or h / 2), hy + (hr or h / 2), 0]
        x0, x1, y0, y1 = min(xs) - 0.5, max(xs) + 0.5, min(ys) - 0.5, max(ys) + 0.5
        W, H = int((x1 - x0) * PPU), int((y1 - y0) * PPU)
        cv = Image.new("RGBA", (W, H), (16, 13, 26, 255))
        X = lambda x: int((x - x0) * PPU); Y = lambda y: int((y1 - y) * PPU)
        cv.alpha_composite(im, (X(-pvx / PPU - ea[0]), Y(pvy / PPU - ea[1])))
        dr = ImageDraw.Draw(cv)
        dr.line([(0, Y(0)), (W, Y(0))], fill=(80, 70, 110, 255))
        dr.line([(X(0), Y(0) - 4), (X(0), Y(0) + 4)], fill=(120, 230, 255, 255)); dr.line([(X(0) - 4, Y(0)), (X(0) + 4, Y(0))], fill=(120, 230, 255, 255))
        if c["shape"] == "Circle": dr.ellipse([X(hx - w), Y(hy + w), X(hx + w), Y(hy - w)], outline=(255, 80, 60, 255))
        else: dr.rectangle([X(hx - w / 2), Y(hy + h / 2), X(hx + w / 2), Y(hy - h / 2)], outline=(255, 80, 60, 255))
        if proj: dr.text((2, 2), "projectile: area moves", fill=(255, 200, 120, 255))
        dr.text((2, H - 11), f"t={t:.2f}", fill=(200, 190, 230, 255))
        panels.append(cv.resize((W * K, H * K), Image.NEAREST))
    pin = solid_in / solid_all if solid_all else 0
    report.append(f"{c['id']:28s} {c['cue']:10s} {name:26s} in {pin:4.0%}  fill {best_fill:4.0%}  live {d:.2f}+{a:.2f}")
    rows.setdefault(c["id"], []).append((c["cue"], panels, pin, best_fill))

for sid, items in rows.items():
    pw = max(sum(p.width for p in ps) + 8 * len(ps) for _, ps, _, _ in items); ph = sum(max(p.height for p in ps) + 20 for _, ps, _, _ in items)
    img = Image.new("RGB", (pw, ph), (8, 6, 12)); dr = ImageDraw.Draw(img); y = 0
    for cue, ps, pin, fill in items:
        dr.text((4, y + 4), f"{sid} / {cue}   in {pin:.0%}  fill {fill:.0%}", fill=(230, 220, 255)); x = 0; y += 18
        for p in ps: img.paste(p, (x, y)); x += p.width + 8
        y += max(p.height for p in ps) + 2
    img.save(f"{out}/{sid}.png")
print("\n".join(report))
