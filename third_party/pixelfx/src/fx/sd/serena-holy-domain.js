// Solar and Dark boss effect: arcane/rune-circle with flourishes (see _skills.js). Generated; edit _skills.js.
import * as base from "../arcane/rune-circle.js";
import { alive } from "./_alive.js";
import { SKILLS } from "./_skills.js";

export const { meta, create } = alive(base, SKILLS["serena-holy-domain"]);
export const act = base.act;
