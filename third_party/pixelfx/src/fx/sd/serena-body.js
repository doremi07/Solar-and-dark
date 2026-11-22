// Solar and Dark: Serena idling, hovering. Option phase: 1 그림자, 2 융합, 3 정화.
import { drawSerena } from "./_serena.js";

export const meta = {
  name: "Serena (idle)",
  desc: "Serena hovers, sword point-down: hair and skirt sway on a slow wind; shadow tendrils in phase 1-2, ribbons of light in phase 3.",
  tags: ["solar-and-dark", "boss", "character", "serena", "idle", "loop"],
  size: [200, 182],
  origin: [86, 174],
  duration: 2,
  loop: true,
  fps: 30,
  bg: "dark",
  options: { phase: 1, seed: 1 },
};

export function draw(p, t, o) {
  drawSerena(p, t, o, "idle", meta.duration);
}
