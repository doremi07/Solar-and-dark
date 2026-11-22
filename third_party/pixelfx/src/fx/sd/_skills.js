// Solar and Dark: the flourishes for each boss skill effect, timed to the base effect's own beats.
// Coordinates are the effect's local pixels (origin = pivot, y down). Times in seconds from the effect's start.
import { PALETTES } from "./_alive.js";

const { DUSK, HALO, CRYSTAL } = PALETTES;
const UP = -Math.PI / 2, RIGHT = 0;

export const SKILLS = {
  // ---------------------------------------------------------------- shared crowns
  "serena-halo": {
    base: "holy/thorn-halo", palette: HALO, name: "Serena's halo", grow: [6, 6, 6, 6],
    motes: [{ rate: 10, x: [-30, 30], y: [-4, 30], vy: [-16, -6], life: [1, 1.8], hot: 0.4 }],
    glints: [{ rate: 3, x: [-26, 26], y: [-26, 26], size: [2, 3] }],
    pulses: [{ x: 0, y: 0, r: 34, k: 0.12, n: 2 }],
  },
  "child-halo": {
    base: "holy/thorn-halo", palette: CRYSTAL, name: "The child's halo", grow: [6, 6, 6, 6],
    motes: [{ rate: 9, x: [-30, 30], y: [-4, 30], vy: [-14, -6], life: [1, 1.8], hot: 0.3 }],
    glints: [{ rate: 4, x: [-28, 28], y: [-28, 28], size: [2, 4] }],
    pulses: [{ x: 0, y: 0, r: 34, k: 0.14, n: 2 }],
  },

  // ---------------------------------------------------------------- Serena
  "serena-shadow-fall": {
    base: "shadow/shadow-tendrils", palette: DUSK, name: "Shadow fall",
    bursts: [{ t: 0.3, x: 0, y: -2, n: 22, dir: UP, spread: 1.6, speed: [90, 220], g: 300, w: 30 }],
    rings: [{ t: 0.3, r: 46, sy: 0.18 }],
    flares: [{ t: 0.32, x: 0, y: -16, r: 50, k: 0.5, white: false }],
    wisps: [{ t0: 0.35, t1: 2.3, rate: 7, x: [-34, 34], y: [-6, -1], vy: [-16, -6], size: [3, 6] }],
    motes: [{ t0: 0.4, t1: 2.4, rate: 14, x: [-36, 36], y: [-4, 0], vy: [-40, -16], life: [0.6, 1] }],
  },
  "serena-shadow-slash": {
    // scythe-reap sweeps from the front, under and round behind: the tip sheds sparks along its path
    base: "melee/scythe-reap", palette: DUSK, name: "Shadow slash", grow: [16, 0, 16, 0],
    gather: [{ t0: 0, t1: 0.13, x: 40, y: -40, r: 26, n: 8 }],
    bursts: [
      { t: 0.14, x: 50, y: -34, n: 10, dir: 0.6, spread: 1.0, speed: [80, 180], g: 240, drag: 4.5, glints: 3, reach: 14 },
      { t: 0.22, x: 22, y: -14, n: 8, dir: Math.PI * 0.85, spread: 1.0, speed: [70, 160], g: 240, drag: 4.5, glints: 2, reach: 12 },
      { t: 0.3, x: -28, y: -12, n: 8, dir: Math.PI * 1.05, spread: 1.0, speed: [70, 160], g: 240, drag: 4.5, glints: 2, reach: 12 },
      { t: 0.38, x: -56, y: -26, n: 6, dir: Math.PI * 1.3, spread: 1.0, speed: [60, 140], g: 240, drag: 4.5, glints: 3, reach: 12 },
    ],
    flares: [{ t: 0.14, x: 44, y: -30, r: 40, k: 0.55 }],
    wisps: [{ t0: 0.16, t1: 0.8, rate: 12, x: [-60, 56], y: [-36, -8], vy: [-12, -2], size: [3, 5] }],
    motes: [{ t0: 0.18, t1: 1.1, rate: 22, x: [-62, 58], y: [-38, -8], vx: [-6, 6], vy: [-24, -6], life: [0.5, 0.9] }],
  },
  "serena-wide-slash": {
    base: "melee/spear-sweep", palette: DUSK, name: "Wide slash", grow: [0, 0, 20, 0],
    bursts: [{ t: 0.1, x: 86, y: -10, n: 18, dir: RIGHT, spread: 0.9, speed: [110, 220], g: 300, drag: 4.5 }],
    rings: [{ t: 0.1, x: 30, y: 0, r: 70, sy: 0.12 }],
    embers: [{ t0: 0.1, t1: 0.7, rate: 30, x: [-60, 100], y: [-6, -1], vx: [10, 40], vy: [-20, -4], life: [0.3, 0.6] }],
    wisps: [{ t0: 0.1, t1: 0.6, rate: 14, x: [-50, 100], y: [-14, -4], vx: [8, 24], size: [3, 5] }],
  },
  "serena-holy-domain": {
    base: "arcane/rune-circle", palette: HALO, name: "Holy domain",
    motes: [{ rate: 22, x: [-58, 58], y: [-8, 8], vy: [-34, -14], life: [0.8, 1.4], hot: 0.4 }],
    embers: [{ rate: 8, x: [-40, 40], y: [-4, 4], vy: [-50, -24], life: [0.6, 1] }],
    glints: [{ rate: 5, x: [-60, 60], y: [-10, 8] }],
    pulses: [{ x: 0, y: -4, r: 70, k: 0.12, n: 2 }],
  },
  "serena-holy-domain-burst": {
    base: "arcane/magic-circle-cast", palette: HALO, name: "Holy domain burst",
    gather: [{ t0: 0.6, t1: 1.5, x: 0, y: -18, r: 64, n: 26, sy: 0.6 }],
    flares: [{ t: 1.52, x: 0, y: -40, r: 80, k: 0.8 }],
    bursts: [{ t: 1.52, x: 0, y: -4, n: 32, dir: UP, spread: 1.4, speed: [120, 300], g: 200, w: 10 }],
    rings: [{ t: 1.52, r: 64, sy: 0.3 }],
    motes: [{ t0: 1.55, t1: 3.3, rate: 30, x: [-12, 12], y: [-170, -8], vy: [-60, -24], life: [0.5, 0.9], hot: 0.5 }],
    embers: [{ t0: 1.6, t1: 3.2, rate: 14, x: [-40, 40], y: [-6, 0], vy: [-40, -14] }],
  },
  "serena-shadow-chains": {
    base: "shadow/shadow-chains", palette: DUSK, name: "Shadow chains",
    wisps: [{ t0: 0.3, t1: 2.0, rate: 10, x: [-22, 22], y: [-40, -4], vy: [-10, -2], size: [3, 5] }],
    rings: [{ t: 0.78, y: -18, r: 26, sy: 0.3 }],
    flares: [{ t: 2.0, x: 0, y: -18, r: 50, k: 0.6 }],
    embers: [{ t0: 2.0, t1: 2.9, rate: 26, x: [-30, 30], y: [-40, -4], vy: [-50, -10] }],
  },
  "serena-rift": {
    base: "ice/ice-spikes", palette: DUSK, name: "Rift of light and shadow", grow: [18, 0, 0, 0],
    bursts: [20, 60, 100, 140].map((x) => ({ t: 0.04 + 0.44 * (x / 172), x, y: -2, n: 9, dir: UP, spread: 1.2, speed: [80, 180], g: 320 })).concat(
      [{ t: 1.42, x: 90, y: -22, n: 28, spread: Math.PI * 2, speed: [80, 220], g: 260, w: 60 }]),
    flares: [{ t: 1.36, x: 90, y: -24, r: 80, k: 0.5 }],
    wisps: [{ t0: 0.1, t1: 1.4, rate: 14, x: [0, 170], y: [-6, -1], vy: [-14, -4], size: [3, 5] }],
    motes: [{ t0: 1.42, t1: 2.6, rate: 22, x: [10, 170], y: [-30, -2], vy: [-36, -10] }],
  },
  "serena-judgment-mark": {
    base: "holy/sunburst", palette: HALO, name: "Mark of judgment",
    glints: [{ rate: 6, x: [-50, 50], y: [-50, 50], size: [2, 4] }],
    embers: [{ rate: 10, x: [-30, 30], y: [-20, 20], vx: [-8, 8], vy: [8, 24], g: 20, life: [0.8, 1.4] }],
    pulses: [{ x: 0, y: 0, r: 50, k: 0.15, n: 4 }],
  },
  "serena-judgment": {
    base: "holy/smite", palette: HALO, name: "Light of judgment",
    gather: [{ t0: 0.1, t1: 0.52, x: 0, y: -8, r: 34, n: 16, sy: 0.4 }],
    flares: [{ t: 0.54, x: 0, y: -12, r: 64, k: 0.9 }],
    bursts: [{ t: 0.54, x: 0, y: -2, n: 30, dir: UP, spread: 2.2, speed: [100, 260], g: 320, w: 6 }],
    rings: [{ t: 0.54, r: 54, sy: 0.3 }],
    embers: [{ t0: 0.6, t1: 2.2, rate: 16, x: [-24, 24], y: [-4, 0], vy: [-36, -12] }],
  },
  "serena-shadow-pillar": {
    base: "arcane/magic-circle-cast", palette: DUSK, opts: { seed: 4 }, name: "Shadow pillar",
    gather: [{ t0: 0.6, t1: 1.5, x: 0, y: -18, r: 64, n: 24, sy: 0.6 }],
    flares: [{ t: 1.52, x: 0, y: -40, r: 80, k: 0.7 }],
    bursts: [{ t: 1.52, x: 0, y: -4, n: 30, dir: UP, spread: 1.4, speed: [120, 300], g: 200, w: 10 }],
    rings: [{ t: 1.52, r: 64, sy: 0.3 }],
    wisps: [{ t0: 1.5, t1: 3.0, rate: 16, x: [-16, 16], y: [-150, -10], vy: [-30, -10], size: [4, 7] }],
    motes: [{ t0: 1.55, t1: 3.3, rate: 24, x: [-12, 12], y: [-170, -8], vy: [-60, -24], life: [0.5, 0.9] }],
  },

  // ---------------------------------------------------------------- the Hidden Child
  "child-light-fall": {
    base: "arcane/star-fall", palette: CRYSTAL, name: "Light fall",
    glints: [{ t0: 0, t1: 1.4, rate: 10, x: [-90, 40], y: [-130, -60], size: [2, 3] }],
    motes: [{ t0: 0.5, t1: 2.2, rate: 26, x: [-52, 52], y: [-4, 0], vy: [-40, -14], hot: 0.5 }],
    flares: [{ t: 0.5, x: 0, y: -6, r: 70, k: 0.35, white: false }],
  },
  "child-shadow-explosion": {
    base: "shadow/dark-explosion", palette: DUSK, name: "Shadow explosion",
    gather: [{ t0: 0, t1: 0.5, x: 0, y: -54, r: 80, n: 30 }],
    // no flare on the release: the black sphere at the heart is the point, light would wash it out
    flares: [{ t: 1.84, x: 0, y: -54, r: 40, k: 0.3, white: false }],
    bursts: [{ t: 0.52, x: 0, y: -54, n: 34, spread: Math.PI * 2, speed: [120, 300], g: 220 }],
    rings: [{ t: 0.55, r: 80, sy: 0.22 }],
    motes: [{ t0: 0.7, t1: 3.2, rate: 16, x: [-60, 60], y: [-100, -10], vy: [-30, -10], hot: 0.6, life: [0.7, 1.2] }],
  },
  "child-crystal": {
    base: "ice/ice-crystal", palette: CRYSTAL, name: "Floating crystal",
    glints: [{ rate: 5, x: [-16, 16], y: [-60, -22], size: [2, 3] }],
    motes: [{ rate: 10, x: [-14, 14], y: [-24, -10], vy: [-24, -10], life: [0.8, 1.4] }],
    pulses: [{ x: 0, y: -40, r: 26, k: 0.15, n: 3 }],
  },
  "child-crystal-lance": {
    base: "ice/ice-lance", palette: CRYSTAL, name: "Crystal lance",
    motes: [{ rate: 50, x: [-130, -20], y: [-4, 4], vx: [-50, -20], vy: [-8, 8], life: [0.2, 0.35], hot: 0.4 }],
    glints: [{ rate: 6, x: [-30, -2], y: [-4, 4], size: [2, 3] }],
  },
  "child-sparkle": {
    base: "impacts/hit-spark", palette: CRYSTAL, name: "Crystal sparkle", floor: false, grow: [6, 0, 0, 38],
    bursts: [{ t: 0.01, x: 0, y: 0, n: 14, spread: Math.PI * 2, speed: [80, 200], g: 120, life: 0.4 }],
    flares: [{ t: 0, x: 0, y: 0, r: 30, k: 0.5, white: false }],
  },
  "child-shadow-dash": {
    base: "melee/thrust", palette: DUSK, opts: { sparks: ["#fbf6ff", "#d2c4ee", "#9a82c8", "#6a4f9a"] }, name: "Shadow dash", grow: [0, 4, 40, 4],
    bursts: [{ t: 0.07, x: 140, y: -26, n: 20, dir: RIGHT, spread: 1.0, speed: [100, 220], g: 200, drag: 4.5 }],
    wisps: [{ t0: 0, t1: 0.4, rate: 30, x: [0, 130], y: [-32, -18], vx: [-20, -4], size: [3, 5] }],
    flares: [{ t: 0.07, x: 140, y: -26, r: 40, k: 0.6 }],
  },
  "child-despair": {
    base: "shadow/black-hole", palette: DUSK, name: "Domain of despair",
    motes: [{ rate: 26, x: [-86, 86], y: [-110, -10], toward: [0, -60], speedK: 0.9, drag: 0.4, g: 0, life: [0.9, 1.5] }],
    wisps: [{ rate: 6, x: [-70, 70], y: [-20, -4], vy: [-10, -2], size: [4, 7] }],
    glints: [{ rate: 4, x: [-80, 80], y: [-100, -20] }],
  },
  "child-light-absorb": {
    base: "explosions/implosion", palette: CRYSTAL, name: "Light absorption", floor: false, grow: [0, 0, 0, 6],
    gather: [{ t0: 0, t1: 0.8, x: 0, y: 0, r: 92, n: 34 }],
    flares: [{ t: 1.02, x: 0, y: 0, r: 76, k: 0.9 }],
    bursts: [{ t: 1.02, x: 0, y: 0, n: 34, spread: Math.PI * 2, speed: [150, 320], g: 60 }],
  },
  "child-light-burst": {
    base: "arcane/mana-burst", palette: HALO, name: "Light burst", grow: [0, 0, 0, 6],
    gather: [{ t0: 0, t1: 0.22, x: 0, y: -28, r: 54, n: 18 }],
    flares: [{ t: 0.22, x: 0, y: -28, r: 64, k: 0.8 }],
    bursts: [{ t: 0.22, x: 0, y: -28, n: 28, spread: Math.PI * 2, speed: [120, 260], g: 160 }],
    embers: [{ t0: 0.3, t1: 1.4, rate: 20, x: [-50, 50], y: [-70, -10], vx: [-6, 6], vy: [4, 16], g: 30 }],
  },
  "child-shadow-beam": {
    base: "arcane/arcane-beam", palette: DUSK, name: "Shadow beam",
    motes: [{ rate: 50, x: [22, 180], y: [-28, -20], vx: [10, 50], vy: [-12, 12], life: [0.25, 0.5], hot: 0.4 }],
    wisps: [{ rate: 10, x: [30, 180], y: [-30, -18], vx: [10, 30], vy: [-8, 0], size: [3, 5] }],
    glints: [{ rate: 6, x: [176, 188], y: [-32, -16], size: [2, 4] }],
  },
  "child-gravity": {
    base: "shadow/void-orb", palette: DUSK, name: "Gravity manipulation",
    motes: [{ rate: 30, x: [-76, 76], y: [-140, -10], toward: [0, -75], speedK: 0.9, drag: 0.4, g: 0, life: [0.8, 1.4] }],
    glints: [{ rate: 5, x: [-70, 70], y: [-140, -10] }],
  },
  "child-infinite-light": {
    base: "holy/holy-pillar", palette: CRYSTAL, name: "Infinite light",
    gather: [{ t0: 0.2, t1: 0.62, x: 0, y: -4, r: 34, n: 16, sy: 0.3 }],
    flares: [{ t: 0.66, x: 0, y: -20, r: 64, k: 0.8 }],
    bursts: [{ t: 0.66, x: 0, y: -2, n: 24, dir: UP, spread: 1.4, speed: [100, 240], g: 260, w: 6 }],
    rings: [{ t: 0.66, r: 44, sy: 0.25 }],
    motes: [{ t0: 0.7, t1: 2.4, rate: 30, x: [-10, 10], y: [-190, -4], vy: [-60, -20], life: [0.5, 0.9], hot: 0.5 }],
    embers: [{ t0: 0.7, t1: 2.6, rate: 12, x: [-30, 30], y: [-4, 0], vy: [-40, -14] }],
  },
};

// ---------------------------------------------------------------- detail pass: debris, runes, light shafts, floor marks
const more = (name, extra) => {
  const c = SKILLS[name];
  for (const [k, v] of Object.entries(extra)) c[k] = [...(c[k] ?? []), ...v];
};
more("serena-halo", { runes: [{ rate: 1.2, x: [-30, 30], y: [-6, 24], vy: [-14, -6], life: [1.2, 1.8] }] });
more("child-halo", { runes: [{ rate: 1.2, x: [-30, 30], y: [-6, 24], vy: [-14, -6], life: [1.2, 1.8] }] });
more("serena-shadow-fall", {
  shards: [{ t: 0.32, x: 0, y: -4, n: 14, dir: UP, spread: 1.8, speed: [80, 180], w: 30, g: 320 }],
  scorch: [{ t: 0.3, r: 44, sy: 0.18, life: 2.6 }],
});
more("serena-shadow-slash", { sparkrings: [{ t: 0.14, x: 50, y: -34, r: 18, n: 8, life: 0.4 }] });
more("serena-wide-slash", { scorch: [{ t: 0.12, x: 40, r: 50, sy: 0.12, life: 1.2 }] });
more("serena-holy-domain", {
  runes: [{ rate: 4, x: [-54, 54], y: [-8, 6], vy: [-22, -10], life: [1, 1.6] }],
  shafts: [{ rate: 6, x: [-50, 50], y: [-2, 4], size: [20, 50] }],
});
more("serena-holy-domain-burst", {
  runes: [{ t0: 0.4, t1: 1.5, rate: 6, x: [-40, 40], y: [-6, 4], vy: [-18, -8], life: [0.8, 1.2] }, { t0: 1.55, t1: 3.0, rate: 8, x: [-14, 14], y: [-120, -10], vy: [-40, -20] }],
  shafts: [{ t0: 1.5, t1: 2.6, rate: 14, x: [-24, 24], y: [-2, 2], size: [60, 150] }],
  scorch: [{ t: 1.52, r: 46, sy: 0.3, life: 2.2 }],
});
more("serena-shadow-chains", {
  runes: [{ t0: 0.1, t1: 2.0, rate: 5, x: [-40, 40], y: [-8, 8], vy: [-18, -8] }],
  shards: [{ t: 2.0, x: 0, y: -20, n: 16, speed: [70, 170], w: 14, g: 280 }],
});
more("serena-rift", {
  shards: [{ t: 1.42, x: 90, y: -20, n: 24, speed: [70, 190], w: 70, g: 320, size: 4 }],
  scorch: [{ t: 0.3, x: 86, r: 90, sy: 0.08, life: 2.4 }],
});
more("serena-judgment-mark", { runes: [{ rate: 2.5, x: [-40, 40], y: [-40, 40], vy: [-10, 10], life: [1, 1.6] }] });
more("serena-judgment", {
  shafts: [{ t0: 0.42, t1: 0.9, rate: 30, x: [-20, 20], y: [-2, 2], size: [40, 110] }],
  runes: [{ t0: 0.6, t1: 2.0, rate: 6, x: [-24, 24], y: [-10, 0], vy: [-26, -12] }],
  scorch: [{ t: 0.54, r: 34, sy: 0.28, life: 2.0 }],
  sparkrings: [{ t: 0.56, y: -4, r: 46, sy: 0.3, n: 14, life: 0.6 }],
});
more("serena-shadow-pillar", {
  runes: [{ t0: 0.4, t1: 1.5, rate: 6, x: [-40, 40], y: [-6, 4], vy: [-18, -8] }],
  shafts: [{ t0: 1.5, t1: 2.6, rate: 10, x: [-22, 22], y: [-2, 2], size: [60, 150] }],
  scorch: [{ t: 1.52, r: 46, sy: 0.3, life: 2.2 }],
});
more("child-light-fall", {
  shards: [0.55, 0.85, 1.15, 1.45].map((t, i) => ({ t, x: -40 + i * 26, y: -3, n: 6, dir: UP, spread: 1.6, speed: [60, 140], g: 340 })),
  scorch: [{ t: 0.5, r: 56, sy: 0.12, life: 1.9 }],
});
more("child-shadow-explosion", {
  shards: [{ t: 0.52, x: 0, y: -54, n: 26, speed: [100, 240], g: 260, size: 4 }],
  scorch: [{ t: 0.6, r: 70, sy: 0.16, life: 3.0 }],
});
more("child-crystal", { sparkrings: [{ t: 0, x: 0, y: -40, r: 14, n: 6, life: 1.2 }, { t: 1.5, x: 0, y: -40, r: 14, n: 6, life: 1.2 }] });
more("child-sparkle", { sparkrings: [{ t: 0, r: 22, n: 10, life: 0.4 }], shards: [{ t: 0.01, n: 6, speed: [60, 140], g: 140, life: 0.45 }] });
more("child-shadow-dash", { shards: [{ t: 0.07, x: 140, y: -26, n: 10, dir: RIGHT, spread: 1.2, speed: [80, 180], g: 240 }] });
more("child-despair", {
  shards: [],
  runes: [{ rate: 3, x: [-80, 80], y: [-20, -4], vy: [-18, -8], life: [1.2, 1.8] }],
});
more("child-light-absorb", { shards: [{ t: 1.02, n: 22, speed: [120, 260], g: 80, size: 3.6 }], sparkrings: [{ t: 1.02, r: 60, n: 16, life: 0.55 }] });
more("child-light-burst", { runes: [{ t0: 0.24, t1: 1.2, rate: 10, x: [-30, 30], y: [-50, -10], vx: [-30, 30], vy: [-30, 10] }], sparkrings: [{ t: 0.22, y: -28, r: 50, n: 14, life: 0.5 }] });
more("child-shadow-beam", { shards: [] });
more("child-gravity", {
  // rocks torn loose, floating up towards the void
  motes: [],
  runes: [{ rate: 3, x: [-60, 60], y: [-120, -20], toward: [0, -75], speedK: 0.7, drag: 0.4, g: 0, life: [1, 1.6] }],
});
more("child-infinite-light", {
  shafts: [{ t0: 0.4, t1: 2.0, rate: 16, x: [-14, 14], y: [-2, 2], size: [60, 180] }],
  runes: [{ t0: 0.7, t1: 2.4, rate: 6, x: [-12, 12], y: [-120, -10], vy: [-40, -16] }],
  scorch: [{ t: 0.66, r: 34, sy: 0.25, life: 2.4 }],
});
