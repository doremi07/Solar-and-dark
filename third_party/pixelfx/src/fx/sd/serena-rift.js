// Solar and Dark boss effect: ice/ice-spikes with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../ice/ice-spikes.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["serena-rift"]);
export const act = base.act;
