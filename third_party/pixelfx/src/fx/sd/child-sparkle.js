// Solar and Dark boss effect: impacts/hit-spark with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../impacts/hit-spark.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["child-sparkle"]);
export const act = base.act;
