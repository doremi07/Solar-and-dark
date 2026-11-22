// Solar and Dark: Serena, the guardian who became a shadow. Drawn as pixel art in code (see _figure.js).
// She hovers, facing right, sword held point-down in front; her phases are colorways and what trails off her:
//   1 그림자 (shadow): black-violet dress, ragged hem, heavy shadow tendrils, eyes closed
//   2 융합 (fusion): dark bodice over a white underskirt, fewer tendrils, eyes half open
//   3 정화 (purified): white and gold, ribbons of light instead of shadow, eyes open and gold
import { TAU, clamp, ease, lerp, sparkle, WHITE } from "../../core/index.js";
import { Raster, lit, tl, H } from "./_figure.js";

export const INK = H(["#0b0712"])[0];

const PHASES = {
  1: {
    skin: H(["#8e7f9c", "#b9aac4", "#d9cfe0"]), hair: H(["#463a5e", "#64587e", "#8a7eaa", "#b4a9cf"]),
    dress: H(["#0e0918", "#1b1229", "#2b1f42", "#46345f"]), inner: H(["#231a35", "#33284b", "#4a3c68"]),
    gold: H(["#5d4630", "#8c6c45", "#b8956a"]), blade: H(["#231b31", "#4a3f63", "#8d82a8", "#c4bada"]),
    tendril: H(["#07040c", "#140d22", "#2a1d44", "#5a4686"]), eye: H(["#2a1d3a"])[0], aura: H(["#5a3f8c"])[0],
    tendrils: 7, ribbons: 0, eyes: "closed", ragged: true,
  },
  2: {
    skin: H(["#a796b0", "#d2c4d8", "#efe6f0"]), hair: H(["#6a5c8a", "#9184b4", "#bdb1da", "#e6defa"]),
    dress: H(["#140d22", "#251a3a", "#3c2c5c", "#5e4a8a"]), inner: H(["#9c92b8", "#cbc3e0", "#f3efff"]),
    gold: H(["#7a5a34", "#b8894e", "#e8c08a"]), blade: H(["#3a3150", "#8378a6", "#cfc6e8", "#f6f2ff"]),
    tendril: H(["#0a0612", "#1b122e", "#35265a", "#7a64b0"]), eye: H(["#3a2a55"])[0], aura: H(["#8d6fd0"])[0],
    tendrils: 3, ribbons: 1, eyes: "half", ragged: false,
  },
  3: {
    skin: H(["#b7a2b8", "#e6d6e2", "#fff4f8"]), hair: H(["#7d6fa8", "#aa9dd0", "#d9d0ef", "#fbf8ff"]),
    dress: H(["#6f6292", "#a99cc6", "#dcd4ec", "#fbf9ff"]), inner: H(["#c7a878", "#ecd3a4", "#fff3d8"]),
    gold: H(["#a8783e", "#e0b070", "#ffe6b0"]), blade: H(["#b8955a", "#f0d49a", "#fff4d8", "#ffffff"]),
    tendril: H(["#a8783e", "#e0b070", "#ffe6b0", "#ffffff"]), eye: H(["#c08a3a"])[0], aura: H(["#ffd890"])[0],
    tendrils: 0, ribbons: 3, eyes: "open", ragged: false,
  },
};

// how the cast pose moves: wind up overhead, cut forward at 0.35 s, settle back by the end (same as idle at 0 and D)
export const CAST_RELEASE = 0.35;
function pose(anim, t, D) {
  const idle = { hx: 12, hy: -31, a: 1.15, lift: 0, flare: 0 };
  if (anim !== "cast") return idle;
  const up = { hx: 8, hy: -52, a: -2.0, lift: 1, flare: 0.6 }, cut = { hx: 18, hy: -36, a: 0.12, lift: 0.4, flare: 1 };
  const mixP = (a, b, f) => ({ hx: lerp(a.hx, b.hx, f), hy: lerp(a.hy, b.hy, f), a: lerp(a.a, b.a, f), lift: lerp(a.lift, b.lift, f), flare: lerp(a.flare, b.flare, f) });
  if (t < CAST_RELEASE - 0.06) return mixP(idle, up, ease.inOutCubic(clamp(t / (CAST_RELEASE - 0.06))));
  if (t < CAST_RELEASE + 0.05) return mixP(up, cut, ease.outQuart(clamp((t - CAST_RELEASE + 0.06) / 0.11)));
  return mixP(cut, idle, ease.inOutCubic(clamp((t - CAST_RELEASE - 0.05) / (D - CAST_RELEASE - 0.05))));
}

export function drawSerena(p, t, o, anim, D) {
  const P = PHASES[o.phase] ?? PHASES[1];
  const ph = t / D;                       // 0..1 over the loop: every motion uses whole cycles of it
  const bob = Math.sin(ph * TAU) * 1.2, Y = -3 + bob;
  const q = pose(anim, t, D);
  const wind = (y, amp, k = 1, off = 0) => Math.sin(ph * TAU * k + y * 0.13 + off) * amp;
  const R = new Raster(-60, -96, 68, 8);

  // aura behind her
  p.glow(2, -30 + Y, 40, P.aura, o.phase === 3 ? 0.28 : 0.18);

  // --- back layer: tendrils (shadow) or ribbons (light)
  const seeds = [[-14, -8, 2.6], [-9, -6, 2.1], [14, -6, 0.6], [4, -5, 1.6], [-4, -38, 3.6], [-6, -32, 3.3], [9, -8, 1.0]];   // last pair: off her back, curling down like torn wings
  for (let i = 0; i < P.tendrils; i++) {
    const [sx, sy, a0] = seeds[i], pts = [];
    let x = sx, y = sy + Y, a = a0;
    const len = i >= 4 && i < 6 ? 12 : 11, step = 2.4 + (o.phase === 1 ? 0.5 : 0);
    for (let k = 0; k <= len; k++) {
      pts.push([x, y]);
      a += Math.sin(ph * TAU * 2 + k * 0.55 + i * 1.7) * 0.32 + (i % 2 ? 0.06 : -0.06);
      x += Math.cos(a) * step; y = Math.min(1, y + Math.sin(a) * step);   // tendrils lie along the floor, never through it
    }
    R.stroke(pts, (u) => (o.phase === 1 ? 4.2 : 3) * (1 - u) + 0.6, (u, b) => lit(P.tendril, clamp(0.25 + b * 0.6 + (1 - u) * 0.1)));
  }
  for (let i = 0; i < P.ribbons; i++) {
    const pts = [];
    for (let k = 0; k <= 16; k++) {
      const u = k / 16, x = 1 - u * 30 - i * 3, y = -31 + Y + u * 14 + i * 4 + Math.sin(ph * TAU + u * 5 + i) * (2 + u * 4);
      pts.push([x, y]);
    }
    R.stroke(pts, (u) => 2.2 * (1 - u) + 0.5, (u) => lit(P.tendril, 0.95 - u * 0.6), 2);
    p.glowLine(pts[0][0], pts[0][1], pts[16][0], pts[16][1], 5, P.aura, 0.12);
  }

  // --- hair: a cap on the crown, then locks falling down her back in waves, darker behind, lighter in front;
  // they sway on the wind and lift on the wind-up
  R.ellipse(3.5, -52 + Y, 5.4, 5, (x, y, dx, dy) => lit(P.hair, 0.6 - dx * 0.25 - dy * 0.3));
  const LOCKS = 9;
  for (let s = 0; s < LOCKS; s++) {
    const f = s / (LOCKS - 1), len = 30 + Math.sin(s * 2.3) * 6 + f * 6;
    const x0 = lerp(-1.5, 6.5, f), y0 = -54 + Math.abs(f - 0.5) * 3;
    const pts = [];
    for (let k = 0; k <= 12; k++) {
      const u = k / 12, y = y0 + u * len;
      const drift = -u * (10 + (1 - f) * 7) - u * u * 3;                    // fall back and away behind her
      const wave = Math.sin(ph * TAU + u * 4.2 + s * 0.9) * (0.6 + u * 2.4); // ripples travel down the lock
      pts.push([x0 + drift + wave - q.lift * u * 8, y + Y - q.lift * u * 7]);
    }
    const shade = 0.22 + f * 0.5;
    R.stroke(pts, (u) => 4.4 * (1 - u * 0.7), (u, b) => lit(P.hair, clamp(shade + b * 0.35 - u * 0.12)));
  }
  // --- back arm (sleeve, then hand)
  R.stroke([[0, -42 + Y], [-2, -36 + Y], [-1.5, -31 + Y]], 2.6, (u, b) => lit(P.dress, 0.3 + b * 0.5));
  R.ellipse(-1.5, -30.5 + Y, 1.2, 1.2, P.skin[0]);

  // --- skirt: long, flaring back, hem rippling; a slit shows the underskirt
  const flare = q.flare;
  const hem = [];
  for (let x = 15; x >= -15; x -= 2.5) hem.push([x + flare * 2, -5 + Y + Math.sin(ph * TAU * 2 + x * 0.35) * 1.4 - flare * Math.max(0, -x) * 0.15]);
  const skirt = [[0, -31 + Y], [7, -31 + Y], [10, -22 + Y], [13 + flare, -13 + Y], ...hem, [-14 - flare * 3, -8 + Y], [-9, -18 + Y], [-3, -27 + Y]];
  R.poly(skirt, (x, y, u, v) => lit(P.dress, tl(u, v * 0.7, Math.sin(x * 0.85 - y * 0.05 + ph * TAU) * 0.14)));
  R.poly([[6, -30 + Y], [13.5 + flare, -6 + Y], [5, -6 + Y]], (x, y, u, v) => lit(P.inner, tl(u, v, 0.1)));
  if (P.ragged) {
    for (let i = 0; i < hem.length; i++) if (i % 2 === 0) R.stroke([hem[i], [hem[i][0] - 1, hem[i][1] + 2 + (i % 3)]], 1.4, P.dress[0]);
  } else for (let i = 1; i < hem.length; i++) R.line(hem[i - 1][0], hem[i - 1][1] - 1, hem[i][0], hem[i][1] - 1, P.gold[1]);

  // --- bodice with a gold V neckline and waist band
  R.poly([[0, -44 + Y], [7, -44 + Y], [8, -38 + Y], [6.5, -31 + Y], [0.5, -31 + Y], [-0.5, -38 + Y]], (x, y, u, v) => lit(P.dress, tl(u, v, 0.15)));
  R.line(1.5, -44 + Y, 3.5, -40 + Y, P.gold[2]); R.line(3.5, -40 + Y, 5.5, -44 + Y, P.gold[1]);
  R.line(0.5, -31.5 + Y, 6.5, -31.5 + Y, P.gold[1]);
  R.set(3.5, -31.5 + Y, P.gold[2]);
  R.ellipse(7.2, -42 + Y, 2, 1.8, (x, y, dx, dy) => lit(P.dress, 0.55 - dx * 0.2 - dy * 0.2));
  R.poly([[3, -46.5 + Y], [5, -46.5 + Y], [5, -43.5 + Y], [3, -43.5 + Y]], P.skin[0]);   // neck

  // --- head: face, eyes, bangs and a lock falling in front of the shoulder
  R.ellipse(4.5, -50 + Y, 3.6, 4.2, (x, y, dx, dy) => lit(P.skin, 0.75 - dx * 0.25 - dy * 0.3));
  if (P.eyes === "closed") { R.line(4.5, -49.5 + Y, 5.5, -49 + Y, P.eye); R.line(6.8, -49.5 + Y, 7.6, -49 + Y, P.eye); }
  else if (P.eyes === "half") { R.line(4.5, -50 + Y, 5.5, -50 + Y, P.eye); R.line(6.8, -50 + Y, 7.6, -50 + Y, P.eye); R.set(5, -49 + Y, P.eye); R.set(7.2, -49 + Y, P.eye); }
  else { R.set(5, -50 + Y, P.eye); R.set(5, -49 + Y, P.eye); R.set(7.4, -50 + Y, P.eye); R.set(7.4, -49 + Y, P.eye); R.set(5.5, -50 + Y, WHITE, 2); }
  R.set(6.5, -47 + Y, P.skin[0]);
  R.poly([[-0.5, -56.5 + Y], [6.5, -56.5 + Y], [8.5, -52 + Y], [7, -50.5 + Y], [5.5, -52.5 + Y], [4, -51 + Y], [2.5, -53 + Y], [1, -51 + Y], [0, -53 + Y]], (x, y, u, v) => lit(P.hair, tl(u, v, 0.2)));
  R.stroke([[8.2, -52 + Y], [8.6 + wind(-46, 0.6), -46 + Y], [7.6 + wind(-40, 0.9), -40 + Y], [6.6 + wind(-34, 1.2), -34 + Y]], (u) => 2.4 * (1 - u) + 0.7, (u, b) => lit(P.hair, 0.55 + b * 0.4));

  // --- front arm and the sword
  const hx = q.hx, hy = q.hy + Y, ca = Math.cos(q.a), sa = Math.sin(q.a);
  const sx = 7, sy = -42 + Y, mx = (sx + hx) / 2, my = (sy + hy) / 2, ex = mx + 2.5, ey = my + 1.5;
  const tipX = hx + ca * 34, tipY = hy + sa * 34;
  R.stroke([[hx, hy], [tipX, tipY]], (u) => 2.6 - u * 1.4, (u, b) => lit(P.blade, clamp(0.45 + b * 0.5 - u * 0.15)));
  R.line(hx + ca * 2 - sa * 0.8, hy + sa * 2 + ca * 0.8, tipX - sa * 0.6, tipY + ca * 0.6, P.blade[3]);   // lit edge
  R.line(hx - sa * 3.5, hy + ca * 3.5, hx + sa * 3.5, hy - ca * 3.5, P.gold[2]);                             // crossguard
  R.stroke([[hx, hy], [hx - ca * 3.5, hy - sa * 3.5]], 1.6, P.gold[0]);                                      // grip
  R.set(hx - ca * 4.5, hy - sa * 4.5, P.gold[2]);                                                            // pommel
  R.stroke([[sx, sy], [ex, ey], [hx, hy]], 2.6, (u, b) => lit(P.dress, 0.4 + b * 0.5));
  R.ellipse(hx, hy, 1.4, 1.4, (x, y, dx, dy) => lit(P.skin, 0.6 - dy * 0.3));

  R.outline(INK, o.phase === 3 ? 0.4 : 0.3);
  R.stamp(p);

  // light: the blade glows in purity, flashes on the cut
  if (o.phase === 3) p.glowLine(hx, hy, tipX, tipY, 4, P.aura, 0.18);
  if (anim === "cast") {
    const g = clamp((t - (CAST_RELEASE - 0.25)) / 0.25) * (1 - clamp((t - CAST_RELEASE) / 0.08));
    if (g > 0) p.glow(tipX, tipY, 10 + g * 8, P.aura, 0.5 * g);
    const f = t - CAST_RELEASE;
    if (f >= 0 && f < 0.12) {
      sparkle(p, tipX, tipY, 6 * (1 - f / 0.12) + 2, P.blade[2], WHITE, 0.6);
      p.glowLine(hx, hy, tipX, tipY, 6, P.aura, 0.4 * (1 - f / 0.12));
    }
  }
}
