// Solar and Dark: the Hidden Child idling, hovering. Option phase: 1 억눌린 빛, 2 폭주, 3 완전한 그림자.
import { drawChild } from "./_child.js";

export const meta = {
  name: "The Hidden Child (idle)",
  desc: "A small girl hovers, bob swaying: she cradles a dark orb among orbiting crystals, then runs wild, then becomes a shadow with white eyes.",
  tags: ["solar-and-dark", "boss", "character", "child", "idle", "loop"],
  size: [108, 96],
  origin: [54, 92],
  duration: 2,
  loop: true,
  fps: 30,
  bg: "dark",
  options: { phase: 1, seed: 1 },
};

export function draw(p, t, o) {
  drawChild(p, t, o, "idle", meta.duration);
}
