#!/usr/bin/env node
// Render pixelfx effects to sprite sheets (PNG + Aseprite-style JSON) and drop them into the Unity project.
// The Unity editor picks up anything in Assets/PixelFx/Sheets/ automatically (PixelFxSheetPostprocessor):
// it sets point filtering / no compression, and builds a PixelFxSheet asset and a ready-to-use prefab.
//
//   node tools/pixelfx/export.mjs explosions/small-explosion
//   node tools/pixelfx/export.mjs fire/torch --name blue-torch --opts '{"palette":["#102","#215","#43a","#8cf","#fff"]}'
//   node tools/pixelfx/export.mjs melee/sword-slash --act          the whole act (stock character + target) baked in
//   node tools/pixelfx/export.mjs ice                              every effect in a category
//
// Options: --name NAME (one id only) --opts JSON --seed N --fps N --act --dest DIR (default Assets/PixelFx/Sheets)
// --flat (write straight into DIR, no category folder)

import { spawnSync } from "node:child_process";
import { mkdtempSync, readdirSync, readFileSync, writeFileSync, copyFileSync, mkdirSync, rmSync, existsSync } from "node:fs";
import { join, dirname, basename } from "node:path";
import { tmpdir } from "node:os";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const PFX = join(ROOT, "third_party", "pixelfx");
const args = process.argv.slice(2), ids = [], flags = {};
for (let i = 0; i < args.length; i++) {
  const a = args[i];
  if (!a.startsWith("--")) ids.push(a);
  else if (a === "--act") flags.act = true;
  else if (a === "--flat") flags.flat = true;
  else flags[a.slice(2)] = args[++i];
}
if (!ids.length) { console.error("usage: node tools/pixelfx/export.mjs <id|category> [--name N] [--opts JSON] [--seed N] [--fps N] [--act]"); process.exit(1); }
if (!existsSync(join(PFX, "tools", "render.mjs"))) { console.error("third_party/pixelfx is missing: run tools/pixelfx/vendor.sh"); process.exit(1); }
const dest = flags.dest ?? join(ROOT, "Assets", "PixelFx", "Sheets");
const version = readFileSync(join(ROOT, "tools", "pixelfx", "PIXELFX_VERSION"), "utf8").match(/version=(.+)/)[1].trim();

const tmp = mkdtempSync(join(tmpdir(), "pixelfx-"));
try {
  const run = (script, extra) => {
    const r = spawnSync(process.execPath, [join(PFX, "tools", script), ...ids, ...extra], { cwd: PFX, stdio: "inherit" });
    if (r.status !== 0) process.exit(r.status ?? 1);
  };
  const passthrough = [];
  for (const k of ["opts", "seed", "fps"]) if (flags[k] !== undefined) passthrough.push("--" + k, flags[k]);

  let produced = []; // [{ id, png, json }]
  if (flags.act) {
    // act.mjs writes records/act/<cat>-<name>.png/.json inside the pixelfx checkout
    if (passthrough.length) console.warn("note: act sheets ignore --opts/--seed/--fps (pixelfx act.mjs renders the stock act)");
    run("act.mjs", ["--sheet"]);
    const actDir = join(PFX, "records", "act");
    for (const id of ids) {
      const stem = id.replace("/", "-"), png = join(actDir, stem + ".sheet.png"), json = join(actDir, stem + ".sheet.json");
      if (!existsSync(json)) { console.error(`no act sheet for ${id} (is it an effect with an act?)`); process.exit(1); }
      produced.push({ id, png, json, suffix: "-act" });
    }
  } else {
    run("render.mjs", ["--sheet", "--out", tmp, ...passthrough]);
    for (const cat of readdirSync(tmp)) for (const name of readdirSync(join(tmp, cat))) {
      const d = join(tmp, cat, name);
      produced.push({ id: `${cat}/${name}`, png: join(d, name + ".png"), json: join(d, name + ".json"), suffix: "" });
    }
  }
  if (flags.name && produced.length !== 1) { console.error("--name works with exactly one effect"); process.exit(1); }

  for (const p of produced) {
    const [cat, name] = p.id.split("/"), out = flags.name ?? name + p.suffix, dir = flags.flat ? dest : join(dest, cat);
    mkdirSync(dir, { recursive: true });
    const json = JSON.parse(readFileSync(p.json, "utf8"));
    json.meta.image = out + ".png";
    json.meta.pixelfx = { ...(json.meta.pixelfx ?? {}), id: p.id, version, ...(flags.opts ? { options: JSON.parse(flags.opts) } : {}), ...(p.suffix ? { baked: "act" } : {}) };
    copyFileSync(p.png, join(dir, out + ".png"));
    writeFileSync(join(dir, out + ".json"), JSON.stringify(json, null, 1));
    console.log(`${p.id} -> ${join(dir, out)}.png/.json`);
  }
} finally {
  rmSync(tmp, { recursive: true, force: true });
}
