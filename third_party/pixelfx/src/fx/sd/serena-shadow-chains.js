// Solar and Dark boss effect: shadow/shadow-chains with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../shadow/shadow-chains.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["serena-shadow-chains"]);
export const act = base.act;
