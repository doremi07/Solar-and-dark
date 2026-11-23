// Solar and Dark: a red sword wave, a crescent of blade energy flying forward (own work on the pixelfx core).
import { fit, rand, sparkle, TAU, WHITE } from "../../core/index.js";
import { beat } from "../projectiles/_lib.js";

export const meta = {
  name: "Red sword wave",
  desc: "A crescent of red blade energy flying forward: a white-hot cutting edge, a crimson body sweeping back to sharp horns, a dithered smear of motion behind, embers.",
  tags: ["projectile", "slash", "sword", "wave", "red", "directional", "loop"],
  size: [128, 80],
  origin: [96, 40],
  duration: 0.6,
  loop: true,
  fps: 30,
  bg: "sky",
  options: { palette: ["#1e0408", "#4a0a12", "#82121e", "#c01e28", "#ec3a36", "#ff7a5c", "#ffc4a8", "#ffffff"], height: 30, seed: 3 },
};

// the crescent: the outer edge is an arc of radius R (centre behind), the inner edge an arc of radius R
// offset back by `thick`, so the band is thickest in the middle and meets in sharp horns
function crescent(p, C, cx, R, thick, H, a, pulse) {
  const hb = H + 1;
  p.shade(cx - R - 1, -hb, cx + 2, hb, (x, y) => {
    const yy = y - 0.5;
    if (Math.abs(yy) > H) return;
    const xo = cx - R + Math.sqrt(Math.max(0, R * R - yy * yy));            // the outer (front) edge at this row
    const xi = xo - thick * Math.sqrt(Math.max(0, 1 - (yy / H) ** 2));     // the inner edge
    if (x > xo || x < xi) return;
    const w = xo - xi, u = (xo - x) / Math.max(0.5, w);                     // 0 at the cutting edge, 1 at the back
    if (a < 1) return u < 0.35 ? C[a > 0.5 ? 5 : 4] : u < 0.75 ? C[a > 0.5 ? 3 : 2] : C[1];   // an echo: solid, dimmer
    if (u < 0.18 || w < 1.2) return pulse ? WHITE : C[6];
    if (u < 0.4) return C[5];
    if (u < 0.7) return C[4];
    return u < 0.9 ? C[3] : C[2];
  });
}

export function draw(p, t, o) {
  const C = fit(o.palette, 8), H = o.height, S = o.seed, D = meta.duration, ph = t / D, fr = Math.floor(t * 30 + 1e-6);
  const left = -meta.origin[0] + 2, wob = Math.sin(ph * TAU * 2);

  // embers and slivers shed from its back, streaming away and cooling
  beat(t, D, 16, S, (r) => r.range(0.22, 0.4), (age, life, r) => {
    const k = age / life, yy = r.range(-H, H) * 0.8, x0 = -16 - Math.abs(yy) * 0.3;
    const x = x0 - age * r.range(90, 160), y = 0.5 + yy + r.range(-1, 1) * age * 30;
    if (x < left) return;
    if (r.chance(0.3)) p.line(x, y, x - 3 * (1 - k) - 1, y, k < 0.4 ? C[6] : C[4]);
    else p.dot(x, y, k < 0.3 ? C[6] : k < 0.6 ? C[5] : k < 0.85 ? C[4] : C[3]);
  });

  // the wave: a sharp crescent whose horns sweep back, and behind it the smear of its own motion, fading back
  // through dark reds in an ordered dither (no separate echoes)
  const R = H + 1, pulse = ((ph * 2) % 1) < 0.18, h = H + Math.round(wob * 0.6);
  p.shade(-R - 26, -h - 1, 2, h + 1, (x, y) => {
    const yy = y - 0.5;
    if (Math.abs(yy) > h) return;
    const xo = -R + Math.sqrt(Math.max(0, R * R - yy * yy));
    const back = 13 * Math.pow(Math.max(0, 1 - (yy / h) ** 2), 0.8);  // the smear is longest in the middle, nothing at the horns
    const u = (xo - x) / Math.max(1, back);
    if (u < 0 || u > 1) return;
    const bayer = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5][(Math.floor(x) & 3) + (Math.floor(y) & 3) * 4] / 16;
    if (u > 0.35 && bayer < (u - 0.35) / 0.65) return;
    return u < 0.35 ? C[3] : u < 0.65 ? C[2] : C[1];
  });
  // speed lines: three short streaks raking back from the blade, hot at the blade and cooling to nothing
  for (const [yy, len, off] of [[-0.45, 9, 0], [0.05, 13, 0.4], [0.5, 7, 0.75]]) {
    const y = Math.round(0.5 + yy * h), xo = -R + Math.sqrt(Math.max(0, R * R - (yy * h) ** 2)) - 6;
    const sh = ((ph * 2 + off) % 1) * 6;
    for (let k = 0; k < len; k++) p.dot(xo - sh - k, y, k < len * 0.3 ? C[4] : k < len * 0.65 ? C[3] : C[2]);
  }
  crescent(p, C, 0, R, 5, h, 1, pulse);

  // a dark hairline on its back edge, so the blade reads crisp against the smear and any background
  for (let yy = -h + 2; yy <= h - 2; yy++) {
    const xo = -R + Math.sqrt(Math.max(0, R * R - yy * yy)), xi = xo - 5 * Math.sqrt(Math.max(0, 1 - (yy / h) ** 2));
    p.dot(Math.floor(xi) - 1, yy + 0.5, C[1]);
  }
  // a glint travelling the cutting edge from horn to horn, and the horns' tips catching the light
  const gy = -h + ((ph * 2) % 1) * 2 * h, gx = -R + Math.sqrt(Math.max(0, R * R - gy * gy));
  sparkle(p, gx, gy + 0.5, 2, WHITE, C[6], 0.5);
  for (const sgn of [-1, 1]) {
    const ty = sgn * (h - 0.5), tx = -R + Math.sqrt(Math.max(0, R * R - ty * ty));
    p.dot(tx, ty + 0.5, WHITE);
    p.glow(tx, ty + 0.5, 3, C[5], 0.4);
  }

  // the light: a crimson bloom on the body, a hot core at the centre of the edge
  p.glow(-4, 0.5, H * 1.15, C[3], 0.5);
  p.glow(1, 0.5, 7, C[6], 0.35);
  sparkle(p, 1, 0.5 + wob * 2, 3, WHITE, C[6], 0.45);

}
