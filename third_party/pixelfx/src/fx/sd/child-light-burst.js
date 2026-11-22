// Solar and Dark boss effect: arcane/mana-burst with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../arcane/mana-burst.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["child-light-burst"]);
export const act = base.act;
