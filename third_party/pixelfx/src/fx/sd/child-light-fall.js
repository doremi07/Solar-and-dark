// Solar and Dark boss effect: arcane/star-fall with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../arcane/star-fall.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["child-light-fall"]);
export const act = base.act;
