using System;
using System.Collections.Generic;
using SolarDark.Combat;
// The Unity-free parts of the boss skill system: hit geometry and the cast timeline.
int fails = 0;
void Check(bool ok, string what) { Console.WriteLine((ok ? "PASS " : "FAIL ") + what); if (!ok) fails++; }

Check(SkillGeometry.CircleCircle(0, 0, 1, 1.5f, 0, 0.6f), "circles touching overlap");
Check(!SkillGeometry.CircleCircle(0, 0, 1, 1.7f, 0, 0.6f), "circles apart miss");
// a 14x0.9 beam pointing right from (7,0): hits at x=13, misses above it
Check(SkillGeometry.BoxCircle(7, 0, 14, 0.9f, 0, 13, 0.3f, 0.5f), "beam hits along its length");
Check(!SkillGeometry.BoxCircle(7, 0, 14, 0.9f, 0, 13, 2f, 0.5f), "beam misses above");
// rotated 90 degrees the beam points up
Check(SkillGeometry.BoxCircle(0, 7, 14, 0.9f, (float)Math.PI / 2, 0, 12, 0.5f), "rotated beam hits above");
Check(!SkillGeometry.BoxCircle(0, 7, 14, 0.9f, (float)Math.PI / 2, 5, 7, 0.5f), "rotated beam misses to the side");
SkillGeometry.Place(2.2f, 1.5f, -1, 0, out var mx, out var my);
Check(Math.Abs(mx + 2.2f) < 1e-5 && Math.Abs(my - 1.5f) < 1e-5, "offset mirrors when facing left");
Check(SkillGeometry.LineOffset(0, 3, 5) == -2.5f && SkillGeometry.LineOffset(2, 3, 5) == 2.5f && SkillGeometry.LineOffset(0, 1, 5) == 0, "line spread");
SkillGeometry.RingOffset(0, 6, 2, out var rx, out var ry);
Check(Math.Abs(rx) < 1e-5 && Math.Abs(ry - 2) < 1e-5, "ring starts straight up");

// Serena's shadow pillar: windup 0.4, 3 pillars 0.35 s apart, hit 1.52 s after each spawns, mark 1.5 s ahead
var plan = SkillTimeline.Plan(0.4f, new List<SkillTimeline.CueTiming> {
    new SkillTimeline.CueTiming { time = 0, count = 3, interval = 0.35f, hitDelay = 1.52f, telegraph = true, hasHit = true, telegraphLead = 1.5f },
});
Check(plan.Count == 6, $"pillar plan has 3 marks + 3 spawns ({plan.Count})");
Check(plan[0].kind == SkillTimeline.Kind.Spawn && Math.Abs(plan[0].t - 0.4f) < 1e-4, $"first pillar spawns when the wind-up ends ({plan[0]})");
var mark = plan.Find(e => e.kind == SkillTimeline.Kind.Telegraph);
Check(Math.Abs(mark.t - 0.42f) < 1e-4, $"its mark appears 1.5 s before the column fires ({mark})");
bool sorted = true; for (int i = 1; i < plan.Count; i++) sorted &= plan[i].t >= plan[i - 1].t;
Check(sorted, "events in time order");
// a mark that would start before the cast is clamped to 0
var early = SkillTimeline.Plan(0.2f, new List<SkillTimeline.CueTiming> {
    new SkillTimeline.CueTiming { time = 0, count = 1, hitDelay = 0.1f, telegraph = true, hasHit = true, telegraphLead = 2f } });
Check(early[0].t == 0 && early[0].kind == SkillTimeline.Kind.Telegraph, "early mark clamps to the cast start");
// no hit, no mark
var none = SkillTimeline.Plan(0.5f, new List<SkillTimeline.CueTiming> { new SkillTimeline.CueTiming { count = 6, interval = 0.1f, telegraph = true, hasHit = false } });
Check(none.Count == 6 && none.TrueForAll(e => e.kind == SkillTimeline.Kind.Spawn), "decor cues only spawn");
Console.WriteLine(fails == 0 ? "all passed" : $"{fails} failed");
return fails == 0 ? 0 : 1;
