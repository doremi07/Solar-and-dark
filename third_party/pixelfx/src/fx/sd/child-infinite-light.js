// Solar and Dark boss effect: holy/holy-pillar with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../holy/holy-pillar.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["child-infinite-light"]);
export const act = base.act;
