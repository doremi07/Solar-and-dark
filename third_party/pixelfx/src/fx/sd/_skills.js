// Solar and Dark: the flourishes for each boss skill effect, timed to the base effect's own beats.
// Coordinates are the effect's local pixels (origin = pivot, y down). Times in seconds from the effect's start.
import { PALETTES } from "./_alive.js";

const { DUSK, HALO, CRYSTAL } = PALETTES;
const UP = -Math.PI / 2, RIGHT = 0;

export const SKILLS = {
  // Restraint: the base effect carries each skill. At most three flourishes on top, each with one job: a beat on
  // contact (sparks or debris), one thing that lingers (motes or wisps, sparse), and a floor mark where it lands.

  // ---------------------------------------------------------------- shared crowns
  "serena-halo": {
    base: "holy/thorn-halo", palette: HALO, name: "Serena's halo", grow: [6, 6, 6, 6],
    motes: [{ rate: 5, x: [-26, 26], y: [0, 26], vy: [-14, -6], life: [1.2, 1.8], hot: 0.4 }],
    glints: [{ rate: 1.5, x: [-24, 24], y: [-24, 24], size: [2, 3] }],
  },
  "child-halo": {
    base: "holy/thorn-halo", palette: CRYSTAL, name: "The child's halo", grow: [6, 6, 6, 6],
    motes: [{ rate: 5, x: [-26, 26], y: [0, 26], vy: [-14, -6], life: [1.2, 1.8], hot: 0.3 }],
    glints: [{ rate: 2, x: [-26, 26], y: [-26, 26], size: [2, 3] }],
  },

  // ---------------------------------------------------------------- Serena
  "serena-shadow-fall": {
    base: "shadow/shadow-tendrils", palette: DUSK, name: "Shadow fall",
    shards: [{ t: 0.32, x: 0, y: -4, n: 9, dir: UP, spread: 1.4, speed: [90, 190], w: 26, g: 320 }],
    scorch: [{ t: 0.3, r: 40, sy: 0.18, life: 2.4 }],
    wisps: [{ t0: 0.4, t1: 2.2, rate: 4, x: [-30, 30], y: [-6, -1], vy: [-14, -6], size: [3, 5] }],
  },
  "serena-shadow-slash": {
    // scythe-reap sweeps from the front, under and round behind: the tip sheds a few sparks where it bites
    base: "melee/scythe-reap", palette: DUSK, name: "Shadow slash", grow: [16, 0, 16, 0],
    bursts: [
      { t: 0.14, x: 50, y: -34, n: 7, dir: 0.6, spread: 0.9, speed: [90, 180], g: 240, drag: 4.5, glints: 2, reach: 14 },
      { t: 0.34, x: -40, y: -18, n: 4, dir: Math.PI * 1.15, spread: 0.9, speed: [70, 140], g: 240, drag: 4.5, glints: 1, reach: 10 },
    ],
    flares: [{ t: 0.14, x: 46, y: -32, r: 28, k: 0.4, white: false }],
    motes: [{ t0: 0.18, t1: 0.9, rate: 8, x: [-56, 54], y: [-34, -10], vx: [-6, 6], vy: [-20, -6], life: [0.4, 0.8] }],
  },
  "serena-wide-slash": {
    base: "melee/spear-sweep", palette: DUSK, opts: { sparks: ["#fbf6ff", "#d2c4ee", "#9a82c8", "#6a4f9a"] }, name: "Wide slash", grow: [0, 0, 20, 0],
    bursts: [{ t: 0.1, x: 86, y: -10, n: 9, dir: RIGHT, spread: 0.7, speed: [110, 210], g: 300, drag: 4.5 }],
    scorch: [{ t: 0.12, x: 40, r: 46, sy: 0.1, life: 1.0 }],
    embers: [{ t0: 0.1, t1: 0.6, rate: 12, x: [-40, 100], y: [-5, -1], vx: [10, 36], vy: [-18, -4], life: [0.3, 0.55] }],
  },
  "serena-holy-domain": {
    base: "arcane/rune-circle", palette: HALO, name: "Holy domain",
    motes: [{ rate: 9, x: [-54, 54], y: [-6, 6], vy: [-30, -12], life: [0.8, 1.4], hot: 0.4 }],
    runes: [{ rate: 1.5, x: [-48, 48], y: [-6, 4], vy: [-18, -8], life: [1.2, 1.6] }],
  },
  "serena-holy-domain-burst": {
    base: "arcane/magic-circle-cast", palette: HALO, name: "Holy domain burst",
    gather: [{ t0: 0.7, t1: 1.5, x: 0, y: -16, r: 56, n: 12, sy: 0.6 }],
    flares: [{ t: 1.52, x: 0, y: -36, r: 56, k: 0.45 }],
    bursts: [{ t: 1.52, x: 0, y: -4, n: 14, dir: UP, spread: 1.2, speed: [120, 280], g: 220, w: 10 }],
    motes: [{ t0: 1.55, t1: 3.1, rate: 10, x: [-10, 10], y: [-150, -10], vy: [-50, -20], life: [0.5, 0.9], hot: 0.5 }],
  },
  "serena-shadow-chains": {
    base: "shadow/shadow-chains", palette: DUSK, name: "Shadow chains",
    wisps: [{ t0: 0.3, t1: 2.0, rate: 4, x: [-20, 20], y: [-36, -4], vy: [-10, -2], size: [3, 5] }],
    shards: [{ t: 2.0, x: 0, y: -20, n: 10, speed: [70, 160], w: 14, g: 280 }],
    flares: [{ t: 2.0, x: 0, y: -18, r: 32, k: 0.4, white: false }],
  },
  "serena-rift": {
    base: "ice/ice-spikes", palette: DUSK, name: "Rift of light and shadow", grow: [18, 0, 0, 0],
    bursts: [40, 120].map((x) => ({ t: 0.04 + 0.44 * (x / 172), x, y: -2, n: 5, dir: UP, spread: 1.0, speed: [80, 160], g: 320 })),
    shards: [{ t: 1.42, x: 90, y: -20, n: 14, speed: [70, 180], w: 60, g: 320, size: 4 }],
    scorch: [{ t: 0.3, x: 86, r: 84, sy: 0.08, life: 2.2 }],
  },
  "serena-judgment-mark": {
    base: "holy/sunburst", palette: HALO, name: "Mark of judgment",
    glints: [{ rate: 2.5, x: [-44, 44], y: [-44, 44], size: [2, 3] }],
    embers: [{ rate: 4, x: [-26, 26], y: [-18, 18], vx: [-8, 8], vy: [8, 22], g: 20, life: [0.8, 1.4] }],
  },
  "serena-judgment": {
    // the smite is the event: a short gather, a modest flare that keeps the column readable, sparks, a burn
    base: "holy/smite", palette: HALO, name: "Light of judgment",
    gather: [{ t0: 0.2, t1: 0.52, x: 0, y: -8, r: 30, n: 8, sy: 0.4 }],
    bursts: [{ t: 0.54, x: 0, y: -2, n: 14, dir: UP, spread: 1.8, speed: [100, 240], g: 320, w: 6 }],
    scorch: [{ t: 0.54, r: 30, sy: 0.26, life: 1.8 }],
  },
  "serena-shadow-pillar": {
    base: "arcane/magic-circle-cast", palette: DUSK, opts: { seed: 4 }, name: "Shadow pillar",
    gather: [{ t0: 0.7, t1: 1.5, x: 0, y: -16, r: 56, n: 12, sy: 0.6 }],
    bursts: [{ t: 1.52, x: 0, y: -4, n: 14, dir: UP, spread: 1.2, speed: [120, 280], g: 220, w: 10 }],
    wisps: [{ t0: 1.5, t1: 2.9, rate: 6, x: [-14, 14], y: [-140, -10], vy: [-28, -10], size: [4, 6] }],
  },

  // ---------------------------------------------------------------- the Hidden Child
  "child-light-fall": {
    base: "arcane/star-fall", palette: CRYSTAL, name: "Light fall",
    shards: [0.6, 1.2].map((t, i) => ({ t, x: -26 + i * 40, y: -3, n: 5, dir: UP, spread: 1.4, speed: [60, 130], g: 340 })),
    scorch: [{ t: 0.5, r: 50, sy: 0.12, life: 1.8 }],
    motes: [{ t0: 0.5, t1: 2.0, rate: 8, x: [-46, 46], y: [-4, 0], vy: [-36, -12], hot: 0.5 }],
  },
  "child-shadow-explosion": {
    base: "shadow/dark-explosion", palette: DUSK, name: "Shadow explosion",
    // no flare on the release: the black sphere at the heart is the point, light would wash it out
    gather: [{ t0: 0.1, t1: 0.5, x: 0, y: -54, r: 70, n: 14 }],
    shards: [{ t: 0.52, x: 0, y: -54, n: 16, speed: [100, 230], g: 260, size: 4 }],
    scorch: [{ t: 0.6, r: 64, sy: 0.16, life: 2.8 }],
  },
  "child-crystal": {
    base: "ice/ice-crystal", palette: CRYSTAL, name: "Floating crystal",
    glints: [{ rate: 2, x: [-14, 14], y: [-58, -24], size: [2, 3] }],
    motes: [{ rate: 4, x: [-12, 12], y: [-24, -12], vy: [-22, -10], life: [0.8, 1.4] }],
  },
  "child-crystal-lance": {
    base: "ice/ice-lance", palette: CRYSTAL, name: "Crystal lance",
    motes: [{ rate: 22, x: [-110, -20], y: [-3, 3], vx: [-50, -20], vy: [-6, 6], life: [0.2, 0.35], hot: 0.4 }],
  },
  "child-sparkle": {
    base: "impacts/hit-spark", palette: CRYSTAL, name: "Crystal sparkle", floor: false, grow: [6, 0, 0, 38],
    bursts: [{ t: 0.01, x: 0, y: 0, n: 7, spread: Math.PI * 2, speed: [80, 180], g: 120, life: 0.4 }],
    shards: [{ t: 0.01, n: 4, speed: [60, 130], g: 140, life: 0.45 }],
  },
  "child-shadow-dash": {
    base: "melee/thrust", palette: DUSK, opts: { sparks: ["#fbf6ff", "#d2c4ee", "#9a82c8", "#6a4f9a"] }, name: "Shadow dash", grow: [0, 4, 40, 4],
    bursts: [{ t: 0.07, x: 140, y: -26, n: 9, dir: RIGHT, spread: 0.8, speed: [100, 210], g: 200, drag: 4.5 }],
    wisps: [{ t0: 0, t1: 0.35, rate: 12, x: [0, 130], y: [-30, -20], vx: [-20, -4], size: [3, 5] }],
  },
  "child-despair": {
    base: "shadow/black-hole", palette: DUSK, name: "Domain of despair",
    motes: [{ rate: 10, x: [-80, 80], y: [-100, -10], toward: [0, -60], speedK: 0.9, drag: 0.4, g: 0, life: [0.9, 1.5] }],
    wisps: [{ rate: 2.5, x: [-64, 64], y: [-18, -4], vy: [-10, -2], size: [4, 6] }],
  },
  "child-light-absorb": {
    base: "explosions/implosion", palette: CRYSTAL, name: "Light absorption", floor: false, grow: [0, 0, 0, 6],
    gather: [{ t0: 0.1, t1: 0.8, x: 0, y: 0, r: 84, n: 14 }],
    flares: [{ t: 1.02, x: 0, y: 0, r: 48, k: 0.5 }],
    shards: [{ t: 1.02, n: 12, speed: [120, 250], g: 80, size: 3.6 }],
  },
  "child-light-burst": {
    base: "arcane/mana-burst", palette: HALO, name: "Light burst", grow: [0, 0, 0, 6],
    gather: [{ t0: 0.04, t1: 0.22, x: 0, y: -28, r: 48, n: 8 }],
    bursts: [{ t: 0.22, x: 0, y: -28, n: 14, spread: Math.PI * 2, speed: [120, 240], g: 160 }],
    embers: [{ t0: 0.3, t1: 1.3, rate: 7, x: [-44, 44], y: [-64, -12], vx: [-6, 6], vy: [4, 14], g: 30 }],
  },
  "child-shadow-beam": {
    base: "arcane/arcane-beam", palette: DUSK, name: "Shadow beam",
    motes: [{ rate: 18, x: [22, 180], y: [-27, -21], vx: [10, 50], vy: [-10, 10], life: [0.25, 0.5], hot: 0.4 }],
    glints: [{ rate: 3, x: [176, 188], y: [-30, -18], size: [2, 3] }],
  },
  "child-gravity": {
    base: "shadow/void-orb", palette: DUSK, name: "Gravity manipulation",
    motes: [{ rate: 10, x: [-70, 70], y: [-130, -10], toward: [0, -75], speedK: 0.9, drag: 0.4, g: 0, life: [0.8, 1.4] }],
    runes: [{ rate: 1.2, x: [-56, 56], y: [-110, -24], toward: [0, -75], speedK: 0.7, drag: 0.4, g: 0, life: [1, 1.6] }],
  },
  "child-infinite-light": {
    base: "holy/holy-pillar", palette: CRYSTAL, name: "Infinite light",
    gather: [{ t0: 0.3, t1: 0.62, x: 0, y: -4, r: 30, n: 8, sy: 0.3 }],
    bursts: [{ t: 0.66, x: 0, y: -2, n: 12, dir: UP, spread: 1.2, speed: [100, 220], g: 260, w: 6 }],
    motes: [{ t0: 0.7, t1: 2.3, rate: 10, x: [-8, 8], y: [-170, -6], vy: [-50, -20], life: [0.5, 0.9], hot: 0.5 }],
  },
};
