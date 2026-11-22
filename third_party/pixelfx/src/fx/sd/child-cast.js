// Solar and Dark: the Hidden Child casting: hands (and the orb) rise over her head, light bursts at CAST_RELEASE
// (0.35 s), then they come back down. Starts and ends on the idle pose. Option phase: 1, 2, 3.
import { drawChild } from "./_child.js";

export const meta = {
  name: "The Hidden Child (cast)",
  desc: "The child lifts her hands over her head, light gathering there, and it bursts at the release before her arms come back down.",
  tags: ["solar-and-dark", "boss", "character", "child", "cast", "attack"],
  size: [108, 96],
  origin: [54, 92],
  duration: 1,
  loop: true,
  fps: 30,
  bg: "dark",
  options: { phase: 1, seed: 1 },
};

export function draw(p, t, o) {
  drawChild(p, t, o, "cast", meta.duration);
}
