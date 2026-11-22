// Solar and Dark boss effect: holy/smite with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../holy/smite.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["serena-judgment"]);
export const act = base.act;
