// Solar and Dark: a red sword wave, a crescent of blade energy flying forward (own work on the pixelfx core).
import { fit, rand, sparkle, TAU, WHITE } from "../../core/index.js";
import { beat } from "../projectiles/_lib.js";

export const meta = {
  name: "Red sword wave",
  desc: "A crescent of red blade energy flying forward: a white-hot cutting edge, a crimson body thinning to sharp horns, two fainter echoes behind, embers shed from its back.",
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
    const k = age / life, yy = r.range(-H, H) * 0.9, x0 = -Math.abs(yy) * 0.45 - 6;
    const x = x0 - age * r.range(90, 160), y = 0.5 + yy + r.range(-1, 1) * age * 30;
    if (x < left) return;
    if (r.chance(0.3)) p.line(x, y, x - 3 * (1 - k) - 1, y, k < 0.4 ? C[6] : C[4]);
    else p.dot(x, y, k < 0.3 ? C[6] : k < 0.6 ? C[5] : k < 0.85 ? C[4] : C[3]);
  });

  // two after-crescents, fainter and further back, breaking up as they fade
  p.glowLine(-34, 0.5, -10, 0.5, H * 1.4, C[2], 0.18);
  crescent(p, C, -24, 40, 3, Math.round(H * 0.8), 0.35, false);
  crescent(p, C, -13, 40, 5, Math.round(H * 0.9), 0.65, false);

  // the wave: a pulse of white runs over its edge twice a loop; it breathes a pixel
  const pulse = ((ph * 2) % 1) < 0.18;
  crescent(p, C, 0, 40 + wob, 9, H + Math.round(wob * 0.6), 1, pulse);

  // the light: a red bloom on the body, a hot glint on the edge and at each horn
  p.glow(-3, 0.5, H * 1.1, C[3], 0.55);
  p.glow(1, 0.5, 8, C[6], 0.35);
  sparkle(p, 1, 0.5 + wob * 2, 3, WHITE, C[6], 0.45);
  for (const s of [-1, 1]) p.glow(-H * H / 80, 0.5 + s * H, 4, C[5], 0.35);
}
