// Solar and Dark boss effect: shadow/dark-explosion with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../shadow/dark-explosion.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["child-shadow-explosion"]);
export const act = base.act;
