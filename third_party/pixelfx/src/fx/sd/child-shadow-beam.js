// Solar and Dark boss effect: arcane/arcane-beam with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../arcane/arcane-beam.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["child-shadow-beam"]);
export const act = base.act;
