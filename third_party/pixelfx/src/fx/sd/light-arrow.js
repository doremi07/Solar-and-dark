// Solar and Dark: Sola's arrow of light (own work on the pixelfx core; not upstream).
import { fit, rand, sparkle, TAU, WHITE } from "../../core/index.js";
import { beat } from "../projectiles/_lib.js";

export const meta = {
  name: "Light arrow",
  desc: "An arrow of light in flight: a white-hot head rimmed in gold, a shaft of light, fletchings like small wings, a ribbon of light behind shedding motes and glints.",
  tags: ["projectile", "arrow", "holy", "light", "directional", "loop"],
  size: [96, 32],
  origin: [80, 16],
  duration: 0.6,
  loop: true,
  fps: 30,
  bg: "sky",
  options: { palette: ["#3a2410", "#7a5226", "#c08a3e", "#e8b85a", "#f8dc8c", "#fff0c4", "#fffbec", "#ffffff"], length: 22, seed: 5 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 8), L = o.length, S = o.seed, D = meta.duration, ph = t / D, fr = Math.floor(t * 30 + 1e-6);
  const left = -meta.origin[0] + 2;

  // the trail: a single ribbon of light, 3 px at the fletching thinning to a thread, rippling gently and cooling
  for (let d = L * 0.75; d < L * 2.3; d += 0.5) {
    const u = (d - L * 0.75) / (L * 1.55), y = 0.5 + Math.sin(d * 0.2 - ph * TAU * 2) * u * 1.4;
    if (-d < left || (u > 0.55 && rand(Math.floor(d), fr) < (u - 0.55) * 1.6)) continue;
    p.dot(-d, y, u < 0.3 ? WHITE : u < 0.6 ? C[6] : C[5]);
    if (u < 0.5) { p.dot(-d, y - 1, C[5]); p.dot(-d, y + 1, C[4]); }     // lit above, a step darker below
  }
  p.glowLine(-L * 0.75, 0.5, -L * 2.1, 0.5, 6, C[4], 0.25);

  // motes shed off the shaft: they drift back and fade; a few wink as glints
  beat(t, D, 12, S, (r) => r.range(0.2, 0.34), (age, life, r) => {
    const k = age / life, x = -r.range(4, L) - age * r.range(110, 170), y = 0.5 + r.range(-1, 1) * (1 + age * 18);
    if (x < left) return;
    if (r.chance(0.25) && k < 0.5) sparkle(p, x, y, 2, WHITE, C[5], 0.25 * (1 - k));
    else p.dot(x, y, k < 0.4 ? C[6] : k < 0.75 ? C[5] : C[3]);
  });

  // the shaft: a 1 px white core in a gold sheath (3 px), brightest toward the head
  p.shade(-L, -2, -6, 3, (x, y) => {
    const u = -x / L, v = Math.abs(y - 0.5);
    if (v > 1.5) return;
    if (v < 0.5) return u < 0.7 ? WHITE : C[6];
    return u < 0.45 ? C[5] : C[4];
  });

  // fletchings of light at the tail: two feathers swept back, a bright spine and a soft vane, flickering
  const fl = 0.85 + 0.15 * Math.sin(ph * TAU * 3);
  for (const s of [-1, 1]) {
    for (let k = 0; k < 7; k++) {
      const x = -L + 2 - k, sp = 1.5 + k * 0.55 * fl, y = 0.5 + s * sp;
      p.dot(x, y, k < 3 ? C[6] : C[5]);                                 // the spine
      for (let v = 1.5; v < sp; v += 1) p.dot(x + 1, 0.5 + s * v, k < 4 ? C[4] : C[3]);   // the vane
    }
  }
  p.glow(-L - 4, 0.5, 9, C[4], 0.32 * fl);

  // the head: a solid leaf-shaped point of light, white at its heart, gold at the edges
  p.shade(-6, -5, 8, 6, (x, y) => {
    const d = 7 - x, v = Math.abs(y - 0.5);                              // d: back from the tip
    if (d < 0 || d > 12) return;
    const w = d < 8 ? d * 0.55 : (12 - d) * 1.1;                         // widest 8 px back, then closes on the shaft
    if (v > w) return;
    const lower = y > 0.5;                                                 // lit from above: the lower facet a step down
    return v < w - 1.6 ? (d < 6 ? (lower ? C[6] : WHITE) : lower ? C[5] : C[6]) : v < w - 0.6 ? (lower ? C[4] : C[5]) : C[3];
  });

  // two motes of light spiralling the shaft, hidden as they pass behind it
  for (const sgn of [0, Math.PI]) {
    for (let d = 2; d < L - 2; d += 0.5) {
      const a = d * 0.5 - ph * TAU * 3 + sgn, y = 0.5 + Math.sin(a) * 2.4;
      if (Math.cos(a) > 0.92) p.dot(-d, y, WHITE);
      else if (Math.cos(a) > 0.75) p.dot(-d, y, C[5]);
    }
  }
  // a lens flare on the head: a thin horizontal streak and a small cross
  const fl2 = 0.7 + 0.3 * Math.sin(ph * TAU * 2);
  p.glowLine(-8, 0.5, 16, 0.5, 2, WHITE, 0.25 * fl2);
  p.line(9, 0.5, 13, 0.5, C[6]);

  // the light it carries
  const g = 0.8 + 0.2 * Math.sin(ph * TAU * 2);
  sparkle(p, 7, 0.5, 3 + Math.round(g * 2), WHITE, C[5], 0.5);
  p.glow(0, 0.5, 18, C[4], 0.55 * g);
  p.glowLine(-2, 0.5, -L, 0.5, 7, C[4], 0.3);
  p.glow(4, 0.5, 6, WHITE, 0.35);
}
