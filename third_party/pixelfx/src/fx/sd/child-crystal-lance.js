// Solar and Dark boss effect: ice/ice-lance with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../ice/ice-lance.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["child-crystal-lance"]);
export const act = base.act;
