"""Read the simulator's frame stream, draw the HUD (boss name, HP, phase, the skill being cast) at 3x, write an MP4
through ffmpeg, and keep the busiest frame of each skill for a storyboard.
   dotnet run -c Release -- serena <repo> | python3 -I overlay.py <W> <H> <out-prefix> <font.ttc>"""
import json, struct, subprocess, sys
from PIL import Image, ImageDraw, ImageFont

OUT, FONT = sys.argv[3], sys.argv[4]   # argv[1:3] (W H) are ignored: each frame header carries its size
K = 3
big, mid, small = (ImageFont.truetype(FONT, s) for s in (30, 20, 15))
ff = None
W = H = 0
src = sys.stdin.buffer
best = {}      # skill -> (score, image)
order = []
n = 0


def bar(d, x, y, w, h, k, col):
    d.rectangle([x, y, x + w, y + h], fill=(10, 8, 16))
    d.rectangle([x + 2, y + 2, x + 2 + int((w - 4) * max(0, min(1, k))), y + h - 2], fill=col)


while True:
    head = src.read(4)
    if len(head) < 4:
        break
    (n_json,) = struct.unpack("<i", head)
    st = json.loads(src.read(n_json).decode("utf-8"))
    if ff is None:
        W, H = st["w"], st["h"]
        ff = subprocess.Popen(["ffmpeg", "-loglevel", "error", "-y", "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{W*K}x{H*K}", "-r", "30",
                               "-i", "-", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "20", OUT + ".mp4"], stdin=subprocess.PIPE)
    raw = src.read(W * H * 3)
    if len(raw) < W * H * 3:
        break
    base = Image.frombytes("RGB", (W, H), raw)
    img = base.resize((W * K, H * K), Image.NEAREST)
    d = ImageDraw.Draw(img)
    cx = img.width // 2
    d.text((cx, 14), f"{st['boss']} — {st['title']}", font=mid, fill=(240, 232, 255), anchor="mt")
    bar(d, cx - 300, 44, 600, 14, st["bossHp"], (150, 110, 240))
    d.text((cx, 66), f"{st['phase']}페이즈 — {st['phaseName']}", font=small, fill=(210, 200, 235), anchor="mt")
    if st["skill"]:
        d.text((cx, 96), st["skill"], font=big, fill=(255, 246, 228), anchor="mt", stroke_width=3, stroke_fill=(30, 18, 48))
    d.text((20, img.height - 54), "Player" + ("  [속박]" if st["rooted"] else ""), font=small, fill=(200, 230, 255))
    bar(d, 20, img.height - 32, 260, 12, st["playerHp"], (110, 200, 255))
    d.text((img.width - 16, img.height - 22), f"t={st['t']:.1f}s  · 실제 게임 C# 코드, Unity 대체 런타임에서 실행", font=small, fill=(150, 140, 175), anchor="rs")
    ff.stdin.write(img.tobytes())
    if st["skill"]:
        # how much is going on: bright pixels in the native frame
        score = sum(1 for p in base.getdata() if p[0] + p[1] + p[2] > 330)   # noqa
        key = (st["phase"], st["skill"])
        if key not in best:
            order.append(key)
        if key not in best or score > best[key][0]:
            best[key] = (score, img.copy())
    n += 1

ff.stdin.close()
ff.wait()
# storyboard: the busiest moment of each skill, 3 across
thumbs = [best[k][1].resize((W * K // 2, H * K // 2), Image.LANCZOS) for k in order]
if thumbs:
    cols = 3
    tw, th = thumbs[0].size
    rows = (len(thumbs) + cols - 1) // cols
    sb = Image.new("RGB", (cols * tw + (cols - 1) * 4, rows * th + (rows - 1) * 4), (0, 0, 0))
    for i, t in enumerate(thumbs):
        sb.paste(t, ((i % cols) * (tw + 4), (i // cols) * (th + 4)))
    sb.save(OUT + "-storyboard.png")
print(f"{n} frames -> {OUT}.mp4, {len(thumbs)} skills -> {OUT}-storyboard.png")
