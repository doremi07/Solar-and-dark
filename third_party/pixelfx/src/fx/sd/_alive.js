// Solar and Dark: makes a library effect feel alive for the boss fights. Written for this game (not upstream pixelfx).
//
// alive(base, cfg) wraps an effect module and layers secondary motion over it, timed to the base effect's own beats:
// energy gathering in before the release, a burst of sparks and a floor ring on contact, motes and embers drifting
// up while it lasts, glints that wink on and off, dark wisps that swell and thin, and light that flares and falls
// away. Every flourish is a pure function of t (analytic particles: drag + gravity in closed form), so one-shots end
// empty and loops wrap with no seam whatever the base effect is.
import { Rng, fit, clamp, ease, lerp, TAU, WHITE, hex, toHex, sparkle, band } from "../../core/index.js";

const mod = (a, n) => ((a % n) + n) % n;

// position and velocity after `age` seconds with drag k (1/s) and gravity g (px/s², +down)
function motion(q, age) {
  const k = q.k || 0.0001, e = Math.exp(-k * age), f = (1 - e) / k;
  const vt = q.g / k; // terminal velocity
  return {
    x: q.x + q.vx * f,
    y: q.y + (q.vy - vt) * f + vt * age,
    vx: q.vx * e,
    vy: (q.vy - vt) * e + vt,
  };
}

const pickRange = (R, r, def) => (Array.isArray(r) ? R.range(r[0], r[1]) : r ?? def);

/** Build the flourish particles once per seed. */
function build(cfg, D, loop, seed) {
  const R = new Rng(seed * 7919 + 13);
  const parts = [];
  const span = (e) => [e.t0 ?? 0, e.t1 ?? D];
  const clampLife = (ts, life) => (loop ? life : Math.max(0.05, Math.min(life, D - 0.04 - ts)));
  const emit = (list, kind, defaults) => {
    for (const e of list ?? []) {
      const [t0, t1] = span(e), n = Math.max(1, Math.round((e.rate ?? 10) * (cfg.density ?? 1.8) * (t1 - t0)));
      for (let i = 0; i < n; i++) {
        const ts = t0 + ((i + R.range(0.05, 0.95)) / n) * (t1 - t0);
        const q = {
          kind, ts,
          x: pickRange(R, e.x, 0), y: pickRange(R, e.y, 0),
          vx: pickRange(R, e.vx, defaults.vx), vy: pickRange(R, e.vy, defaults.vy),
          k: e.drag ?? defaults.k, g: e.g ?? defaults.g,
          size: pickRange(R, e.size, defaults.size), hot: R.chance(e.hot ?? 0.3), ph: R.range(0, TAU),
        };
        if (e.toward) { q.vx = (e.toward[0] - q.x) * (e.speedK ?? 1); q.vy = (e.toward[1] - q.y) * (e.speedK ?? 1); }
        q.life = clampLife(ts, pickRange(R, e.life, defaults.life));
        if (q.life > 0.05 && (loop || ts < D - 0.06)) parts.push(q);
      }
    }
  };
  emit(cfg.motes, "mote", { vx: [-6, 6], vy: [-30, -12], k: 0.8, g: -6, size: [1, 2.4], life: [0.7, 1.4] });
  emit(cfg.embers, "ember", { vx: [-10, 10], vy: [-40, -18], k: 1.2, g: -10, size: 1, life: [0.5, 1] });
  emit(cfg.glints, "glint", { vx: 0, vy: 0, k: 1, g: 0, size: [2.5, 5], life: [0.3, 0.55] });
  emit(cfg.wisps, "wisp", { vx: [-5, 5], vy: [-14, -6], k: 0.6, g: 0, size: [3, 6], life: [0.8, 1.4] });
  for (const b of cfg.bursts ?? []) {
    for (let i = 0; i < Math.round((b.n ?? 16) * (cfg.density ?? 1.8) * 0.8); i++) {
      const a0 = b.dir ?? -Math.PI / 2, sp = b.spread ?? TAU, a = a0 + R.range(-sp / 2, sp / 2), s = pickRange(R, b.speed, R.range(80, 200));
      const ts = b.t + R.range(0, b.jitter ?? 0.02);
      parts.push({ kind: "spark", ts, x: b.x + R.range(-(b.w ?? 2), b.w ?? 2), y: b.y, vx: Math.cos(a) * s, vy: Math.sin(a) * s, k: b.drag ?? 3, g: b.g ?? 260, size: 1, hot: R.chance(0.45), life: clampLife(ts, R.range(0.35, b.life ?? 0.85)), ph: 0 });
    }
    // every burst leaves a shower of glints hanging where it went off
    for (let i = 0; i < (b.glints ?? 7); i++) {
      const a = R.range(0, TAU), r = R.range(4, b.reach ?? 26), ts = b.t + R.range(0.05, 0.6);
      parts.push({ kind: "glint", ts, x: b.x + Math.cos(a) * r, y: Math.min(-1, b.y + Math.sin(a) * r * 0.8), size: R.range(2, 4.5), life: clampLife(ts, R.range(0.25, 0.5)), ph: 0 });
    }
  }
  for (const g of cfg.gather ?? []) {
    for (let i = 0; i < (g.n ?? 14); i++) {
      const a = R.range(0, TAU), r = R.range(g.r * 0.6, g.r), ts = R.range(g.t0, g.t1 - 0.12);
      parts.push({ kind: "streak", ts, x: g.x + Math.cos(a) * r, y: g.y + Math.sin(a) * r * (g.sy ?? 1), tx: g.x, ty: g.y, life: Math.min(g.t1 - ts, R.range(0.18, 0.35)), size: 1, hot: R.chance(0.4), ph: 0 });
    }
  }
  for (const r of cfg.rings ?? []) parts.push({ kind: "ring", ts: r.t, x: r.x ?? 0, y: r.y ?? 0, size: r.r ?? 30, sy: r.sy ?? 0.2, life: clampLife(r.t, r.life ?? 0.45), ph: 0 });
  for (const f of cfg.flares ?? []) parts.push({ kind: "flare", ts: f.t, x: f.x ?? 0, y: f.y ?? 0, size: f.r ?? 40, k: f.k ?? 0.6, life: clampLife(f.t, f.life ?? 0.35), ph: 0, white: f.white ?? true });
  for (const p of cfg.pulses ?? []) parts.push({ kind: "pulse", ts: 0, x: p.x ?? 0, y: p.y ?? 0, size: p.r ?? 30, k: p.k ?? 0.2, life: D, n: p.n ?? 2, ph: 0 });
  // a one-shot ends empty: nothing may outlive the base effect
  return loop ? parts : parts.filter((q) => q.kind === "pulse" || q.ts + q.life <= D - 0.03);
}

function drawPart(p, q, age, C, ramp, D, floor) {
  const u = age / q.life, fade = Math.min(1, u * 6) * (1 - u * u);
  // on a ground effect, flying bits that reach the floor are gone (they'd vanish into it)
  if (floor && (q.kind === "spark" || q.kind === "ember" || q.kind === "mote") && motion(q, age).y > 0.5) return;
  switch (q.kind) {
    case "mote":
    case "ember": {
      const m = motion(q, age), wob = Math.sin(age * 7 + q.ph) * (q.kind === "mote" ? 1.2 : 0.6);
      const x = m.x + wob, y = m.y, sp = Math.hypot(m.vx, m.vy) || 1;
      const head = q.hot ? WHITE : q.kind === "ember" ? C[6] : C[7];
      if (q.kind === "ember" && Math.sin(age * 23 + q.ph) < -0.6) return; // flicker
      p.dot(x, y, head, fade);
      if (q.size >= 2) for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) p.dot(x + dx, y + dy, C[6], fade * 0.9);
      p.dot(x - (m.vx / sp) * 1.5, y - (m.vy / sp) * 1.5, C[5], fade * 0.8);
      if (q.kind === "mote") p.dot(x - (m.vx / sp) * 3, y - (m.vy / sp) * 3, C[4], fade * 0.5);
      p.glow(x, y, q.size >= 2 ? 7 : 4, C[5], (q.size >= 2 ? 0.32 : 0.2) * fade);
      return;
    }
    case "spark": {
      const m = motion(q, age), sp = Math.hypot(m.vx, m.vy), L = clamp(sp * 0.045, 1, 9);
      const dx = m.vx / (sp || 1), dy = m.vy / (sp || 1);
      p.line(m.x, m.y, m.x - dx * L, m.y - dy * L, (v) => (v < 0.35 ? (q.hot ? WHITE : C[7]) : C[5]), (v) => fade * (1 - v * 0.6));
      p.glow(m.x, m.y, 6, C[6], 0.32 * fade);
      return;
    }
    case "glint": {
      const arm = q.size * Math.sin(Math.PI * clamp(u));
      if (arm >= 0.8) sparkle(p, q.x, q.y, arm, C[6], C[7], 0.35);
      else p.dot(q.x, q.y, C[6], 0.8);
      return;
    }
    case "wisp": {
      const m = motion(q, age), r = q.size * (0.6 + 0.8 * ease.outCubic(clamp(u))), a = 0.55 * Math.sin(Math.PI * clamp(u));
      p.disc(m.x, m.y, r, C[1], a);
      p.disc(m.x - r * 0.2, m.y - r * 0.35, r * 0.55, C[3], a * 0.45);
      return;
    }
    case "streak": {
      const e = ease.inCubic(clamp(u)), x = lerp(q.x, q.tx, e), y = lerp(q.y, q.ty, e);
      const dx = q.tx - q.x, dy = q.ty - q.y, d = Math.hypot(dx, dy) || 1, L = 2 + 6 * e;
      p.line(x, y, x - (dx / d) * L, y - (dy / d) * L, (v) => (v < 0.3 ? (q.hot ? WHITE : C[7]) : C[5]), (v) => (1 - v) * (0.4 + 0.6 * e));
      return;
    }
    case "ring": {
      const r = q.size * ease.outCubic(clamp(u));
      band(p, q.x, q.y, r, q.sy, 2, (1 - u) * 1.2, ramp, () => true, u < 0.15);
      const u2 = clamp((age - 0.08) / q.life);
      if (u2 > 0) band(p, q.x, q.y, q.size * 0.7 * ease.outCubic(u2), q.sy, 1, (1 - u2) * 0.8, ramp, () => true, false);
      return;
    }
    case "flare": {
      const k = q.k * (1 - u) * (1 - u);
      p.glow(q.x, q.y, q.size * (0.7 + 0.3 * u), C[5], k);
      p.glow(q.x, q.y, q.size * 0.35, C[7], k * 0.8);
      if (q.white && u < 0.18) sparkle(p, q.x, q.y, 3 + 6 * (1 - u / 0.18), C[7], WHITE, 0.6);
      return;
    }
    case "pulse": {
      const b = 0.5 + 0.5 * Math.sin((age / D) * TAU * q.n);
      p.glow(q.x, q.y, q.size, C[5], q.k * (0.6 + 0.4 * b));
      return;
    }
  }
}

/**
 * Wrap `base` (an effect module) with flourishes. cfg: { name, desc, palette, opts, light, grow: [l, t, r, b],
 * motes, embers, glints, wisps (emitters: {t0, t1, rate, x, y, vx, vy, life, size, drag, g, hot, toward: [x, y], speedK}),
 * bursts ({t, x, y, n, speed, dir, spread, g}), gather ({t0, t1, x, y, r, n, sy}), rings ({t, x, y, r, sy}),
 * flares ({t, x, y, r, k}), pulses ({x, y, r, k, n}), back: ["wisps", ...] kinds drawn behind }.
 */
export function alive(base, cfg) {
  const bm = base.meta, [gl, gt, gr, gb] = cfg.grow ?? [0, 0, 0, 0];
  const meta = {
    ...bm,
    name: cfg.name ?? bm.name,
    desc: cfg.desc ?? bm.desc,
    tags: [...new Set([...(bm.tags ?? []), "solar-and-dark", "boss"])],
    size: [bm.size[0] + gl + gr, bm.size[1] + gt + gb],
    origin: [bm.origin[0] + gl, bm.origin[1] + gt],
    options: { ...bm.options, ...(cfg.palette ? { palette: cfg.palette } : {}), ...(cfg.opts ?? {}), seed: bm.options?.seed ?? 1 },
  };
  const D = bm.duration, loop = !!bm.loop, back = new Set(cfg.back ?? ["wisp", "ring", "pulse"]);

  function create(o) {
    const impl = base.create ? base.create(o) : base;
    const C = fit(o.palette, 8), ramp = C.map(toHex);
    const parts = build(cfg, D, loop, o.seed ?? 1);
    const layer = (p, t, wantBack) => {
      for (const q of parts) {
        if (back.has(q.kind) !== wantBack) continue;
        const age = loop && q.kind !== "pulse" ? mod(t - q.ts, D) : t - q.ts;
        if (age < 0 || age >= q.life) continue;
        drawPart(p, q, age, C, ramp, D, cfg.floor !== false);
      }
    };
    return {
      update(dt, oo) {
        if (impl.update) impl.update(dt, oo);
      },
      drawBack(p, t, oo) {
        layer(p, t, true);
        if (impl.drawBack) impl.drawBack(p, t, oo);
      },
      draw(p, t, oo) {
        impl.draw(p, t, oo);
        layer(p, t, false);
      },
    };
  }
  return { meta, create };
}

export const PALETTES = {
  DUSK: ["#0b0712", "#1a1028", "#2c1c44", "#45306a", "#6a4f9a", "#9a82c8", "#d2c4ee", "#fbf6ff"],
  HALO: ["#1a1020", "#3d2a3a", "#7a5640", "#c0905a", "#e8c08a", "#f8e2b8", "#fff6e4", "#ffffff"],
  CRYSTAL: ["#120c22", "#261a46", "#43307a", "#6a52b0", "#9a86dc", "#c8baf4", "#ece6ff", "#ffffff"],
};
export { hex };
