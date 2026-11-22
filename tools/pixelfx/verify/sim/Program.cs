using System; using System.IO; using PixelFx;
// Drive the real parser/timing code like PixelFxPlayer does: a 60 fps game loop, 1 world unit = 16 px.
var d = PixelFxSheetData.Parse(File.ReadAllText(args[0]));
void Check(bool ok, string what) { Console.Error.WriteLine((ok ? "PASS " : "FAIL ") + what); if (!ok) Environment.ExitCode = 1; }
Check(d.frames.Count == 163 && d.frameWidth == 300 && d.frameHeight == 240, $"parsed {d.frames.Count} frames {d.frameWidth}x{d.frameHeight}");
Check(Math.Abs(d.TotalSeconds - 163 * 0.033f) < 0.01, $"total {d.TotalSeconds:F3}s");
d.PivotNormalized(out var px, out var py); Check(Math.Abs(px - 0.5) < 1e-6 && Math.Abs(py - (1 - 196 / 240.0)) < 1e-6, $"pivot {px:F4},{py:F4}");
d.UnityRect(0, out var x0, out var y0, out _, out _); Check(x0 == 0 && y0 == d.textureHeight - 240, $"frame0 unity rect y={y0}");
d.UnityRect(162, out var xl, out var yl, out _, out _); Check(yl >= 0, $"last frame unity rect {xl},{yl}");
Check(d.FrameAt(0, false, out _) == 0 && d.FrameAt(0.034f, false, out _) == 1, "frame timing start");
d.FrameAt(99, false, out var fin); Check(fin, "one-shot finishes");
Check(d.FrameAt(d.TotalSeconds + 0.001f, true, out var f2) == 0 && !f2, "loop wraps");
// game loop: print "time frameIndex unityX unityY" every 60fps tick until finished
float t = 0; int ticks = 0;
using var w = new StreamWriter(args[1]);
while (true) { int i = d.FrameAt(t, d.loop, out var done); d.UnityRect(i, out var ux, out var uy, out _, out _); w.WriteLine($"{t:F4} {i} {ux} {uy}"); if (done) break; t += 1f / 60f; ticks++; }
Check(ticks > 300 && ticks < 330, $"played {ticks} ticks ({t:F2}s) then finished");

// An Aseprite "Export Sprite Sheet" of an edited file (made by to-aseprite.py): trimmed frames, pivot and pixelfx meta
// in the "origin" slice, pivot nudged by the artist to (90, 101).
var ase = PixelFxSheetData.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "aseprite-export.json")));
Check(ase.id == "shadow/shadow-chains" && !ase.loop && ase.fps == 30, $"aseprite export keeps the pixelfx meta ({ase.id}, loop {ase.loop}, {ase.fps} fps)");
Check(ase.pivotX == 90 && ase.pivotY == 101, $"the slice's pivot wins ({ase.pivotX},{ase.pivotY})");
Check(ase.frameWidth == 176 && ase.frameHeight == 120 && ase.frames.Count == 3, $"full frame size from sourceSize ({ase.frameWidth}x{ase.frameHeight})");
// frame 0 is a 40x70 crop at (60,20): the pivot (90,101) sits 30 px in and 81 px down, i.e. 11 px below the crop
ase.PivotNormalized(0, out var tx, out var ty);
Check(Math.Abs(tx - 30 / 40f) < 1e-5 && Math.Abs(ty - (1 - 81 / 70f)) < 1e-5, $"trimmed frame keeps the effect in place (pivot {tx:F3},{ty:F3})");
ase.PivotNormalized(2, out var fx2, out var fy2);
Check(Math.Abs(fx2 - 90 / 176f) < 1e-5 && Math.Abs(fy2 - (1 - 101 / 120f)) < 1e-5, "untrimmed frame pivot");
// a plain Aseprite sheet with no slice at all still imports, feet at the bottom middle
var plain = PixelFxSheetData.Parse("{\"frames\":[{\"frame\":{\"x\":0,\"y\":0,\"w\":32,\"h\":48},\"duration\":100}],\"meta\":{\"image\":\"x.png\",\"size\":{\"w\":32,\"h\":48}}}");
Check(plain.pivotX == 16 && plain.pivotY == 48 && plain.fps == 10, $"plain sheet: bottom-middle pivot, fps from durations ({plain.pivotX},{plain.pivotY}, {plain.fps})");
