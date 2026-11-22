// Solar and Dark boss effect: shadow/black-hole with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../shadow/black-hole.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["child-despair"]);
export const act = base.act;
