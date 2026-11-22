// Solar and Dark: the Hidden Child (움켜쥔 아이), the final boss. Drawn as pixel art in code (see _figure.js).
// A small girl hovering, facing right. Phases:
//   1 억눌린 빛 (held-in light): white dress, dark sash, cradling a dark orb, a few crystals orbiting her
//   2 폭주 (runaway): hair wild, eyes glowing, hem torn dark, arms flung open, shadow tendrils, more crystals
//   3 완전한 그림자 (full shadow): her body gone to shadow with a pale rim, white eyes, great tendrils behind
import { TAU, clamp, ease, lerp, sparkle, WHITE } from "../../core/index.js";
import { Raster, lit, tl, H } from "./_figure.js";
import { INK } from "./_serena.js";

const PHASES = {
  1: {
    skin: H(["#c4aebd", "#ead9e2", "#fdf3f8"]), hair: H(["#5f5290", "#8b7dbd", "#b6aadd", "#ddd5f5"]),
    dress: H(["#9d93bd", "#cdc6e2", "#efebfa", "#ffffff"]), sash: H(["#1a1028", "#33244e"]), shoe: H(["#2a1d3c"])[0],
    eye: H(["#6a4fb0", "#b9a6f4"]), tendril: H(["#07040c", "#140d22", "#2a1d44", "#5a4686"]), crystal: H(["#43307a", "#9a86dc", "#ece6ff"]),
    aura: H(["#b9a6f4"])[0], tendrils: 0, crystals: 3, wild: 0, glowEyes: false, orb: true, torn: false, arms: "orb",
  },
  2: {
    skin: H(["#a891a8", "#d6c3d2", "#f2e6ee"]), hair: H(["#6a5a98", "#9384c4", "#c4b8e8", "#efe9ff"]),
    dress: H(["#8a80aa", "#bcb4d6", "#e2dcf2", "#f8f6ff"]), sash: H(["#120a1e", "#2a1d40"]), shoe: H(["#1e142e"])[0],
    eye: H(["#b48cff", "#f0e4ff"]), tendril: H(["#07040c", "#160e26", "#30214e", "#6a54a0"]), crystal: H(["#43307a", "#9a86dc", "#ece6ff"]),
    aura: H(["#9a6cf0"])[0], tendrils: 4, crystals: 5, wild: 1, glowEyes: true, orb: false, torn: true, arms: "open",
  },
  3: {
    skin: H(["#120b1e", "#24183a", "#3e2c62"]), hair: H(["#140c22", "#261a3e", "#43306a", "#7a62b6"]),
    dress: H(["#0c0716", "#1a1129", "#2d1f48", "#55408a"]), sash: H(["#05030a", "#120a1e"]), shoe: H(["#05030a"])[0],
    eye: H(["#ffffff", "#ffffff"]), tendril: H(["#040208", "#0e0818", "#221639", "#6a54b0"]), crystal: H(["#2a1d44", "#6a54a0", "#c8baf4"]),
    aura: H(["#6a3fd0"])[0], tendrils: 7, crystals: 0, wild: 1.4, glowEyes: true, orb: false, torn: true, arms: "down",
  },
};

export const CAST_RELEASE = 0.35;

export function drawChild(p, t, o, anim, D) {
  const P = PHASES[o.phase] ?? PHASES[1];
  const ph = t / D, bob = Math.sin(ph * TAU) * 1, Y = -4 + bob;
  // cast: hands rise over her head, light bursts from them at the release, then they come back
  let raise = 0;
  if (anim === "cast") {
    raise = t < CAST_RELEASE ? ease.inOutCubic(clamp(t / CAST_RELEASE)) : 1 - ease.inOutCubic(clamp((t - CAST_RELEASE - 0.08) / (D - CAST_RELEASE - 0.08)));
  }
  const R = new Raster(-54, -92, 54, 4);
  p.glow(1, -20 + Y, 30, P.aura, o.phase === 3 ? 0.3 : 0.2);

  // --- crystals behind her (the far half of their orbit)
  const crystals = [];
  for (let i = 0; i < P.crystals; i++) {
    const a = ph * TAU + (i * TAU) / P.crystals, cx = Math.cos(a) * 15, depth = Math.sin(a);
    crystals.push({ x: cx + 1, y: -24 + Y + Math.sin(a * 2 + i) * 3 - depth * 2, depth, s: i % 2 ? 1 : 1.4 });
  }
  const drawCrystal = (c) => {
    const h = 4 * c.s, w = 2 * c.s;
    R.poly([[c.x, c.y - h], [c.x + w, c.y], [c.x, c.y + h * 0.8], [c.x - w, c.y]], (x, y, u, v) => lit(P.crystal, tl(u, v, c.depth * 0.2)), 2);
    R.set(c.x - 0.5, c.y - h * 0.5, P.crystal[2], 2);
  };
  for (const c of crystals) if (c.depth < 0) drawCrystal(c);

  // --- tendrils
  const seeds = [[-6, -22, 3.5], [8, -22, -0.4], [-4, -12, 2.6], [6, -12, 0.5], [-8, -28, 3.9], [10, -28, -0.8], [0, -10, 1.6]];
  for (let i = 0; i < P.tendrils; i++) {
    const [sx, sy, a0] = seeds[i], pts = [];
    let x = sx, y = sy + Y, a = a0;
    const len = o.phase === 3 ? 11 : 9, step = o.phase === 3 ? 2.7 : 2.4;
    for (let k = 0; k <= len; k++) {
      pts.push([x, y]);
      a += Math.sin(ph * TAU * 2 + k * 0.6 + i * 1.9) * 0.36 + (i % 2 ? -0.05 : 0.05);
      x += Math.cos(a) * step; y = Math.min(1, y + Math.sin(a) * step);   // tendrils lie along the floor, never through it
    }
    R.stroke(pts, (u) => (o.phase === 3 ? 5 : 3.4) * (1 - u) + 0.6, (u, b) => lit(P.tendril, clamp(0.2 + b * 0.6 + (1 - u) * 0.1)));
  }

  // --- hair, back: a big soft bob, wilder in runaway
  const wind = (k, amp) => Math.sin(ph * TAU + k) * amp;
  R.ellipse(0.5, -31.5 + Y, 7, 6.4, (x, y, dx, dy) => lit(P.hair, 0.55 - dx * 0.25 - dy * 0.3 + Math.sin(x * 1.4 + y) * 0.08));
  for (let s = 0; s < 6; s++) {   // wavy ends below the bob
    const x0 = -6 + s * 2.2, pts = [];
    for (let k = 0; k <= 5; k++) { const u = k / 5; pts.push([x0 - u * 2.5 + wind(s + u * 3, 0.8 + u), -28 + Y + u * (5 + (s % 3)) ]); }
    R.stroke(pts, (u) => 3 * (1 - u * 0.6), (u, b) => lit(P.hair, clamp(0.3 + s * 0.08 + b * 0.3)));
  }
  if (o.phase === 2) for (let s = 0; s < 5; s++) {   // runaway: locks lifting off and curling back, flicking
    const a0 = -Math.PI / 2 + (s - 2) * 0.7, pts = [];
    for (let k = 0; k <= 6; k++) {
      const u = k / 6, a = a0 + u * (s < 2 ? -1.6 : s > 2 ? 1.6 : 0.4) + wind(s * 1.3, 0.25) * u, r = 5 + u * 6;
      pts.push([0.5 + Math.cos(a) * r, -32 + Y + Math.sin(a) * r]);
    }
    R.stroke(pts, (u) => 2.6 * (1 - u * 0.8), (u, b) => lit(P.hair, clamp(0.3 + b * 0.35)));
  }

  // --- legs and shoes
  R.stroke([[0, -11 + Y], [0, -3.5 + Y]], 1.8, (u, b) => lit(P.skin, 0.5 + b * 0.3));
  R.stroke([[3.5, -11 + Y], [4, -3.5 + Y]], 1.8, (u, b) => lit(P.skin, 0.6 + b * 0.3));
  R.ellipse(0.5, -3 + Y, 1.6, 1, P.shoe); R.ellipse(4.5, -3 + Y, 1.6, 1, P.shoe);

  // --- dress: puffy, flared, scalloped hem (torn and dark in runaway)
  const hem = [];
  for (let x = 10; x >= -8; x -= 1.5) hem.push([x, -10 + Y + (Math.round((x + 8) / 1.5) % 2 ? 0.8 : -0.4) + Math.sin(ph * TAU * 2 + x * 0.5) * 0.6 + (P.torn && Math.round(x) % 3 === 0 ? 1.8 : 0)]);
  R.poly([[-2.5, -21 + Y], [5.5, -21 + Y], [8, -15 + Y], ...hem, [-6.5, -14 + Y]], (x, y, u, v) => lit(P.dress, tl(u, v * 0.8, Math.sin(x * 1.1 + ph * TAU) * 0.1)));
  if (P.torn) for (let i = 0; i < hem.length; i += 2) R.stroke([hem[i], [hem[i][0] - 0.5, hem[i][1] - 2.5]], 1, P.tendril[2]);
  R.poly([[-2, -25 + Y], [5, -25 + Y], [5.5, -20.5 + Y], [-2.5, -20.5 + Y]], (x, y, u, v) => lit(P.dress, tl(u, v, 0.1)));
  R.line(-2.5, -21 + Y, 5.5, -21 + Y, P.sash[0]);   // sash
  R.line(-2, -22 + Y, 5, -22 + Y, P.sash[1]);
  R.set(1.5, -25 + Y, P.sash[1]); R.set(0.5, -24 + Y, P.sash[0]); R.set(2.5, -24 + Y, P.sash[0]);   // collar bow

  // --- arms
  const L = { orb: [[-1.5, -23.5], [0.5, -19], [2.5, -18]], open: [[-1.5, -23.5], [-5, -22], [-8, -24]], down: [[-1.5, -23.5], [-4, -18], [-5, -14]] }[P.arms];
  const Rt = { orb: [[5, -23.5], [6.5, -19.5], [4.5, -18]], open: [[5, -23.5], [9, -22], [12, -24.5]], down: [[5, -23.5], [8, -18], [9, -14]] }[P.arms];
  const lift = (pts, side) => pts.map(([x, y], k) => [lerp(x, k === 0 ? x : x * 0.3 + side * k * 1.5, raise), lerp(y, k === 0 ? y : -27 - k * 6, raise) + Y]);
  const la = lift(L, -1), ra = lift(Rt, 1);
  R.stroke(la, 1.8, (u, b) => lit(P.skin, 0.45 + b * 0.3));
  R.stroke(ra, 1.8, (u, b) => lit(P.skin, 0.6 + b * 0.3));
  if (o.phase === 3) for (const a of [la, ra]) { const [hx, hy] = a[2]; R.line(hx, hy, hx - 1, hy + 2, P.tendril[3]); R.line(hx + 1, hy, hx + 1.5, hy + 2, P.tendril[3]); }   // claws

  // --- the orb she holds (phase 1), rises with the cast
  let orb = null;
  if (P.orb) {
    orb = { x: lerp(3.5, 1.5, raise), y: lerp(-16.5, -42, raise) + Y, r: 3.4 + raise * 1.2 };
    R.ellipse(orb.x, orb.y, orb.r, orb.r, (x, y, dx, dy) => (dx * dx + dy * dy > 0.55 && dx + dy < -0.3 ? P.crystal[1] : P.sash[0]));
    R.set(orb.x - 1.2, orb.y - 1.2, P.crystal[2], 2);
  }

  // --- face, then bangs over it, eyes
  R.ellipse(2, -30.2 + Y, 4.2, 4, (x, y, dx, dy) => lit(P.skin, 0.75 - dx * 0.2 - dy * 0.3));
  R.poly([[-3, -36 + Y], [6.5, -36 + Y], [7, -32 + Y], [5.5, -33.5 + Y], [4, -31.8 + Y], [2.5, -33.5 + Y], [1, -32 + Y], [-0.5, -33.5 + Y], [-2.5, -31 + Y]], (x, y, u, v) => lit(P.hair, tl(u, v, 0.15)));
  const eyes = [[1.8, -30.5], [4.8, -30.5]];
  for (const [ex, ey] of eyes) {
    const tag = P.glowEyes ? 2 : 1;
    R.set(ex, ey + Y, P.eye[0], tag); R.set(ex, ey + 1 + Y, P.eye[0], tag); R.set(ex + 1, ey + 1 + Y, P.eye[0], tag);
    R.set(ex + 1, ey + Y, P.eye[1], 2);
    if (!P.glowEyes) R.set(ex, ey - 1 + Y, P.hair[0]);   // lash
  }
  if (!P.glowEyes) R.set(4, -27.6 + Y, P.skin[0]);   // mouth

  for (const c of crystals) if (c.depth >= 0) drawCrystal(c);

  R.outline(INK, o.phase === 3 ? 0.15 : 0.3);
  R.stamp(p);

  // rim light on the shadow form, glowing eyes, the orb's light, the cast burst
  if (o.phase === 3) p.glow(-1, -34 + Y, 8, P.tendril[3], 0.35);
  if (P.glowEyes) for (const [ex, ey] of eyes) p.glow(ex, ey + Y, 4, P.eye[1], o.phase === 3 ? 0.7 : 0.45);
  if (orb) p.glow(orb.x, orb.y, 8 + raise * 6, P.crystal[1], 0.35 + raise * 0.3);
  for (const c of crystals) p.glow(c.x, c.y, 4, P.crystal[1], 0.2);
  if (anim === "cast") {
    const hx = (la[2][0] + ra[2][0]) / 2, hy = Math.min(la[2][1], ra[2][1]) - 2, f = t - CAST_RELEASE;
    if (raise > 0.3) p.glow(hx, hy, 6 + raise * 6, P.aura, 0.35 * raise);
    if (f >= 0 && f < 0.12) sparkle(p, hx, hy, 6 * (1 - f / 0.12) + 2, P.crystal[2], WHITE, 0.6);
  }
}
