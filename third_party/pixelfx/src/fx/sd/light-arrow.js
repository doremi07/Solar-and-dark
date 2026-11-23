// Solar and Dark: Sola's arrow of light (own work on the pixelfx core; not upstream).
import { fit, rand, sparkle, TAU, WHITE } from "../../core/index.js";
import { beat } from "../projectiles/_lib.js";

export const meta = {
  name: "Light arrow",
  desc: "An arrow of light in flight: a white-hot head rimmed in gold, a shaft of light, fletchings like small wings, a ribbon of light behind shedding motes and glints.",
  tags: ["projectile", "arrow", "holy", "light", "directional", "loop"],
  size: [136, 36],
  origin: [116, 18],
  duration: 0.6,
  loop: true,
  fps: 30,
  bg: "sky",
  options: { palette: ["#3a2410", "#7a5226", "#c08a3e", "#e8b85a", "#f8dc8c", "#fff0c4", "#fffbec", "#ffffff"], length: 38, seed: 5 },
};

export function draw(p, t, o) {
  const C = fit(o.palette, 8), L = o.length, S = o.seed, D = meta.duration, ph = t / D, fr = Math.floor(t * 30 + 1e-6);
  const left = -meta.origin[0] + 2;

  // the ribbon of light behind: a thin wavering band that thins and cools as it trails off
  for (let d = L * 0.55; d < L * 2.1; d += 0.5) {
    const u = (d - L * 0.55) / (L * 1.55), y = 0.5 + Math.sin(d * 0.22 - ph * TAU * 2) * (0.4 + u * 1.6);
    if (-d < left || rand(Math.floor(d), fr) < u * 0.55) continue;
    p.dot(-d, y, u < 0.25 ? C[6] : u < 0.55 ? C[5] : C[4]);
  }
  p.glowLine(-L * 0.5, 0.5, -L * 1.9, 0.5, 6, C[4], 0.22);

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
    for (let k = 0; k < 10; k++) {
      const x = -L + 2 - k, sp = 1.5 + k * 0.5 * fl, y = 0.5 + s * sp;
      p.dot(x, y, k < 4 ? C[6] : C[5]);                                 // the spine
      for (let v = 1.5; v < sp; v += 1) p.dot(x + 1, 0.5 + s * v, k < 5 ? C[4] : C[3]);   // the vane
    }
  }
  p.glow(-L - 4, 0.5, 9, C[4], 0.32 * fl);

  // the head: a solid leaf-shaped point of light, white at its heart, gold at the edges
  p.shade(-6, -5, 8, 6, (x, y) => {
    const d = 7 - x, v = Math.abs(y - 0.5);                              // d: back from the tip
    if (d < 0 || d > 12) return;
    const w = d < 8 ? d * 0.55 : (12 - d) * 1.1;                         // widest 8 px back, then closes on the shaft
    if (v > w) return;
    return v < w - 1.6 ? (d < 6 ? WHITE : C[6]) : v < w - 0.6 ? C[5] : C[3];
  });

  // the light it carries
  const g = 0.8 + 0.2 * Math.sin(ph * TAU * 2);
  sparkle(p, 7, 0.5, 3 + Math.round(g * 2), WHITE, C[5], 0.5);
  p.glow(0, 0.5, 18, C[4], 0.55 * g);
  p.glowLine(-2, 0.5, -L, 0.5, 7, C[4], 0.3);
  p.glow(4, 0.5, 6, WHITE, 0.35);
}
