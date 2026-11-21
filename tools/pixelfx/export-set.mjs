#!/usr/bin/env node
// Export a set of named, recoloured sheets described by a JSON manifest (see tools/pixelfx/boss-fx.json):
//   node tools/pixelfx/export-set.mjs tools/pixelfx/boss-fx.json [name-filter]
// Each entry: { name, id, palette: "<key in palettes>" | [hex...], opts: {...} } -> <dest>/<name>.png/.json
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const [manifestPath, filter] = process.argv.slice(2);
if (!manifestPath) { console.error("usage: node tools/pixelfx/export-set.mjs <manifest.json> [name-filter]"); process.exit(1); }
const m = JSON.parse(readFileSync(manifestPath, "utf8"));
let n = 0;
for (const s of m.sheets) {
  if (filter && !s.name.includes(filter)) continue;
  const palette = typeof s.palette === "string" ? m.palettes[s.palette] : s.palette;
  if (s.palette && !palette) { console.error(`${s.name}: unknown palette ${s.palette}`); process.exit(1); }
  const opts = { ...(palette ? { palette } : {}), ...(s.opts ?? {}) };
  const args = [join(ROOT, "tools", "pixelfx", "export.mjs"), s.id, "--name", s.name, "--flat", "--dest", join(ROOT, m.dest)];
  if (Object.keys(opts).length) args.push("--opts", JSON.stringify(opts));
  const r = spawnSync(process.execPath, args, { stdio: ["ignore", "ignore", "inherit"] });
  if (r.status !== 0) { console.error(`${s.name}: export failed`); process.exit(r.status ?? 1); }
  console.log(`${s.name.padEnd(28)} <- ${s.id}`);
  n++;
}
console.log(`${n} sheets -> ${m.dest}`);
