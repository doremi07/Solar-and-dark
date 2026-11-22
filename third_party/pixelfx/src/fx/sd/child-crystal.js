// Solar and Dark boss effect: ice/ice-crystal with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../ice/ice-crystal.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["child-crystal"]);
export const act = base.act;
