// Solar and Dark: Serena, the guardian who became a shadow. Drawn as pixel art in code (see _figure.js), about
// 120 px tall, after the concept sheets: knee-long silver-lavender hair with a sheen, a high-collared dark gown with
// gold filigree over a white underskirt, bell sleeves, a long ornate sword held point-down, shadow trailing off her.
//   1 그림자 (shadow): the gown near black, hem dissolving into heavy tendrils, eyes closed
//   2 융합 (fusion): white panel and underskirt showing, fewer tendrils, eyes half open
//   3 정화 (purified): white and gold, ribbons of light instead of shadow, eyes open and gold
import { TAU, clamp, ease, lerp, sparkle, WHITE } from "../../core/index.js";
import { Raster, lit, tl, H, curve, strand } from "./_figure.js";

export const INK = H(["#0b0712"])[0];

const PHASES = {
  1: {
    skin: H(["#7e6d8e", "#a998b6", "#cbbdd6", "#e4daea"]),
    hair: H(["#2e2442", "#463a5e", "#62567e", "#8579a4", "#aea3c9", "#d4cbe8"]),
    dress: H(["#07040c", "#0f0a18", "#1a1228", "#271c3c", "#3a2b56", "#544076"]),
    panel: H(["#1a1228", "#271c3c", "#3a2b56", "#4f3d70"]),
    under: H(["#140d22", "#21173a", "#33284f", "#4a3c6a"]),
    gold: H(["#4a3622", "#76583a", "#a68052", "#cfa978"]), gem: H(["#3a2060", "#7a4fc0", "#c8a8ff"]),
    blade: H(["#1d1628", "#3c3252", "#665b82", "#9a90b6", "#cfc8e2"]),
    tendril: H(["#050309", "#0d0816", "#1a1129", "#2c1f45", "#4d3a78", "#8a72c4"]), eye: H(["#2a1d3a", "#5a4680"]),
    aura: H(["#5a3f8c"])[0], tendrils: 8, ribbons: 0, eyes: "closed", ragged: 1,
  },
  2: {
    skin: H(["#94829e", "#bfaec8", "#ddd0e2", "#f2eaf4"]),
    hair: H(["#4a3e68", "#6a5d8c", "#8d80b0", "#b3a7d2", "#d8cfec", "#f6f2ff"]),
    dress: H(["#0a0612", "#140d22", "#211733", "#32254a", "#493868", "#66528e"]),
    panel: H(["#9a90b6", "#c3bad8", "#e2dcf0", "#fbf9ff"]),
    under: H(["#8a80aa", "#b6acd0", "#dcd5ee", "#f8f6ff"]),
    gold: H(["#6a4c2c", "#a07642", "#d0a56a", "#f0d09a"]), gem: H(["#4a2a80", "#9a6ce8", "#e2d0ff"]),
    blade: H(["#3a3150", "#6b6090", "#a49ac4", "#d6cfec", "#f8f5ff"]),
    tendril: H(["#060309", "#110a1d", "#20163a", "#36285c", "#5a4890", "#9a86d8"]), eye: H(["#3a2a55", "#7a62b0"]),
    aura: H(["#8d6fd0"])[0], tendrils: 4, ribbons: 1, eyes: "half", ragged: 0.5,
  },
  3: {
    skin: H(["#b39cb2", "#d8c6d6", "#efe2ec", "#fff6fa"]),
    hair: H(["#6e60a0", "#9184c0", "#b4a9da", "#d6cef0", "#eee9fa", "#ffffff"]),
    dress: H(["#5c5080", "#8577a8", "#ab9fcc", "#cfc6e6", "#ebe6f6", "#ffffff"]),
    panel: H(["#cdb489", "#ead2a4", "#fbecc8", "#fffaf0"]),
    under: H(["#c2b2d6", "#ddd3ec", "#f2edfa", "#ffffff"]),
    gold: H(["#946634", "#c8944e", "#ecc27e", "#fff0c4"]), gem: H(["#b0722e", "#f0b45a", "#fff2c8"]),
    blade: H(["#a8834a", "#d8b674", "#f4dca4", "#fff4d8", "#ffffff"]),
    tendril: H(["#946634", "#c8944e", "#ecc27e", "#fff0c4", "#fffaf0", "#ffffff"]), eye: H(["#9a6a2a", "#e8b860"]),
    aura: H(["#ffd890"])[0], tendrils: 0, ribbons: 4, eyes: "open", ragged: 0,
  },
};

export const CAST_RELEASE = 0.35;
function pose(anim, t, D) {
  const idle = { hx: 24, hy: -64, a: 1.2, lift: 0, flare: 0 };
  if (anim !== "cast") return idle;
  const up = { hx: 14, hy: -104, a: -2.6, lift: 1, flare: 0.6 }, cut = { hx: 36, hy: -78, a: 0.08, lift: 0.4, flare: 1 };
  const m = (a, b, f) => ({ hx: lerp(a.hx, b.hx, f), hy: lerp(a.hy, b.hy, f), a: lerp(a.a, b.a, f), lift: lerp(a.lift, b.lift, f), flare: lerp(a.flare, b.flare, f) });
  if (t < CAST_RELEASE - 0.06) return m(idle, up, ease.inOutCubic(clamp(t / (CAST_RELEASE - 0.06))));
  if (t < CAST_RELEASE + 0.05) return m(up, cut, ease.outQuart(clamp((t - CAST_RELEASE + 0.06) / 0.11)));
  return m(cut, idle, ease.inOutCubic(clamp((t - CAST_RELEASE - 0.05) / (D - CAST_RELEASE - 0.05))));
}

export function drawSerena(p, t, o, anim, D) {
  const P = PHASES[o.phase] ?? PHASES[1];
  const ph = t / D;
  const Y = -8 + Math.sin(ph * TAU) * 1.5;
  const q = pose(anim, t, D);
  const at = (x, y) => [x, y + Y];
  const R = new Raster(-74, -150, 114, 8);

  p.glow(8, -60 + Y, 70, P.aura, o.phase === 3 ? 0.26 : 0.16);

  // ---------------------------------------------------------------- behind her: tendrils / ribbons of light
  const tendrilSeeds = [[-24, -14, 2.8, 52], [-16, -12, 2.4, 44], [30, -10, 0.35, 46], [-30, -24, 3.1, 40], [-6, -78, 3.5, 40], [-10, -66, 3.2, 46], [26, -12, 0.7, 36], [-20, -30, 3.4, 38]];
  for (let i = 0; i < P.tendrils; i++) {
    const [sx, sy, a0, len] = tendrilSeeds[i], ctrl = [];
    let x = sx, y = sy + Y, a = a0;
    for (let k = 0; k <= 6; k++) {
      ctrl.push([x, Math.min(2, y)]);
      a += Math.sin(ph * TAU * 2 + k * 0.7 + i * 1.7) * 0.42 + (i % 2 ? 0.08 : -0.08);
      x += Math.cos(a) * (len / 6); y += Math.sin(a) * (len / 6);
    }
    const pts = curve(ctrl, 5);
    const w0 = (o.phase === 1 ? 8 : 6) * (i < 2 ? 1.1 : 1);
    R.stroke(pts, (u) => w0 * Math.pow(1 - u, 0.9) + 0.6, (u, b, d) => (d > 0.72 && b > 0.05 ? P.tendril[4 + (u < 0.3 ? 1 : 0)] : strand(P.tendril.slice(0, 5), b, d, -0.15 + (1 - u) * 0.1)));
    if (i < 3) {   // a small branch off the bigger ones
      const j = Math.floor(pts.length * 0.45), [bx, by] = pts[j], ba = a0 + (i % 2 ? -0.9 : 0.9);
      R.stroke(curve([[bx, by], [bx + Math.cos(ba) * 8, by + Math.sin(ba) * 8], [bx + Math.cos(ba + 0.6) * 14, Math.min(2, by + Math.sin(ba + 0.6) * 14)]], 4), (u) => 3 * (1 - u) + 0.5, (u, b, d) => strand(P.tendril.slice(0, 5), b, d, -0.1));
    }
  }
  for (let i = 0; i < P.ribbons; i++) {
    const ctrl = [];
    for (let k = 0; k <= 5; k++) {
      const u = k / 5;
      ctrl.push([6 - u * 60 - i * 4, -66 + Y + u * (22 + i * 8) + Math.sin(ph * TAU + u * 4 + i * 1.3) * (2 + u * 7)]);
    }
    const pts = curve(ctrl, 5);
    R.stroke(pts, (u) => 3 * (1 - u) + 0.6, (u, b, d) => (d < 0.4 ? P.tendril[5] : P.tendril[2 + Math.round(b + 1)]), 2);
    p.glowLine(pts[0][0], pts[0][1], pts[pts.length - 1][0], pts[pts.length - 1][1], 6, P.aura, 0.1);
  }

  // ---------------------------------------------------------------- back hair: a dark volume, then long locks over it
  const massSway = (u) => Math.sin(ph * TAU + u * 3) * u * 3;
  R.poly([at(-2, -108), at(14, -110), at(15, -96), [6 + massSway(0.3) - q.lift * 4, -80 + Y - q.lift * 4], [-6 + massSway(0.6) - q.lift * 9, -58 + Y - q.lift * 8],
          [-18 + massSway(0.9) - q.lift * 13, -40 + Y - q.lift * 11], [-30 + massSway(1) - q.lift * 15, -38 + Y - q.lift * 12], [-26 + massSway(0.8), -56 + Y], [-14, -80 + Y], [-7, -98 + Y]],
         (x, y, u, v) => lit(P.hair, 0.12 + Math.sin(x * 1.1 - y * 0.25) * 0.08));
  const LOCKS = 15;
  for (let s = 0; s < LOCKS; s++) {
    const f = s / (LOCKS - 1);
    const len = 62 + Math.sin(s * 2.1) * 9 + (1 - f) * 8;
    const x0 = lerp(-1, 13, f), y0 = -104 + Math.abs(f - 0.55) * 6;
    const ctrl = [];
    for (let k = 0; k <= 5; k++) {
      const u = k / 5, y = y0 + u * len;
      const drift = -u * (14 + (1 - f) * 14) - u * u * 6;
      const wave = Math.sin(ph * TAU + u * 3.6 + s * 0.8) * (0.6 + u * 3.6);
      ctrl.push([x0 + drift + wave - q.lift * u * 14, y + Y - q.lift * u * 12]);
    }
    const shade = -0.32 + f * 0.36;   // back locks darker
    R.stroke(curve(ctrl, 5), (u) => (7.5 - f * 1.5) * (1 - u * 0.7) + 0.5, (u, b, d) => strand(P.hair, b, d, shade + (u > 0.85 ? -0.12 : 0)));
  }

  // ---------------------------------------------------------------- far arm in its bell sleeve
  const farSh = at(2, -84), farEl = at(-2, -74), farHd = at(0, -63);
  R.stroke([farSh, farEl, farHd], (u) => 4 + u * 2.6, (u, b, d) => strand(P.dress, b, d, -0.2));
  R.line(farHd[0] - 3, farHd[1] - 1, farHd[0] + 3, farHd[1] - 1, P.gold[1]);
  R.ellipse(farHd[0], farHd[1] + 1.5, 1.8, 2, (x, y, dx, dy) => lit(P.skin, 0.4 - dy * 0.2));

  // ---------------------------------------------------------------- the skirt: outer gown split over the underskirt
  const flare = q.flare;
  const hemPt = (x) => [x + flare * 3, -11 + Y + Math.sin(ph * TAU * 2 + x * 0.22) * 2 - flare * Math.max(0, -x) * 0.12];
  const hem = [];
  for (let x = 32; x >= -30; x -= 2) hem.push(hemPt(x));
  R.poly([at(10, -66), at(17, -66), [33 + flare * 3, -12 + Y], [8, -10 + Y]], (x, y, u, v) => lit(P.under, tl(u * 0.6, v * 0.6, 0.1 + Math.sin(x * 0.7) * 0.06)));
  const back = [at(3, -66), at(12, -66), [14 + flare, -40 + Y], [10, -11 + Y], ...hem.filter(([x]) => x < 10), [-30 - flare * 4, -16 + Y], [-18, -36 + Y], [-4, -56 + Y]];
  R.poly(back, (x, y, u, v) => {
    const fold = Math.sin((x - y * 0.3) * 0.32 + ph * TAU * 0.5);
    return lit(P.dress, tl(u * 0.8, v * 0.7, fold * 0.12 - 0.05));
  });
  R.poly([at(15, -66), at(17, -66), [34 + flare * 3, -13 + Y], [28 + flare * 2, -11 + Y]], (x, y, u, v) => lit(P.dress, tl(u, v, 0.05)));
  for (let k = 0; k < 6; k++) {
    const f = k / 5, x0 = lerp(4, 11, f), x1 = lerp(-26, 8, f) + flare * 2, y1 = -12 + Y + Math.sin(ph * TAU * 2 + x1 * 0.22) * 2;
    if (k % 2) continue;   // three folds, not a fan
    R.line(x0, -56 + Y, x1, y1 - 2, P.dress[1]);
    R.line(x0 + 1, -54 + Y, x1 + 1.5, y1 - 3, P.dress[3]);
    R.line(x0 + 2, -52 + Y, x1 + 2.5, y1 - 4, P.dress[2]);
  }
  R.line(12, -65 + Y, 10, -12 + Y, P.gold[2]); R.line(16, -65 + Y, 30 + flare * 3, -12 + Y, P.gold[2]);
  R.line(11, -64 + Y, 9, -12 + Y, P.gold[0]);
  if (P.ragged < 1) for (let i = 1; i < hem.length; i++) R.line(hem[i - 1][0], hem[i - 1][1] - 1, hem[i][0], hem[i][1] - 1, P.gold[1 + (i % 4 === 0 ? 1 : 0)]);
  if (P.ragged > 0) {
    for (let i = 0; i < hem.length; i += 1) {
      if (P.ragged < 1 && i % 3) continue;
      const [hx, hy] = hem[i], L = 4 + ((i * 7) % 5) * P.ragged * 2 + Math.sin(ph * TAU * 2 + i) * 1.5;
      R.stroke(curve([[hx, hy - 1], [hx - 1.5, hy + L * 0.5], [hx - 3 + Math.sin(ph * TAU + i) * 2, Math.min(3, hy + L)]], 3), (u) => 2.8 * (1 - u) + 0.4, (u, b, d) => strand(P.tendril.slice(0, 5), b, d, -0.05));
    }
  } else {
    for (let x = 10; x < 32; x += 3) { const [hx, hy] = hemPt(x); R.ellipse(hx, hy + 0.5, 1.6, 1.2, P.under[3]); R.set(hx, hy + 0.5, P.under[1]); }
  }

  // ---------------------------------------------------------------- bodice, collar, filigree
  R.poly([at(1, -87), at(19, -87), at(18, -80), at(15.5, -67), at(4.5, -67), at(2, -80)], (x, y, u, v) => lit(P.dress, tl(u, v, 0.12)));
  R.poly([at(8, -86), at(12, -86), at(12.5, -67), at(7.5, -67)], (x, y, u, v) => lit(P.panel, tl(u, v, 0.1)));
  R.line(7.5, -86 + Y, 7.5, -68 + Y, P.gold[1]); R.line(12.5, -86 + Y, 12.5, -68 + Y, P.gold[1]);
  for (const [dx, dy, c] of [[0, -3, 3], [0, -2, 3], [0, -1, 2], [0, 0, 3], [0, 1, 2], [0, 2, 1], [-1, -1, 2], [1, -1, 2], [-2, -1, 1], [2, -1, 1]]) R.set(10 + dx, -79 + dy + Y, P.gold[c]);
  R.poly([at(4, -69), at(16, -69), at(16, -66), at(4, -66)], (x, y, u, v) => lit(P.gold, 0.4 + (1 - v) * 0.5));
  R.ellipse(10, -67.5 + Y, 1.8, 1.8, (x, y, dx, dy) => lit(P.gem, 0.5 - dx * 0.3 - dy * 0.3));
  R.set(9.5, -68.5 + Y, P.gem[2]);
  R.poly([at(5, -92), at(15, -92), at(16, -87), at(4, -87)], (x, y, u, v) => lit(P.dress, tl(u, v, 0.2)));
  R.line(5, -92 + Y, 15, -92 + Y, P.gold[3]); R.line(4, -87.5 + Y, 16, -87.5 + Y, P.gold[1]);
  R.ellipse(18, -85 + Y, 3.5, 3, (x, y, dx, dy) => lit(P.dress, 0.62 - dx * 0.2 - dy * 0.25));
  R.ellipse(2, -85 + Y, 3.2, 2.8, (x, y, dx, dy) => lit(P.dress, 0.45 - dx * 0.2 - dy * 0.25));
  R.line(15.5, -87.5 + Y, 21, -84 + Y, P.gold[2]);

  // ---------------------------------------------------------------- head
  R.poly([at(7, -94), at(13, -94), at(13, -90), at(7, -90)], P.skin[1]);
  R.line(7, -91 + Y, 13, -91 + Y, P.skin[0]);
  const face = [at(3.5, -106), at(17, -106), at(18, -100), at(17, -95.5), at(13.5, -92.5), at(10.5, -92.5), at(6, -95.5), at(4, -100)];
  R.poly(face, (x, y, u, v) => lit(P.skin, 0.95 - u * 0.25 - v * 0.35 - (x < 6 ? 0.2 : 0)));
  const eye = (cx, cy, w) => {
    if (P.eyes === "closed") {
      R.line(cx - w, cy, cx + w, cy + 0.5, P.eye[0]); R.set(cx - w - 1, cy - 0.5, P.eye[0]); R.set(cx + w, cy + 1.5, P.eye[0]);
    } else {
      const open = P.eyes === "half" ? 1 : 2;
      R.line(cx - w - 0.5, cy - open, cx + w + 0.5, cy - open, P.eye[0]);
      for (let yy = 0; yy < open + 1; yy++) for (let xx = -w; xx <= w; xx++) R.set(cx + xx, cy - open + 1 + yy, yy === 0 ? P.eye[0] : P.eye[1]);
      R.set(cx + w - 0.5, cy - open + 1, WHITE, 2);
      R.set(cx + w + 1, cy - open - 0.5, P.eye[0]);
    }
  };
  eye(14, -99 + Y, 1.5); eye(7.5, -99 + Y, 1);
  R.set(16.5, -97 + Y, P.skin[0]);
  R.line(13, -94.8 + Y, 14.5, -94.8 + Y, P.skin[0]);
  if (o.phase === 3) { R.set(15.5, -96.5 + Y, H(["#e8b8c8"])[0]); R.set(8, -96.5 + Y, H(["#e8b8c8"])[0]); }
  R.ellipse(9.5, -106 + Y, 10.5, 9.5, (x, y, dx, dy) => {
    if (dy > 0.25 && x > 4 && x < 17) return undefined;
    const ring = Math.abs(dy + 0.4 + dx * dx * 0.3) < 0.07 && dx > -0.65 && dx < 0.55;
    return ring ? P.hair[5] : lit(P.hair, 0.5 - dx * 0.25 - dy * 0.25 + Math.sin(x * 1.5 - y * 0.4) * 0.07);
  });
  for (let k = 0; k < 6; k++) {
    const x = 4.5 + k * 2.4, tip = -98.5 - (k % 2) * 2 - (k === 2 ? 1 : 0);
    R.poly([[x - 1.8, -106 + Y], [x + 1.8, -106 + Y], [x + 0.6, tip + Y]], (xx, yy, u, v) => lit(P.hair, 0.75 - u * 0.25 - v * 0.2 + (k % 2) * 0.05));
  }
  for (const [x0, len, sh] of [[17.5, 36, 0.12], [15.5, 26, 0.0], [4.5, 22, -0.1]]) {
    const ctrl = [];
    for (let k = 0; k <= 4; k++) { const u = k / 4; ctrl.push([x0 + Math.sin(u * 2.4) * 2.5 + Math.sin(ph * TAU + u * 3 + x0) * u * 1.4 - q.lift * u * 3, -103 + u * len + Y - q.lift * u * 4]); }
    R.stroke(curve(ctrl, 5), (u) => 4 * (1 - u * 0.75) + 0.5, (u, b, d) => strand(P.hair, b, d, sh));
  }

  // ---------------------------------------------------------------- near arm and the sword
  const hx = q.hx, hy = q.hy + Y, ca = Math.cos(q.a), sa = Math.sin(q.a), nx = -sa, ny = ca;
  const BL = 66, tipX = hx + ca * BL, tipY = hy + sa * BL;
  R.stroke([[hx + ca * 3, hy + sa * 3], [tipX, tipY]], (u) => 4.4 - u * 3, (u, b, d) => (d < 0.25 ? P.blade[1] : strand(P.blade, b, d, 0.05 - u * 0.1)));
  R.line(hx + ca * 5, hy + sa * 5, hx + ca * BL * 0.7, hy + sa * BL * 0.7, P.blade[1]);
  R.line(hx + ca * 4 + nx * 1.6, hy + sa * 4 + ny * 1.6, tipX + nx * 0.4, tipY + ny * 0.4, P.blade[4]);
  const gx = hx + ca * 2, gy = hy + sa * 2;
  R.stroke([[gx - nx * 7, gy - ny * 7], [gx + nx * 7, gy + ny * 7]], 2.2, (u, b, d) => strand(P.gold, b, d, 0.1));
  for (const s of [-1, 1]) {
    const ex = gx + nx * 7 * s, ey = gy + ny * 7 * s;
    R.stroke(curve([[ex, ey], [ex + ca * 2, ey + sa * 2], [ex + ca * 3 - nx * s * 2, ey + sa * 3 - ny * s * 2]], 3), 1.4, P.gold[2]);
  }
  R.ellipse(gx, gy, 1.6, 1.6, (x, y, dx, dy) => lit(P.gem, 0.6 - dx * 0.3 - dy * 0.3));
  for (let k = 1; k <= 7; k++) R.set(hx - ca * k, hy - sa * k, k % 2 ? P.gold[0] : P.dress[3]);
  R.ellipse(hx - ca * 8.5, hy - sa * 8.5, 1.8, 1.8, (x, y, dx, dy) => lit(P.gem, 0.55 - dx * 0.3 - dy * 0.3));
  const sh = at(18, -84), el = [lerp(sh[0], hx, 0.5) + 4, lerp(sh[1], hy, 0.5) + 2];
  R.stroke([sh, el, [hx - ca * 1, hy - sa * 1]], (u) => 4.4 + u * 1.8, (u, b, d) => strand(P.dress, b, d, 0.05));
  R.stroke([[hx - ca * 2.5 - nx * 3, hy - sa * 2.5 - ny * 3], [hx - ca * 2.5 + nx * 3, hy - sa * 2.5 + ny * 3]], 1.6, P.gold[2]);
  R.ellipse(hx - ca * 0.5, hy - sa * 0.5, 2, 2, (x, y, dx, dy) => lit(P.skin, 0.65 - dx * 0.2 - dy * 0.3));

  R.outline(INK, o.phase === 3 ? 0.42 : 0.3);
  R.stamp(p);

  if (o.phase === 3) { p.glowLine(hx, hy, tipX, tipY, 5, P.aura, 0.2); p.glow(10, -67 + Y, 6, P.gem[1], 0.3); }
  else p.glow(10, -67.5 + Y, 4, P.gem[1], 0.25);
  if (anim === "cast") {
    const g = clamp((t - (CAST_RELEASE - 0.25)) / 0.25) * (1 - clamp((t - CAST_RELEASE) / 0.08));
    if (g > 0) p.glow(tipX, tipY, 14 + g * 10, P.aura, 0.5 * g);
    const f = t - CAST_RELEASE;
    if (f >= 0 && f < 0.12) {
      sparkle(p, tipX, tipY, 8 * (1 - f / 0.12) + 2, P.blade[3], WHITE, 0.6);
      p.glowLine(hx, hy, tipX, tipY, 8, P.aura, 0.45 * (1 - f / 0.12));
    }
  }
}
