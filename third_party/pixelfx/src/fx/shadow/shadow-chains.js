// Solar and Dark: Serena's "shadow chains" bind. Written for this game (not upstream pixelfx).
import { Particles, Lights, Bits, B, P, Rng, fit, sparkle, clamp, smooth, ease, TAU, WHITE, READY, mix } from "../../core/index.js";

export const meta = {
  name: "Shadow chains",
  desc: "A sigil burns into the floor; four dark chains lash up out of it and wrap the body twice, cinch with a flash, rattle as it strains, then shatter into shards and motes.",
  tags: ["shadow", "chain", "chains", "bind", "snare", "root", "status", "dark", "actor", "one-shot"],
  size: [176, 120],
  origin: [88, 100],
  duration: 3.2,
  loop: false,
  fps: 30,
  bg: "ground",
  actor: 2,
  options: {
    palette: ["#0b0712", "#1a1028", "#2c1c44", "#45306a", "#6a4f9a", "#9a82c8", "#d2c4ee", "#fbf6ff"],
    seed: 5,
  },
};

// timing: sigil, chains out, wraps closing, cinch, break, gone
const SIGIL = 0.0, OUT = 0.24, WRAP = 0.56, CINCH = 0.78, BREAK = 2.0, RX = 42, RY = 10;
const LINK = 4.4; // px between link centres
const LX = -0.6, LY = -0.8; // light from the top left

const GRAB = { turn: 0.9, crouch: 3, fx: -10, fy: -30, bx: 11, by: -26, ffx: -2, fbx: 3 };
export const act = {
  poses: [
    [-0.2, { ...READY, ffx: -2, fbx: 3 }],
    [0.4, { turn: 0.9, lean: -1, crouch: 1, fx: -9, fy: -22, bx: 10, by: -22, ffx: -2, fbx: 3 }],
    [CINCH, { ...GRAB, fx: -6, fy: -18, bx: 7, by: -18 }, "snap"],
    [1.05, { ...GRAB, lean: -3, fx: -6, fy: -18, bx: 7, by: -18 }],
    [1.35, { ...GRAB, lean: 3, fx: -6, fy: -18, bx: 7, by: -18 }],
    [1.65, { ...GRAB, lean: -3, fx: -6, fy: -18, bx: 7, by: -18 }],
    [1.9, { ...GRAB, crouch: 6, fx: -6, fy: -18, bx: 7, by: -18 }],
    [BREAK, { turn: 1.1, crouch: -1, y: -2, fx: -14, fy: -34, bx: 15, by: -34, ffx: -3, fbx: 4 }, "snap"],
    [2.3, { ...READY, crouch: 4 }],
    [3.2, { ...READY, ffx: -2, fbx: 3 }],
  ],
};

// a quadratic curve from the floor anchor, up and over, down onto the body
function curve(c, u) {
  const v = 1 - u;
  return [v * v * c.ax + 2 * v * u * c.cx + u * u * c.bx, v * v * c.ay + 2 * v * u * c.cy + u * u * c.by];
}

// sample a chain's curve every LINK px of arc length: [{ x, y, th }]
function links(c) {
  const out = [];
  let [px, py] = curve(c, 0), acc = 0;
  out.push({ x: px, y: py, th: Math.atan2(c.cy - c.ay, c.cx - c.ax) });
  for (let i = 1; i <= 200; i++) {
    const [x, y] = curve(c, i / 200), d = Math.hypot(x - px, y - py);
    acc += d;
    if (acc >= LINK) {
      out.push({ x, y, th: Math.atan2(y - py, x - px) });
      acc -= LINK;
    }
    px = x;
    py = y;
  }
  return out;
}

// one link: even links face us (a ring with a dark hole), odd ones are edge-on (a short bar); lit on the side
// towards the light, white where the glint passes
function link(p, x, y, th, i, C, hot) {
  const ca = Math.cos(th), sa = Math.sin(th);
  if (i % 2 === 0) {
    for (let k = 0; k < 18; k++) {
      const g = (k / 18) * TAU, lx = Math.cos(g) * 2.2, ly = Math.sin(g) * 1.3;
      p.px(x + lx * ca - ly * sa, y + lx * sa + ly * ca, C[1]);
    }
    for (let k = 0; k < 20; k++) {
      const g = (k / 20) * TAU, lx = Math.cos(g) * 3.1, ly = Math.sin(g) * 2;
      const X = x + lx * ca - ly * sa, Y = y + lx * sa + ly * ca;
      const nx = Math.cos(g) * ca - Math.sin(g) * sa, ny = Math.cos(g) * sa + Math.sin(g) * ca, f = nx * LX + ny * LY;
      p.px(X, Y, hot > 0.5 ? WHITE : f > 0.45 ? C[6] : f > -0.2 ? C[5] : C[3]);
    }
    p.px(x, y, C[0]);
  } else {
    const nx = -sa, ny = ca, f = nx * LX + ny * LY >= 0 ? 1 : -1;
    for (let d = -2.6; d <= 2.6; d += 0.5) {
      p.px(x + ca * d - nx * f * 0.6, y + sa * d - ny * f * 0.6, C[1]);
      p.px(x + ca * d, y + sa * d, C[3]);
      p.px(x + ca * d + nx * f * 0.9, y + sa * d + ny * f * 0.9, hot > 0.5 ? WHITE : Math.abs(d) < 1.6 ? C[6] : C[5]);
    }
  }
}

export function create(o) {
  const C = fit(o.palette, 8), R = new Rng(o.seed), floor = () => 0;
  const DIM = [C[0], C[0], C[1], C[2], C[3], C[4], C[5], C[6]];
  const ps = new Particles({ rng: R, ground: floor, sparks: [C[7], C[6], C[5], C[4]], haze: C[4] });
  const bits = new Bits({ ground: floor }), lights = new Lights();

  // four chains: two from behind (dim, drawn behind the body), two in front; each lands on one of the wraps
  const chains = [
    { ang: -2.5, wrap: 0, side: -1, t0: OUT + 0.05 },
    { ang: -0.65, wrap: 1, side: 1, t0: OUT },
    { ang: 2.55, wrap: 1, side: -1, t0: OUT + 0.09 },
    { ang: 0.6, wrap: 0, side: 1, t0: OUT + 0.03 },
  ].map((c) => {
    const ax = Math.cos(c.ang) * RX, ay = Math.sin(c.ang) * RY, by = c.wrap === 0 ? -24 : -12;
    const back = ay < 0, bx = c.side * 9;
    return { ...c, back, ax, ay, bx, by, cx: ax * 0.75 + R.range(-4, 4), cy: by - R.range(26, 34), rattle: R.range(0, TAU) };
  });
  const pull = (c, tt) => {
    const k = 0.75 * ease.outBack(clamp((tt - CINCH + 0.04) / 0.16), 2.2) * (1 - 0.25 * smooth(1.2, BREAK, tt));
    const mx = (c.ax + c.bx) / 2, my = (c.ay + c.by) / 2;
    return { ...c, cx: c.cx0 + (mx - c.cx0) * k, cy: c.cy0 + (my - 4 - c.cy0) * k };
  };
  for (const c of chains) { c.cx0 = c.cx; c.cy0 = c.cy; c.pts = links(c); c.n0 = c.pts.length; }
  // the two wraps round the body: tilted rings of links, closing from wide to tight
  const wraps = [{ y: -24, t0: WRAP }, { y: -12, t0: WRAP + 0.06 }];
  let t = 0, cinched = false, broken = false, acc = 0;

  const ringR = (w, tt) => 20 - 6 * ease.outBack(clamp((tt - w.t0) / (CINCH - w.t0)), 2);
  const sigilK = (tt) => smooth(SIGIL, SIGIL + 0.25, tt) * (1 - smooth(BREAK + 0.2, BREAK + 1.0, tt));

  function shatter(x, y, th) {
    bits.add({ kind: B.CHUNK, x, y, vx: R.range(-70, 70) + Math.cos(th) * 20, vy: -R.range(60, 170), a: R.range(0, TAU), va: R.range(-14, 14), s: R.range(1, 2), ramp: [C[1], C[3], C[5], C[6]], g: 0.8, drag: 1.2, bounce: 0.3, life: R.range(0.5, 0.9) });
    if (R.chance(0.35)) ps.spawn(P.MOTE, x, y, R.range(-20, 20), -R.range(20, 60), R.range(0.4, 0.8), R.pick([C[6], C[5]]), 1);
  }

  return {
    update(dt) {
      t += dt;
      for (const c of chains) if (t > CINCH - 0.05) c.pts = links(pull(c, t));
      if (!cinched && t >= CINCH) {
        cinched = true;
        lights.add(0, -18, 46, C[6], 0.85, 0.3);
        for (let i = 0; i < 18; i++) {
          const a = R.range(0, TAU);
          ps.spawn(P.SPARK, Math.cos(a) * 12, -18 + Math.sin(a) * 3, Math.cos(a) * R.range(90, 200), Math.sin(a) * R.range(40, 120) - 60, R.range(0.2, 0.45));
        }
      }
      if (!broken && t >= BREAK) {
        broken = true;
        lights.add(0, -18, 54, C[7], 0.9, 0.35);
        for (const c of chains) for (const q of c.pts) shatter(q.x, q.y, q.th);
        for (const w of wraps) for (let k = 0; k < 16; k++) {
          const g = (k / 16) * TAU;
          shatter(Math.cos(g) * 12, w.y + Math.sin(g) * 3, g);
        }
        for (let i = 0; i < 26; i++) {
          const a = R.range(0, TAU);
          ps.spawn(P.SPARK, Math.cos(a) * 8, -18, Math.cos(a) * R.range(80, 240), -R.range(80, 220), R.range(0.25, 0.55));
        }
        for (let i = 0; i < 8; i++) ps.spawn(P.VAPOUR, R.range(-14, 14), -R.range(6, 30), R.range(-10, 10), -R.range(8, 20), R.range(0.7, 1.1), R.chance(0.5) ? C[3] : C[4], R.range(4, 7));
      }
      // the sigil breathes motes; glints run along the taut chains while they hold
      const k = sigilK(t);
      if (k > 0.3 && t < BREAK + 0.2) for (acc += dt * 14 * k; acc > 1; acc--) {
        const g = R.range(0, TAU);
        ps.spawn(P.MOTE, Math.cos(g) * RX * 0.9, Math.sin(g) * RY * 0.9, R.range(-4, 4), -R.range(20, 50), R.range(0.5, 1), R.pick([C[6], C[5], C[5], C[4]]), R.range(0.6, 1));
      }
      if (t > CINCH && t < BREAK && R.chance(dt * 5)) {
        const c = R.pick(chains), q = c.pts[Math.floor(R.range(0.2, 0.9) * c.pts.length)];
        ps.spawn(P.STAR, q.x, q.y - 1, 0, -3, R.range(0.25, 0.45), C[7], R.range(1.2, 2.2));
      }
      ps.update(dt);
      bits.update(dt);
      lights.update(dt);
    },
    drawBack(p) {
      const k = sigilK(t);
      if (k > 0) {
        // the sigil: two counter-turning rings and eight ticks on the floor, pooled in violet light
        const turn = t * 0.8;
        band(p, RX, RY, 1.1, k * 0.7, C);
        band(p, RX * 0.72, RY * 0.72, 0.6, k * 0.45, C);
        for (let i = 0; i < 8; i++) {
          const g = (i / 8) * TAU + turn, x = Math.cos(g), y = Math.sin(g);
          p.line(x * RX * 0.74, y * RY * 0.74, x * RX * 0.96, y * RY * 0.96, i % 2 ? C[4] : C[5], k * 0.7);
        }
        p.glow(0, -2, RX * 1.4, C[4], 0.28 * k);
        p.glow(0, 0, RX * 0.8, C[5], 0.18 * k);
      }
      ps.draw(p, "back");
      if (!broken) {
        for (const c of chains) if (c.back) drawChain(p, c, DIM);
        for (const w of wraps) drawWrap(p, w, DIM, (g) => Math.sin(g) < 0);
      }
    },
    draw(p) {
      if (!broken) {
        for (const c of chains) if (!c.back) drawChain(p, c, C);
        for (const w of wraps) drawWrap(p, w, C, (g) => Math.sin(g) >= 0);
        if (t >= CINCH && t < CINCH + 0.08) sparkle(p, 0, -18, 8 * (1 - (t - CINCH) / 0.08) + 2, C[6], C[7], 0.6);
      }
      bits.draw(p);
      ps.draw(p, "front");
      lights.draw(p);
    },
  };

  function band(p, rx, ry, th, k, ramp) {
    for (let j = 0, m = Math.ceil(rx * TAU); j < m; j++) {
      const g = (j / m) * TAU;
      for (let d = 0; d < th; d += 0.5) p.dot(Math.cos(g) * (rx - d), Math.sin(g) * (ry - d * 0.25), d < 0.5 ? ramp[5] : ramp[3], k);
    }
  }

  // a chain grows out of the floor along its curve (overshooting a little), then hangs taut and trembles
  function drawChain(p, c, ramp) {
    const e = ease.outCubic(clamp((t - c.t0) / (WRAP - c.t0 + 0.06)));
    if (e <= 0) return;
    const n = e >= 1 ? c.pts.length : Math.max(1, Math.min(c.pts.length, Math.floor(c.n0 * e))), strain = t > CINCH ? Math.sin(t * 38 + c.rattle) * 0.6 * (1 - smooth(1.2, BREAK, t) * 0.5) : 0;
    const glintAt = ((t - c.t0) * 40) % (c.pts.length + 30);
    // a dark spine first, so the links read as one heavy chain rather than loose dashes
    for (let i = 1; i < n; i++) {
      const a = c.pts[i - 1], b = c.pts[i];
      p.line(a.x, a.y, b.x, b.y, ramp[1]);
      p.line(a.x, a.y + 1, b.x, b.y + 1, ramp[0]);
    }
    for (let i = 0; i < n; i++) {
      const q = c.pts[i], u = i / c.pts.length, sway = strain * Math.sin(u * Math.PI);
      link(p, q.x - Math.sin(q.th) * sway, q.y + Math.cos(q.th) * sway, q.th, i, ramp, Math.abs(i - glintAt) < 0.6 ? 1 : 0);
      if (i % 4 === 0) p.glow(q.x, q.y, 8, ramp[4], 0.12);
    }
    // the leading end: a hooked spike of light until it bites
    if (t < WRAP + 0.06) {
      const q = c.pts[n - 1];
      p.px(q.x + Math.cos(q.th) * 2, q.y + Math.sin(q.th) * 2, WHITE);
      p.px(q.x + Math.cos(q.th), q.y + Math.sin(q.th), ramp[6]);
      p.glow(q.x, q.y, 7, ramp[6], 0.4);
    }
    // where it leaves the floor: a lit collar
    p.line(c.ax - 3, c.ay, c.ax + 3, c.ay, ramp[5]);
    p.px(c.ax, c.ay - 1, ramp[6]);
  }

  // a wrap: links on a tilted ring round the body, coming round from where its chain bit
  function drawWrap(p, w, ramp, keep) {
    if (t < w.t0) return;
    const r = ringR(w, t), sweep = TAU * ease.outCubic(clamp((t - w.t0) / (CINCH - w.t0)));
    const n = Math.round((r * TAU * 0.85) / LINK), strain = t > CINCH ? Math.sin(t * 31 + w.y) * 0.4 : 0;
    for (let i = 0; i < n; i++) {
      const g = (i / n) * TAU - Math.PI / 2;
      if ((i / n) * TAU > sweep || !keep(g)) continue;
      const x = Math.cos(g) * r, y = w.y + Math.sin(g) * r * 0.3 + strain;
      link(p, x, y, g + Math.PI / 2, i, ramp, Math.abs(((t * 2.5) % 1) * n - i) < 0.5 && t > CINCH ? 1 : 0);
    }
    if (t > CINCH - 0.05) p.glow(0, w.y, r + 6, ramp[5], 0.16);
  }
}
