// Solar and Dark: a tiny raster for drawing characters as pixel art. Written for this game (not upstream pixelfx).
//
// Shapes fill a grid of color numbers (later shapes cover earlier ones), lit from the top left in hard ramp steps;
// then a selective outline runs round the silhouette and the grid is stamped into the painter pixel by pixel.
import { hex, mix } from "../../core/index.js";

export class Raster {
  constructor(x0, y0, x1, y1) {
    this.x0 = x0; this.y0 = y0; this.w = x1 - x0; this.h = y1 - y0;
    this.c = new Float64Array(this.w * this.h).fill(NaN);   // NaN = empty (colors are signed 32-bit numbers)
    this.tag = new Uint8Array(this.w * this.h);
  }
  i(x, y) {
    x = Math.round(x) - this.x0; y = Math.round(y) - this.y0;
    return x < 0 || y < 0 || x >= this.w || y >= this.h ? -1 : y * this.w + x;
  }
  set(x, y, c, tag = 1) {
    const k = this.i(x, y);
    if (k >= 0 && c != null) { this.c[k] = c; this.tag[k] = tag; }
  }
  get(x, y) {
    const k = this.i(x, y);
    return k < 0 ? NaN : this.c[k];
  }

  /** Fill a polygon; color is a number or (x, y, u, v) => number, u/v 0..1 across its bounding box. */
  poly(pts, color, tag = 1) {
    let minY = Infinity, maxY = -Infinity, minX = Infinity, maxX = -Infinity;
    for (const [x, y] of pts) { minY = Math.min(minY, y); maxY = Math.max(maxY, y); minX = Math.min(minX, x); maxX = Math.max(maxX, x); }
    const bw = Math.max(1, maxX - minX), bh = Math.max(1, maxY - minY);
    for (let y = Math.floor(minY); y <= Math.ceil(maxY); y++) {
      const yc = y + 0.5, xs = [];
      for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
        const [xa, ya] = pts[i], [xb, yb] = pts[j];
        if ((ya <= yc) !== (yb <= yc)) xs.push(xa + ((yc - ya) / (yb - ya)) * (xb - xa));
      }
      xs.sort((a, b) => a - b);
      for (let k = 0; k + 1 < xs.length; k += 2)
        for (let x = Math.round(xs[k]); x < Math.round(xs[k + 1]); x++)
          this.set(x, y, typeof color === "function" ? color(x, y, (x - minX) / bw, (y - minY) / bh) : color, tag);
    }
  }

  ellipse(cx, cy, rx, ry, color, tag = 1) {
    for (let y = Math.floor(cy - ry); y <= Math.ceil(cy + ry); y++)
      for (let x = Math.floor(cx - rx); x <= Math.ceil(cx + rx); x++) {
        const dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry;
        if (dx * dx + dy * dy <= 1) this.set(x, y, typeof color === "function" ? color(x, y, dx, dy) : color, tag);
      }
  }

  /** A stroke of varying width along points: w(u) gives the width at 0..1 along it. */
  stroke(pts, w, color, tag = 1) {
    const n = pts.length;
    let total = 0;
    const seg = [];
    for (let i = 1; i < n; i++) { const d = Math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1]); seg.push(d); total += d; }
    let acc = 0;
    for (let i = 1; i < n; i++) {
      const [ax, ay] = pts[i - 1], [bx, by] = pts[i], d = seg[i - 1], steps = Math.max(1, Math.ceil(d * 2));
      for (let s = 0; s <= steps; s++) {
        const f = s / steps, x = ax + (bx - ax) * f, y = ay + (by - ay) * f, u = (acc + d * f) / (total || 1);
        const r = (typeof w === "function" ? w(u) : w) / 2;
        if (r < 0.5) { this.set(x, y, typeof color === "function" ? color(u, 0, 0) : color, tag); continue; }
        for (let yy = Math.floor(y - r); yy <= Math.ceil(y + r); yy++)
          for (let xx = Math.floor(x - r); xx <= Math.ceil(x + r); xx++) {
            const ex = xx + 0.5 - x, ey = yy + 0.5 - y;
            if (ex * ex + ey * ey <= r * r) this.set(xx, yy, typeof color === "function" ? color(u, (-ex - ey) / (r * 1.5), Math.sqrt(ex * ex + ey * ey) / r) : color, tag);
          }
      }
      acc += d;
    }
  }

  line(x0, y0, x1, y1, c, tag = 1) {
    const n = Math.max(1, Math.ceil(Math.max(Math.abs(x1 - x0), Math.abs(y1 - y0))));
    for (let i = 0; i <= n; i++) this.set(x0 + ((x1 - x0) * i) / n, y0 + ((y1 - y0) * i) / n, c, tag);
  }

  /**
   * Selective outline: transparent pixels next to the figure get a dark line; where the figure is light the line
   * is a dark shade of it, so the silhouette reads without a heavy black border everywhere.
   */
  outline(ink, k = 0.35) {
    const out = [];
    for (let y = 0; y < this.h; y++)
      for (let x = 0; x < this.w; x++) {
        if (!Number.isNaN(this.c[y * this.w + x])) continue;
        let near = NaN;
        for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
          const xx = x + dx, yy = y + dy;
          if (xx < 0 || yy < 0 || xx >= this.w || yy >= this.h) continue;
          const v = this.c[yy * this.w + xx];
          if (!Number.isNaN(v) && this.tag[yy * this.w + xx] !== 2) { near = v; break; }   // tag 2 = glow bits: no outline
        }
        if (!Number.isNaN(near)) out.push([y * this.w + x, mix(ink, near, k)]);
      }
    for (const [i, c] of out) { this.c[i] = c; this.tag[i] = 1; }
  }

  stamp(p, alpha = 1) {
    for (let y = 0; y < this.h; y++)
      for (let x = 0; x < this.w; x++) {
        const c = this.c[y * this.w + x];
        if (!Number.isNaN(c)) alpha >= 1 ? p.px(x + this.x0, y + this.y0, c) : p.dot(x + this.x0, y + this.y0, c, alpha);
      }
  }
}

/** Light from the top left: pick a ramp step from a brightness b (0 dark .. 1 light), with hard steps. */
export function lit(ramp, b) {
  const n = ramp.length;
  return ramp[Math.max(0, Math.min(n - 1, Math.floor(b * n)))];
}

/** Brightness across a shape's box, brighter to the top left. */
export const tl = (u, v, bias = 0) => Math.max(0, Math.min(0.999, 0.85 - u * 0.45 - v * 0.4 + bias));

export const H = (list) => list.map(hex);

/** A smooth path through control points (Catmull-Rom), n samples per segment. */
export function curve(pts, n = 6) {
  const out = [];
  for (let i = 0; i < pts.length - 1; i++) {
    const p0 = pts[Math.max(0, i - 1)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Math.min(pts.length - 1, i + 2)];
    for (let k = 0; k < n; k++) {
      const t = k / n, t2 = t * t, t3 = t2 * t;
      const f = (a, b, c, d) => 0.5 * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (-a + 3 * b - 3 * c + d) * t3);
      out.push([f(p0[0], p1[0], p2[0], p3[0]), f(p0[1], p1[1], p2[1], p3[1])]);
    }
  }
  out.push(pts[pts.length - 1]);
  return out;
}

/** Shade across a strand: dark at its edges, its ramp's middle in the body, a light line down the lit side. */
export function strand(ramp, b, d, bias = 0) {
  const k = 0.55 + b * 0.35 - d * d * 0.5 + bias;
  return lit(ramp, Math.max(0, Math.min(0.999, k)));
}
