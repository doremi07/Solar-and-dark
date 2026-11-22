// Solar and Dark boss effect: holy/thorn-halo with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../holy/thorn-halo.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["child-halo"]);
export const act = base.act;
