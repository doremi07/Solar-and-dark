// Solar and Dark boss effect: melee/spear-sweep with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../melee/spear-sweep.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["serena-wide-slash"]);
export const act = base.act;
