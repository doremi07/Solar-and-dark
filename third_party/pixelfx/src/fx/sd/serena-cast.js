// Solar and Dark: Serena casting: winds the sword up overhead and cuts forward at CAST_RELEASE (0.35 s), then settles.
// Starts and ends on the idle pose, so it loops cleanly back into serena-body. Option phase: 1, 2, 3.
import { drawSerena } from "./_serena.js";

export const meta = {
  name: "Serena (cast)",
  desc: "Serena raises her sword overhead, hair lifting, and cuts forward with a flash at the tip, then settles back to her hover.",
  tags: ["solar-and-dark", "boss", "character", "serena", "cast", "attack"],
  size: [200, 182],
  origin: [86, 174],
  duration: 1,
  loop: true,
  fps: 30,
  bg: "dark",
  options: { phase: 1, seed: 1 },
};

export function draw(p, t, o) {
  drawSerena(p, t, o, "cast", meta.duration);
}
