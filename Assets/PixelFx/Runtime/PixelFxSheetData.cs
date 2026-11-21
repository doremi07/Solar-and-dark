using System;
using System.Collections.Generic;
using System.Globalization;

namespace PixelFx
{
    /// <summary>One frame of a sheet, in the PNG's pixel coordinates (origin top-left, as in the JSON).</summary>
    [Serializable]
    public struct PixelFxFrame
    {
        public int x, y, w, h;
        public int durationMs;
    }

    /// <summary>A contact moment from the effect's act: freeze everything for <see cref="freeze"/> seconds at <see cref="t"/>.</summary>
    [Serializable]
    public struct PixelFxHit
    {
        /// <summary>Seconds after the effect's first frame.</summary>
        public float t;
        /// <summary>Hit-stop length in seconds.</summary>
        public float freeze;
        public float shake;
        public float amp;
        public float push;
    }

    /// <summary>
    /// Everything a pixelfx sprite sheet JSON says, without UnityEngine types, so it can be tested outside Unity.
    /// </summary>
    [Serializable]
    public class PixelFxSheetData
    {
        public string id = "";
        public string displayName = "";
        public string version = "";
        public string image = "";
        public int textureWidth, textureHeight;
        public float fps = 30;
        public bool loop;
        public int frameWidth, frameHeight;
        /// <summary>The effect's origin within a frame, in pixels from the frame's top-left (meta.pixelfx.pivot).</summary>
        public int pivotX, pivotY;
        public int columns;
        /// <summary>True when the sheet was baked with the stock character and target (tools/pixelfx/export.mjs --act).</summary>
        public bool bakedAct;
        /// <summary>Seconds the attacker winds up before the effect's first frame (from meta.pixelfx.act).</summary>
        public float windup;
        public float recovery;
        public List<PixelFxFrame> frames = new List<PixelFxFrame>();
        public List<PixelFxHit> hits = new List<PixelFxHit>();

        public float TotalSeconds
        {
            get
            {
                long ms = 0;
                foreach (var f in frames) ms += f.durationMs;
                return ms / 1000f;
            }
        }

        /// <summary>
        /// Unity pivot for a frame: normalised, measured from the frame's bottom-left.
        /// </summary>
        public void PivotNormalized(out float px, out float py)
        {
            px = frameWidth > 0 ? pivotX / (float)frameWidth : 0.5f;
            py = frameHeight > 0 ? 1f - pivotY / (float)frameHeight : 0f;
        }

        /// <summary>A frame's rect with Unity's texture origin (bottom-left).</summary>
        public void UnityRect(int index, out int x, out int y, out int w, out int h)
        {
            var f = frames[index];
            x = f.x;
            y = textureHeight - f.y - f.h;
            w = f.w;
            h = f.h;
        }

        /// <summary>
        /// Which frame to show <paramref name="seconds"/> after the start. Loops wrap; one-shots hold the last
        /// frame and report <paramref name="finished"/> once their time is up.
        /// </summary>
        public int FrameAt(float seconds, bool looping, out bool finished)
        {
            finished = false;
            int n = frames.Count;
            if (n == 0) { finished = true; return -1; }
            long total = 0;
            foreach (var f in frames) total += Math.Max(1, f.durationMs);
            double ms = seconds * 1000.0;
            if (ms < 0) ms = 0;
            if (looping) ms %= total;
            else if (ms >= total) { finished = true; return n - 1; }
            long acc = 0;
            for (int i = 0; i < n; i++)
            {
                acc += Math.Max(1, frames[i].durationMs);
                if (ms < acc) return i;
            }
            return n - 1;
        }

        public static PixelFxSheetData Parse(string json)
        {
            var root = MiniJson.Parse(json) as Dictionary<string, object>;
            if (root == null) throw new FormatException("pixelfx sheet: root is not an object");
            var d = new PixelFxSheetData();
            var meta = Obj(root, "meta") ?? throw new FormatException("pixelfx sheet: no meta");
            d.image = Str(meta, "image");
            var size = Obj(meta, "size");
            if (size != null) { d.textureWidth = Int(size, "w"); d.textureHeight = Int(size, "h"); }

            var pfx = Obj(meta, "pixelfx") ?? throw new FormatException("pixelfx sheet: no meta.pixelfx (not a pixelfx sheet?)");
            d.id = Str(pfx, "id");
            d.displayName = Str(pfx, "name");
            d.version = Str(pfx, "version");
            d.fps = (float)Num(pfx, "fps", 30);
            d.loop = pfx.TryGetValue("loop", out var lp) && lp is bool b && b;
            d.columns = Int(pfx, "columns");
            d.bakedAct = Str(pfx, "baked") == "act";
            if (pfx.TryGetValue("frameSize", out var fs) && fs is List<object> fsl && fsl.Count >= 2)
            { d.frameWidth = ToInt(fsl[0]); d.frameHeight = ToInt(fsl[1]); }
            if (pfx.TryGetValue("pivot", out var pv) && pv is List<object> pvl && pvl.Count >= 2)
            { d.pivotX = ToInt(pvl[0]); d.pivotY = ToInt(pvl[1]); }

            var act = Obj(pfx, "act");
            if (act != null)
            {
                d.windup = (float)Num(act, "windup", 0);
                d.recovery = (float)Num(act, "recovery", 0);
                if (act.TryGetValue("hits", out var hs) && hs is List<object> hl)
                    foreach (var h in hl)
                        if (h is Dictionary<string, object> ho)
                            d.hits.Add(new PixelFxHit
                            {
                                t = (float)Num(ho, "t", 0), freeze = (float)Num(ho, "freeze", 0),
                                shake = (float)Num(ho, "shake", 0), amp = (float)Num(ho, "amp", 0), push = (float)Num(ho, "push", 0),
                            });
            }

            // frames: Aseprite "array" form (what pixelfx writes) or "hash" form
            var defaultMs = (int)Math.Round(1000.0 / Math.Max(1, d.fps));
            if (root.TryGetValue("frames", out var fr))
            {
                IEnumerable<object> list = fr as List<object>;
                if (list == null && fr is Dictionary<string, object> hash) list = hash.Values;
                if (list != null)
                    foreach (var o in list)
                    {
                        if (!(o is Dictionary<string, object> fo)) continue;
                        var r = Obj(fo, "frame") ?? throw new FormatException("pixelfx sheet: frame without rect");
                        d.frames.Add(new PixelFxFrame
                        {
                            x = Int(r, "x"), y = Int(r, "y"), w = Int(r, "w"), h = Int(r, "h"),
                            durationMs = fo.ContainsKey("duration") ? Int(fo, "duration") : defaultMs,
                        });
                    }
            }
            if (d.frames.Count == 0) throw new FormatException("pixelfx sheet: no frames");
            if (d.frameWidth == 0) { d.frameWidth = d.frames[0].w; d.frameHeight = d.frames[0].h; }
            if (d.textureWidth == 0 || d.textureHeight == 0)
                foreach (var f in d.frames)
                {
                    d.textureWidth = Math.Max(d.textureWidth, f.x + f.w);
                    d.textureHeight = Math.Max(d.textureHeight, f.y + f.h);
                }
            return d;
        }

        static Dictionary<string, object> Obj(Dictionary<string, object> o, string k) =>
            o.TryGetValue(k, out var v) ? v as Dictionary<string, object> : null;
        static string Str(Dictionary<string, object> o, string k) =>
            o.TryGetValue(k, out var v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";
        static double Num(Dictionary<string, object> o, string k, double def) =>
            o.TryGetValue(k, out var v) && v is double x ? x : def;
        static int Int(Dictionary<string, object> o, string k) => o.TryGetValue(k, out var v) ? ToInt(v) : 0;
        static int ToInt(object v) => v is double x ? (int)Math.Round(x) : 0;
    }
}
