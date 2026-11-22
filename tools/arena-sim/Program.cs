// Runs the real boss arena (BossLibrary + BossArenaScene + all runtime scripts) on the Unity stand-in and streams
// rendered frames: [int32 json length][json HUD state][W*H*3 RGB bytes] per frame, to stdout.
//   dotnet run -- <serena|child> <repo root> [seconds-per-extra]
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PixelFx;
using SolarDark.Combat;
using SolarDark.Combat.Editor;
using UnityEditor;
using UnityEngine;

static class Program
{
    const float Dt = 1f / 60f;
    static Stream stdout;
    static int W, H;
    static byte[] rgb;
    static float[] acc;

    static int Main(string[] args)
    {
        string which = args.Length > 0 ? args[0] : "serena", repo = args.Length > 1 ? args[1] : "../..";
        AssetDatabase.RepoRoot = Path.GetFullPath(repo);
        var work = Path.Combine(Path.GetTempPath(), "arena-sim-" + which);
        Directory.CreateDirectory(work);
        Directory.SetCurrentDirectory(work);   // BossLibrary writes asset folders relative to the project

        // the real data and the real scene builder
        var (serena, child) = BossLibrary.Build();
        var def = which == "child" ? child : serena;
        BossArenaScene.Build(def, which == "child" ? PlaceholderBody.Kind.HiddenChild : PlaceholderBody.Kind.Serena);
        Engine.EnterPlayMode();

        var boss = Engine.FindAll<BossController>().Single();
        var player = Engine.FindAll<PlayerDummy>().Single();
        var playerHp = player.GetComponent<Health>();
        // no keyboard here: the stand-in holds its ground in front of the boss so every attack has a target
        player.autoPilot = false;
        player.transform.position = new Vector3(-1.5f, 0, 0);
        var hits = new Dictionary<string, int>();
        playerHp.onHit.AddListener(h => hits[h.skillId] = hits.TryGetValue(h.skillId, out var k) ? k + 1 : 1);
        var cam = Camera.main;
        H = Mathf.RoundToInt(cam.orthographicSize * 2 * 16);
        W = (H * 16 / 9) & ~1;
        rgb = new byte[W * H * 3];
        acc = new float[W * H * 3];
        stdout = Console.OpenStandardOutput();

        // the showcase: each phase in turn, each of its new skills forced once, entry skills played by the AI itself
        var shown = new HashSet<BossSkill>();
        var log = new List<string>();
        int frame = 0;
        bool trace = Environment.GetEnvironmentVariable("SIM_TRACE") == "1";
        BossSkill lastCast = null;
        float clock = 0;

        void Run(float seconds, Func<bool> until = null)
        {
            float end = clock + seconds;
            while (clock < end && (until == null || !until()))
            {
                Engine.Tick(Dt);
                clock += Dt;
                if (playerHp.Fraction < 0.25f) playerHp.ResetFull();   // keep the stand-in alive for the whole reel
                if (trace && boss.Casting != lastCast)
                {
                    Console.Error.WriteLine($"trace clock={clock:F2} time={Time.time:F2} scale={Time.timeScale} casting={boss.Casting?.id ?? "-"}");
                    lastCast = boss.Casting;
                }
                if (frame++ % 2 == 0) Emit(boss, playerHp, cam);
            }
        }

        Run(1.2f);
        for (int p = 0; p < def.phases.Count; p++)
        {
            if (p > 0)
            {
                boss.JumpToPhase(p);
                log.Add($"phase {p + 1} {def.phases[p].name} (entry: {def.phases[p].entrySkill?.displayName})");
                Run(8f, () => boss.Phase == p && boss.Casting == null && clock > 0);   // wait for the phase entry to play out
                Run(0.6f);
            }
            foreach (var s in def.phases[p].skills)
            {
                if (!shown.Add(s)) continue;
                Run(8f, () => boss.Casting == null);   // let whatever the AI picked finish, or the forced cast queues behind it
                boss.ForceNext(s);
                Run(6f, () => boss.Casting == s);
                float start = clock;
                Run(12f, () => boss.Casting != s);
                hits.TryGetValue(s.id, out var nh);
                int sub = s.cues.Where(c => c.subSkill != null).Sum(c => hits.TryGetValue(c.subSkill.id, out var k) ? k : 0);
                log.Add($"  {s.displayName,-22} cast {clock - start:F2}s  hits on player {nh + sub}");
                Run(0.9f);
            }
        }
        Run(1.5f);
        stdout.Flush();

        Console.Error.WriteLine($"{def.displayName}: {frame / 2} frames, {clock:F1}s simulated");
        foreach (var l in log) Console.Error.WriteLine(l);
        Console.Error.WriteLine($"exceptions: {Engine.Errors.Count}");
        foreach (var e in Engine.Errors.Distinct().Take(5)) Console.Error.WriteLine(e);
        return Engine.Errors.Count == 0 ? 0 : 1;
    }

    static void Emit(BossController boss, Health player, Camera cam)
    {
        Render(cam);
        var st = boss.CurrentPhase;
        string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        var json = $"{{\"boss\":\"{Esc(boss.definition.displayName)}\",\"title\":\"{Esc(boss.definition.title)}\",\"phase\":{boss.Phase + 1},\"phaseName\":\"{Esc(st?.name)}\"," +
                   $"\"skill\":\"{Esc(boss.Casting?.displayName)}\",\"bossHp\":{boss.Health.Fraction:F3},\"playerHp\":{player.Fraction:F3},\"t\":{Time.time:F2},\"rooted\":{(player.GetComponent<StatusReceiver>().IsRooted ? "true" : "false")}}}";
        var jb = Encoding.UTF8.GetBytes(json);
        stdout.Write(BitConverter.GetBytes(jb.Length));
        stdout.Write(jb);
        stdout.Write(rgb);
    }

    // Draw every active SpriteRenderer, sorted like Unity's 2D renderer (sorting order, then draw order), with
    // nearest-neighbour sampling by inverse-mapping each screen pixel into the sprite's texture.
    static void Render(Camera cam)
    {
        var bg = cam.backgroundColor;
        for (int i = 0; i < W * H; i++) { acc[i * 3] = bg.r; acc[i * 3 + 1] = bg.g; acc[i * 3 + 2] = bg.b; }
        var cp = cam.transform.position;
        float upp = cam.orthographicSize * 2 / H;               // world units per screen pixel
        float left = cp.x - W / 2f * upp, bottom = cp.y - H / 2f * upp;

        foreach (var r in Engine.Renderers().OrderBy(r => r.sortingOrder).ThenBy(r => r.serial))
        {
            var sp = r.sprite;
            if (sp == null || sp.texture == null || r.color.a <= 0.003f) continue;
            var tr = r.transform;
            float ppu = sp.pixelsPerUnit, w = sp.rect.width, h = sp.rect.height;
            // the sprite's quad in its own local units
            float x0 = -sp.pivot.x / ppu, x1 = (w - sp.pivot.x) / ppu, y0 = -sp.pivot.y / ppu, y1 = (h - sp.pivot.y) / ppu;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var (lx, ly) in new[] { (x0, y0), (x1, y0), (x0, y1), (x1, y1) })
            {
                tr.ToWorld(lx, ly, out var wx, out var wy);
                minX = Math.Min(minX, wx); maxX = Math.Max(maxX, wx); minY = Math.Min(minY, wy); maxY = Math.Max(maxY, wy);
            }
            int sx0 = Math.Max(0, (int)Math.Floor((minX - left) / upp)), sx1 = Math.Min(W - 1, (int)Math.Ceiling((maxX - left) / upp));
            int sy0 = Math.Max(0, (int)Math.Floor((minY - bottom) / upp)), sy1 = Math.Min(H - 1, (int)Math.Ceiling((maxY - bottom) / upp));
            if (sx0 > sx1 || sy0 > sy1) continue;
            var tex = sp.texture;
            var c = r.color;
            for (int py = sy0; py <= sy1; py++)
            {
                float wy = bottom + (py + 0.5f) * upp;
                for (int px = sx0; px <= sx1; px++)
                {
                    float wx = left + (px + 0.5f) * upp;
                    tr.ToLocal(wx, wy, out var lx, out var ly);
                    if (r.flipX) lx = -lx;
                    float u = lx * ppu + sp.pivot.x, v = ly * ppu + sp.pivot.y;
                    if (u < 0 || v < 0 || u >= w || v >= h) continue;
                    int tx = (int)(sp.rect.x + u), ty = (int)(sp.rect.y + v);
                    var t = tex.pixels[ty * tex.width + tx];
                    float a = t.a / 255f * c.a;
                    if (a <= 0.003f) continue;
                    int o = ((H - 1 - py) * W + px) * 3;             // image rows go down
                    acc[o] += (t.r / 255f * c.r - acc[o]) * a;
                    acc[o + 1] += (t.g / 255f * c.g - acc[o + 1]) * a;
                    acc[o + 2] += (t.b / 255f * c.b - acc[o + 2]) * a;
                }
            }
        }
        for (int i = 0; i < rgb.Length; i++) rgb[i] = (byte)Math.Clamp((int)(acc[i] * 255 + 0.5f), 0, 255);
    }
}
