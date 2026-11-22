// Solar and Dark boss effect: shadow/shadow-tendrils with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../shadow/shadow-tendrils.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["serena-shadow-fall"]);
export const act = base.act;
