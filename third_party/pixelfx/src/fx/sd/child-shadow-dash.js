// Solar and Dark boss effect: melee/thrust with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../melee/thrust.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["child-shadow-dash"]);
export const act = base.act;
