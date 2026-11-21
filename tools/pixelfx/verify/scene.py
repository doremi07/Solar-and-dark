import sys
# Stand-in game scene for tools/pixelfx/verify/run.sh: pivot on the ground at x=300, a 12x24 hero for scale.
# Tuned to explosions/small-explosion (300x240 frames, pivot 150,196).
from PIL import Image, ImageDraw, ImageOps
png, ticks, out = sys.argv[1:4]
sheet = ImageOps.flip(Image.open(png).convert("RGBA"))   # Unity texture space: origin bottom-left
H = sheet.height; FW, FH = 300, 240; PX, PY = 0.5, 1 - 196 / 240   # pivot from the sim
W, SH, GY = 480, 300, 250                                # scene size; ground line y (screen)
def scene(ux, uy):
    img = Image.new("RGBA", (W, SH), (20, 18, 31, 255)); d = ImageDraw.Draw(img)
    d.rectangle([0, GY, W, SH], fill=(51, 46, 61, 255))
    for x in range(0, W, 16): d.line([x, GY, x + 8, GY], fill=(70, 64, 82, 255))
    d.rectangle([120, GY - 24, 131, GY - 1], fill=(120, 180, 220, 255)); d.rectangle([122, GY - 20, 124, GY - 18], fill=(10, 10, 20, 255))  # stand-in hero
    fr = ImageOps.flip(sheet.crop((ux, uy, ux + FW, uy + FH)))   # back to screen space for display
    ax, ay = 300 - int(PX * FW), GY - int((1 - PY) * FH)                  # place pivot at the ground point x=300
    img.alpha_composite(fr, (ax, ay)); return img.convert("RGB")
rows = [l.split() for l in open(ticks)]
frames = [scene(int(r[2]), int(r[3])) for r in rows[::2]]                 # 30 fps gif
frames[0].save(out + ".gif", save_all=True, append_images=frames[1:], duration=33, loop=0)
pick = [rows[i] for i in (0, 6, 12, 24, 48, 90, 150, 240, len(rows) - 1)]
strip = Image.new("RGB", (W * 3 + 8, SH * 3 + 8), (0, 0, 0))
for k, r in enumerate(pick):
    im = scene(int(r[2]), int(r[3])); ImageDraw.Draw(im).text((6, 6), f"t={float(r[0]):.2f}s frame {r[1]}", fill=(255, 255, 255))
    strip.paste(im, ((k % 3) * (W + 4), (k // 3) * (SH + 4)))
strip.save(out + "-strip.png"); print("ok", len(frames))
