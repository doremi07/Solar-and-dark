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
