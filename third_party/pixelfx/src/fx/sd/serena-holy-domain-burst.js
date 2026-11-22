// Solar and Dark boss effect: arcane/magic-circle-cast with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../arcane/magic-circle-cast.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["serena-holy-domain-burst"]);
export const act = base.act;
