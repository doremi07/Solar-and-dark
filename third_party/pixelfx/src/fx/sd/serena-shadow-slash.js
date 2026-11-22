// Solar and Dark boss effect: melee/scythe-reap with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../melee/scythe-reap.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["serena-shadow-slash"]);
export const act = base.act;
