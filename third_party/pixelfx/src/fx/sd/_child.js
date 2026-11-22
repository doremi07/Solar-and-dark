// Solar and Dark: the Hidden Child (움켜쥔 아이), the final boss. Drawn as pixel art in code (see _figure.js), about
// 80 px tall, after the concept sheets: a small girl with long wavy white-lavender hair, big eyes, a white frilled dress
// with a black ribbon, cradling a dark orb, faceted crystals orbiting her.
//   1 억눌린 빛 (held-in light): calm, white dress, the orb held to her chest
//   2 폭주 (runaway): hair lifting wild, eyes glowing, hem torn dark, arms flung open, tendrils, more crystals
//   3 완전한 그림자 (full shadow): her body gone to shadow with a pale rim, white eyes, great tendrils behind
import { TAU, clamp, ease, lerp, sparkle, WHITE } from "../../core/index.js";
import { Raster, lit, tl, H, curve, strand, grid } from "./_figure.js";
import { INK } from "./_serena.js";

const PHASES = {
  1: {
    skin: H(["#b09aae", "#d6c4d2", "#eedfe8", "#fdf4f8"]), hair: H(["#5a4c86", "#7d70ac", "#a296cc", "#c5bce4", "#e2dcf4", "#fbf8ff"]),
    dress: H(["#8f86b2", "#b4acd2", "#d6d0ea", "#eeebf8", "#ffffff"]), ribbon: H(["#0e0818", "#1e1430", "#34264e"]), shoe: H(["#1a1028", "#2e2244", "#4a3a66"]),
    eye: H(["#2e1f5a", "#6a4fb0", "#a88cf0"]), tendril: H(["#050309", "#0d0816", "#1a1129", "#2c1f45", "#4d3a78", "#8a72c4"]),
    crystal: H(["#2c1e58", "#4f3a96", "#7c66c8", "#b4a4ec", "#ece6ff"]), orbC: H(["#06030c", "#130a24", "#2a1a4c", "#6a4fb0", "#b9a6f4"]),
    aura: H(["#b9a6f4"])[0], tendrils: 0, crystals: 4, wild: 0, glowEyes: false, orb: true, torn: 0, arms: "orb",
  },
  2: {
    skin: H(["#9c869c", "#c6b2c2", "#e2d2dc", "#f6ebf2"]), hair: H(["#463a70", "#665898", "#8a7cbc", "#b0a4da", "#d6cef0", "#f4f0ff"]),
    dress: H(["#5e5282", "#857aa8", "#aca2ca", "#d0c8e6", "#ece8f8"]), ribbon: H(["#08040e", "#160e24", "#2a1d40"]), shoe: H(["#120a1e", "#241838", "#3a2c58"]),
    eye: H(["#9a6cf0", "#d4b8ff", "#ffffff"]), tendril: H(["#050309", "#100a1c", "#1e1436", "#342658", "#5a4890", "#9a86d8"]),
    crystal: H(["#2c1e58", "#4f3a96", "#7c66c8", "#b4a4ec", "#ece6ff"]), orbC: H(["#06030c", "#130a24", "#2a1a4c", "#6a4fb0", "#b9a6f4"]),
    aura: H(["#9a6cf0"])[0], tendrils: 5, crystals: 6, wild: 1, glowEyes: true, orb: false, torn: 1, arms: "open",
  },
  3: {
    skin: H(["#0e0818", "#1a1029", "#2a1d44", "#46346e"]), hair: H(["#0a0612", "#140d22", "#22173a", "#34265a", "#52408a", "#8a72c8"]),
    dress: H(["#08050e", "#120c1e", "#1e1530", "#30234c", "#4c3a78"]), ribbon: H(["#040208", "#0a0612", "#160e24"]), shoe: H(["#040208", "#0a0612", "#160e24"]),
    eye: H(["#ffffff", "#ffffff", "#ffffff"]), tendril: H(["#030206", "#0a0612", "#160e26", "#281c46", "#46367c", "#8a72d0"]),
    crystal: H(["#1a1230", "#2c2050", "#4a3a7a", "#7a66b8", "#c8baf4"]), orbC: H(["#06030c", "#130a24", "#2a1a4c", "#6a4fb0", "#b9a6f4"]),
    aura: H(["#6a3fd0"])[0], tendrils: 8, crystals: 0, wild: 1.3, glowEyes: true, orb: false, torn: 1, arms: "down", rim: H(["#8a72d0"])[0],
  },
};

export const CAST_RELEASE = 0.35;

export function drawChild(p, t, o, anim, D) {
  const P = PHASES[o.phase] ?? PHASES[1];
  const ph = t / D, Y = -8 + Math.sin(ph * TAU) * 1.4;
  let raise = 0;
  if (anim === "cast") raise = t < CAST_RELEASE ? ease.inOutCubic(clamp(t / CAST_RELEASE)) : 1 - ease.inOutCubic(clamp((t - CAST_RELEASE - 0.08) / (D - CAST_RELEASE - 0.08)));
  const at = (x, y) => [x, y + Y];
  const R = new Raster(-82, -140, 82, 8);
  p.glow(2, -40 + Y, 54, P.aura, o.phase === 3 ? 0.3 : 0.18);

  // ---------------------------------------------------------------- crystals (far half of the orbit first)
  const crystals = [];
  for (let i = 0; i < P.crystals; i++) {
    const a = ph * TAU + (i * TAU) / P.crystals + Math.PI / 4, depth = Math.sin(a);
    crystals.push({ x: Math.cos(a) * 30 + 2, y: -46 + Y + depth * (depth > 0 ? 28 : 8) + Math.sin(a * 2 + i) * 3 - (i % 2) * 5, depth, s: (i % 2 ? 1 : 1.35) * (1 + depth * 0.15), spin: Math.cos(a * 2 + i) });   // a tilted orbit: the front pass goes low past her skirt, never across her face
  }
  const drawCrystal = (c) => {
    const h = 7 * c.s, w = 3.4 * c.s * (0.6 + 0.4 * Math.abs(c.spin)), dim = c.depth < 0 ? -0.2 : 0;
    // two faces split down the middle: left lit, right in shade, a bright ridge
    R.poly([[c.x, c.y - h], [c.x, c.y + h * 0.75], [c.x - w, c.y]], lit(P.crystal, 0.7 + dim), 2);
    R.poly([[c.x, c.y - h], [c.x + w, c.y], [c.x, c.y + h * 0.75]], lit(P.crystal, 0.35 + dim), 2);
    R.line(c.x, c.y - h + 1, c.x, c.y + h * 0.6, P.crystal[4], 2);
    R.set(c.x - w * 0.5, c.y - h * 0.3, P.crystal[4], 2);
  };
  for (const c of crystals) if (c.depth < 0) drawCrystal(c);

  // ---------------------------------------------------------------- tendrils behind her
  const seeds = [[-10, -44, 3.6, 46], [14, -44, -0.5, 46], [-8, -24, 2.7, 40], [12, -24, 0.4, 40], [-12, -56, 4.0, 42], [16, -56, -0.9, 42], [-4, -14, 2.2, 34], [8, -14, 0.9, 34]];
  for (let i = 0; i < P.tendrils; i++) {
    const [sx, sy, a0, len0] = seeds[i], len = len0 * (o.phase === 3 ? 1.25 : 0.8), ctrl = [];
    let x = sx, y = sy + Y, a = a0;
    for (let k = 0; k <= 6; k++) {
      ctrl.push([x, Math.min(2, y)]);
      a += Math.sin(ph * TAU * 2 + k * 0.7 + i * 1.9) * 0.45 + (i % 2 ? -0.07 : 0.07);
      x += Math.cos(a) * (len / 6); y += Math.sin(a) * (len / 6);
    }
    const w0 = o.phase === 3 ? 9 : 5.5;
    R.stroke(curve(ctrl, 5), (u) => w0 * Math.pow(1 - u, 0.85) + 0.6, (u, b, d) => (d > 0.72 && b > 0.05 ? P.tendril[4 + (u < 0.3 ? 1 : 0)] : strand(P.tendril.slice(0, 5), b, d, -0.15 + (1 - u) * 0.1)));
  }

  // ---------------------------------------------------------------- hair: long waves down her back (lifting wild in runaway)
  const wild = P.wild;
  R.poly([at(-12, -72), at(16, -74), at(18, -58), [14 + Math.sin(ph * TAU) * 2, -34 + Y - wild * 6], [-2, -28 + Y - wild * 4], [-16 + Math.sin(ph * TAU + 1) * 2, -32 + Y - wild * 6], at(-17, -54)],
         (x, y, u, v) => lit(P.hair, 0.15 + Math.sin(x * 1.2 - y * 0.2) * 0.06));
  for (let s = 0; s < 12; s++) {
    const f = s / 11, x0 = lerp(-12, 16, f), side = x0 < 2 ? -1 : 1, ctrl = [];
    for (let k = 0; k <= 5; k++) {
      const u = k / 5, wave = Math.sin(u * 7 + s * 1.3 + ph * TAU) * (1.5 + u * 1.5);   // the waves of her hair
      const lift = wild * u * u * (10 + (s % 3) * 4);
      ctrl.push([x0 + side * u * 6 + wave + side * lift * 0.6 + Math.sin(ph * TAU * 2 + s) * wild * u * 2, -66 + u * (36 + (s % 4) * 3) + Y - lift]);
    }
    R.stroke(curve(ctrl, 5), (u) => 5.5 * (1 - u * 0.6) + 0.5, (u, b, d) => strand(P.hair, b, d, -0.2 + Math.abs(f - 0.5) * -0.2 + 0.1));
  }

  // ---------------------------------------------------------------- legs and shoes
  for (const [x0, x1, sh] of [[-1, -1.5, -0.1], [6, 6.5, 0.1]]) {
    R.stroke([at(x0, -24), at(x1, -8)], 3.2, (u, b, d) => strand(P.skin, b, d, sh));
    R.ellipse(x1 + 0.5, -6 + Y, 3, 2, (x, y, dx, dy) => lit(P.shoe, 0.6 - dx * 0.2 - dy * 0.4));
    R.line(x1 - 1.5, -8 + Y, x1 + 2, -8 + Y, P.shoe[2]);   // strap
  }

  // ---------------------------------------------------------------- dress: bodice, puffed skirt in two frilled tiers
  const hemY = (x, base) => base + Y + Math.sin(ph * TAU * 2 + x * 0.4) * 1 + (P.torn && Math.round(x) % 5 === 0 ? 3 : 0);
  const tier = (top, base, half, shift, ramp, bias) => {
    const pts = [at(-half * 0.55 + shift, top), at(half * 0.55 + shift + 3, top)];
    for (let x = half + shift + 3; x >= -half + shift; x -= 2) pts.push([x, hemY(x, base)]);
    R.poly(pts, (x, y, u, v) => lit(ramp, tl(u, v * 0.7, bias + Math.sin(x * 0.9 + ph * TAU) * 0.08)));
    for (let x = -half + shift + 1; x <= half + shift + 2; x += 3) {   // scallops with a lace hole
      const yy = hemY(x, base);
      R.ellipse(x, yy, 1.7, 1.4, ramp[ramp.length - 1]);
      R.set(x, yy, ramp[1]);
    }
  };
  tier(-36, -20, 14, 0, P.dress, -0.08);
  tier(-30, -25, 11.5, 0.5, P.dress, 0.1);
  if (P.torn) for (let x = -12; x <= 16; x += 3) R.stroke([[x, hemY(x, -20)], [x - 1, hemY(x, -20) + 4 + (Math.abs(x) % 3)]], 1.5, P.tendril[2]);
  R.poly([at(-5, -48), at(9, -48), at(10, -36), at(-6, -36)], (x, y, u, v) => lit(P.dress, tl(u, v, 0.15)));
  R.poly([at(-6, -38), at(10, -38), at(10, -35.5), at(-6, -35.5)], (x, y, u, v) => lit(P.ribbon, 0.5 + (1 - v) * 0.4));   // sash
  // puffed sleeves
  R.ellipse(-6, -46 + Y, 3.4, 3, (x, y, dx, dy) => lit(P.dress, 0.5 - dx * 0.2 - dy * 0.3));
  R.ellipse(10, -46 + Y, 3.4, 3, (x, y, dx, dy) => lit(P.dress, 0.65 - dx * 0.2 - dy * 0.3));
  // the black ribbon bow at the collar
  R.poly([at(2, -48), at(-3, -51), at(-3.5, -46)], (x, y, u, v) => lit(P.ribbon, 0.6 - u * 0.4));
  R.poly([at(2, -48), at(7, -51), at(7.5, -46)], (x, y, u, v) => lit(P.ribbon, 0.7 - u * 0.4));
  R.stroke([at(1.5, -47.5), at(0.5, -44), at(0, -40.5)], 1.6, P.ribbon[1]); R.stroke([at(2.5, -47.5), at(3.5, -44), at(4.5, -40.5)], 1.6, P.ribbon[1]);   // tails hanging
  R.set(-0.5, -40 + Y, P.ribbon[0]); R.set(5, -40 + Y, P.ribbon[0]);
  R.ellipse(2, -48 + Y, 1.3, 1.2, P.ribbon[2]);

  // ---------------------------------------------------------------- arms
  const hm = { l: P.skin[2], s: P.skin[1], d: P.skin[0] };
  const poses = {
    orb: { l: [[-6, -45], [-5, -38], [-1, -34]], r: [[10, -45], [11, -38], [7, -34]] },
    open: { l: [[-6, -45], [-12, -42], [-18, -46]], r: [[10, -45], [16, -42], [22, -46]] },
    down: { l: [[-6, -45], [-10, -36], [-12, -28]], r: [[10, -45], [14, -36], [16, -28]] },
  }[P.arms];
  const lift = (pts, side) => pts.map(([x, y], k) => [lerp(x, k === 0 ? x : 2 + side * (3 + k * 2), raise), lerp(y, k === 0 ? y : -54 - k * 9, raise) + Y]);
  const la = lift(poses.l, -1), ra = lift(poses.r, 1);
  R.stroke(la, 2.8, (u, b, d) => strand(P.skin, b, d, -0.1));
  R.stroke(ra, 2.8, (u, b, d) => strand(P.skin, b, d, 0.05));
  if (P.arms === "open") for (const [a, flip] of [[la, true], [ra, false]]) {
    const [hx, hy] = a[2];
    grid(R, ["l.l.l", "sssss", ".sss.", "..d.."], Math.round(hx - 2), Math.round(hy - 3), hm, { flip });   // fingers spread
  }
  if (o.phase === 3) for (const a of [la, ra]) { const [hx, hy] = a[2]; for (const dx of [-1.5, 0, 1.5]) R.line(hx + dx, hy, hx + dx * 1.6, hy + 3.5, P.tendril[4]); }

  // ---------------------------------------------------------------- the orb (phase 1): a black sphere swirling with violet light
  let orb = null;
  if (P.orb) {
    orb = { x: lerp(3, 2, raise), y: lerp(-34, -88, raise) + Y, r: 6 + raise * 2 };
    R.ellipse(orb.x, orb.y, orb.r, orb.r, (x, y, dx, dy) => {
      const rr = Math.hypot(dx, dy), ang = Math.atan2(dy, dx), swirl = Math.sin(ang * 2 - rr * 5 + ph * TAU * 2);
      if (rr > 0.82) return dx + dy < -0.2 ? P.orbC[4] : P.orbC[3];
      return swirl > 0.6 && rr > 0.3 ? P.orbC[3] : swirl > 0.1 ? P.orbC[2] : P.orbC[rr < 0.45 ? 0 : 1];
    }, 2);
    R.set(orb.x - orb.r * 0.45, orb.y - orb.r * 0.45, WHITE, 2);
    // small hands cupping it from below on either side
    grid(R, [".ls", "lss", "sd.", ".d."], Math.round(orb.x - orb.r - 1), Math.round(orb.y), hm);
    grid(R, [".ls", "lss", "sd.", ".d."], Math.round(orb.x + orb.r - 2), Math.round(orb.y - 1), hm, { flip: true });
  }

  // ---------------------------------------------------------------- head: face, big eyes, bangs, side locks
  // the face, pixel by pixel: a round chibi face turned a little to the right, big eyes, small mouth
  const FACE = [
    "...lllllllllllll...",
    "..lllllllllllllll..",
    ".lllllllllllllllll.",
    ".llllllllllllllllll",
    "dllllllllllllllllll",
    "dllllllllllllllllll",
    "dllllllllllllllllll",
    "dllllllllllllllllll",
    "dllllllllllllllllll",
    "dsllllllllllllllll.",
    ".dsbblllllllllbbll.",
    ".dsssllllmmllllll..",
    "..ddsssllllllllo...",
    "...dddsslllllo.....",
    ".....dddddsoo......",
  ];
  const blush = o.phase === 3 ? P.skin[1] : H(["#eab4c8"])[0];
  const fy = Math.round(-68 + Y);
  grid(R, FACE, -7, fy, { l: P.skin[2], s: P.skin[1], d: P.skin[0], b: blush, m: P.glowEyes ? P.skin[1] : P.skin[0], o: P.skin[0] });
  // big eyes: a heavy lash line, the iris dark at the top and light at the bottom, two catchlights
  const glowTag = P.glowEyes ? 2 : 1;
  const em = { E: P.ribbon[0], I: { c: P.eye[0], tag: glowTag }, i: { c: P.eye[1], tag: glowTag }, j: { c: P.eye[2], tag: 2 }, h: { c: WHITE, tag: 2 } };
  grid(R, ["EEEEE", "IhhII", "IhIII", "iiiii", ".jjj."], 5, fy + 5, em);    // near eye
  grid(R, ["EEEE", "IhII", "IIII", "iiii", ".jj."], -3, fy + 5, em);        // far eye
  R.set(10, fy + 4, P.ribbon[0]); R.set(-4, fy + 4, P.ribbon[0]);            // lash flicks
  R.line(5, fy + 2, 8, fy + 2, P.hair[2]); R.line(-3, fy + 2, -1, fy + 2, P.hair[2]);   // brows
  // crown and bangs
  R.ellipse(3, -66 + Y, 12.5, 10, (x, y, dx, dy) => {
    if (dy > 0.2 && x > -5 && x < 12) return undefined;
    const ring = Math.abs(dy + 0.35 + dx * dx * 0.3) < 0.07 && dx > -0.6 && dx < 0.5;
    return ring ? P.hair[5] : lit(P.hair, 0.52 - dx * 0.25 - dy * 0.25 + Math.sin(x * 1.4 - y * 0.4) * 0.07);
  });
  R.poly([at(-7, -71), at(13, -71), at(13, -66), at(-7, -66)], (x, y, u, v) => lit(P.hair, 0.55 - u * 0.2));   // a base under the bangs: no holes for the outline to find
  for (let k = 0; k < 7; k++) {
    const x = -5 + k * 2.6, tip = -64.5 - (k % 2) * 1.5 - (k === 3 ? 0.5 : 0) + (k === 0 || k === 6 ? 3 : 0);   // on the brow, longer at the temples
    R.poly([[x - 2, -68 + Y], [x + 2, -68 + Y], [x + 0.8 + Math.sin(ph * TAU + k) * wild, tip + Y]], (xx, yy, u, v) => lit(P.hair, 0.7 - u * 0.2 - v * 0.15 + (k % 2) * 0.05));
  }
  for (const [x0, sh] of [[12.5, 0.1], [-6.5, -0.1]]) {
    const ctrl = [];
    for (let k = 0; k <= 4; k++) { const u = k / 4; ctrl.push([x0 + Math.sin(u * 6 + x0) * 1.6 + (x0 > 0 ? 1 : -1) * wild * u * 4, -64 + u * 20 + Y - wild * u * 4]); }
    R.stroke(curve(ctrl, 5), (u) => 4 * (1 - u * 0.6) + 0.5, (u, b, d) => strand(P.hair, b, d, sh));
  }

  for (const c of crystals) if (c.depth >= 0) drawCrystal(c);

  R.outline(INK, o.phase === 3 ? 0.18 : 0.3);
  R.stamp(p);

  // ---------------------------------------------------------------- light
  if (P.rim) { p.glow(-4, -66 + Y, 12, P.rim, 0.3); p.glowLine(-12, -60 + Y, -14, -30 + Y, 4, P.rim, 0.15); }
  if (P.glowEyes) for (const [ex, ey] of [[7, -61], [-1.5, -61]]) p.glow(ex, ey + Y, 6, P.eye[1], o.phase === 3 ? 0.8 : 0.5);
  if (orb) { p.glow(orb.x, orb.y, 14 + raise * 8, P.orbC[3], 0.35 + raise * 0.3); }
  for (const c of crystals) p.glow(c.x, c.y, 7, P.crystal[3], c.depth < 0 ? 0.1 : 0.22);
  if (anim === "cast") {
    const hx = (la[2][0] + ra[2][0]) / 2, hy = Math.min(la[2][1], ra[2][1]) - 3, f = t - CAST_RELEASE;
    if (raise > 0.3) p.glow(hx, hy, 10 + raise * 10, P.aura, 0.35 * raise);
    if (f >= 0 && f < 0.12) sparkle(p, hx, hy, 8 * (1 - f / 0.12) + 2, P.crystal[4], WHITE, 0.6);
  }
}
