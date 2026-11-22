// Solar and Dark boss effect: explosions/implosion with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../explosions/implosion.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["child-light-absorb"]);
export const act = base.act;
