// Solar and Dark: the bosses' crown of light. Written for this game (not upstream pixelfx).
import { fit, sparkle, TAU, WHITE } from "../../core/index.js";

export const meta = {
  name: "Thorn halo",
  desc: "A front-facing crown of light behind the head: a double ring studded with thorns, four long compass rays and four short ones, breathing, with a glint running round it.",
  tags: ["holy", "halo", "crown", "radiant", "rays", "boss", "aura", "loop"],
  size: [96, 100],
  origin: [48, 52],
  duration: 4,
  loop: true,
  fps: 30,
  bg: "dark",
  options: { palette: ["#1a1020", "#3d2a3a", "#7a5640", "#c0905a", "#e8c08a", "#f8e2b8", "#fff6e4", "#ffffff"], seed: 1 },
};

const R = 20, THORNS = 16;

// a tapered spike from radius r0 out by L at angle a: a white spine, ramp-coloured flanks
function spike(p, C, a, r0, L, w0, k) {
  const ca = Math.cos(a), sa = Math.sin(a), x0 = ca * r0, y0 = sa * r0, x1 = ca * (r0 + L), y1 = sa * (r0 + L), pad = w0 + 1;
  p.shade(Math.min(x0, x1) - pad, Math.min(y0, y1) - pad, Math.max(x0, x1) + pad, Math.max(y0, y1) + pad, (x, y, out) => {
    const along = x * ca + y * sa - r0, u = along / L;
    if (u < 0 || u > 1) return;
    const across = Math.abs(-x * sa + y * ca), w = (w0 / 2) * (1 - u) + 0.3;
    if (across > w) return;
    out.c = across < 0.5 ? (u < 0.5 ? WHITE : C[6]) : u < 0.4 ? C[6] : C[5];
    out.a = u < 0.8 ? k : k * (1 - (u - 0.8) / 0.2) * 1.3;
  });
  p.glowLine(x0, y0, ca * (r0 + L * 0.7), sa * (r0 + L * 0.7), 4, C[5], 0.14 * k);
}

export function draw(p, t, o) {
  const C = fit(o.palette, 8), ph = t / meta.duration, breathe = 0.5 + 0.5 * Math.sin(ph * TAU * 2);
    const rot = ph * ((2 * TAU) / THORNS); // two steps: long and short thorns alternate
  p.glow(0, 0, 44, C[4], 0.18 + 0.06 * breathe);
  p.glowRing(0, 0, R - 2, 5, C[5], 0.16);

  // compass rays: four long (up, down, left, right), four short on the diagonals; they don't turn
  for (let i = 0; i < 8; i++) {
    const a = (i * TAU) / 8 - Math.PI / 2, long = i % 2 === 0;
    const L = (long ? (i === 0 ? 24 : 18) : 9) * (0.9 + 0.1 * breathe);
    spike(p, C, a, R - 1, L, long ? 3.6 : 2.4, 1);
  }

  // the outer ring: thin, cool; the inner ring: brighter, with a darker inside edge
  p.shade(-R - 2, -R - 2, R + 2, R + 2, (x, y, out) => {
    const d = Math.hypot(x, y);
    if (Math.abs(d - R) < 0.6) out.c = C[6];
    else if (Math.abs(d - (R - 1.2)) < 0.5) out.c = C[4];
    else if (Math.abs(d - (R - 4)) < 0.55) {
      // light from the top left: the upper-left arc of the inner ring is white
      const lit = (-x - y) / (d * Math.SQRT2);
      out.c = lit > 0.55 ? WHITE : lit > -0.2 ? C[6] : C[5];
    } else if (Math.abs(d - (R - 5)) < 0.5) out.c = C[2];
    else return;
  });

  // thorns: small hooked spikes outward from the ring, alternating long and short
  for (let i = 0; i < THORNS; i++) {
    const a = rot + (i * TAU) / THORNS, L = i % 2 ? 3 : 5;
    const ca = Math.cos(a), sa = Math.sin(a);
    for (let d = 0; d <= L; d += 0.5) p.px(ca * (R + 1 + d), sa * (R + 1 + d), d > L - 1 ? C[6] : C[5]);
    p.px(ca * (R + 1), sa * (R + 1), C[3]);
  }

  // studs on the inner ring, one per compass ray
  for (let i = 0; i < 8; i++) {
    const a = (i * TAU) / 8 - Math.PI / 2, x = Math.cos(a) * (R - 4), y = Math.sin(a) * (R - 4);
    p.px(x, y, WHITE);
    p.px(x + 1, y, C[5]);
    p.px(x, y + 1, C[4]);
  }

  // a glint runs round the outer ring once per loop and flares where it passes the top ray
  const g = ph * TAU - Math.PI / 2, gx = Math.cos(g) * R, gy = Math.sin(g) * R;
  for (let k = 0; k < 6; k++) {
    const b = g - k * 0.06;
    p.px(Math.cos(b) * R, Math.sin(b) * R, k < 2 ? WHITE : C[6]);
  }
  p.glow(gx, gy, 7, C[6], 0.35);
  const top = Math.pow(Math.max(0, Math.cos(ph * TAU)), 24);
  if (top > 0) sparkle(p, 0, -R - 24, 1 + 4 * top, WHITE, C[6], 0.5);
  // the tip of the top ray always holds a small star
  sparkle(p, 0, -R - 22, 2 + breathe, C[6], C[7], 0.3);
}
