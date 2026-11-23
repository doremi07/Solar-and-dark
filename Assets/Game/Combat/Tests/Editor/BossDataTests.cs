using System.Collections.Generic;
using NUnit.Framework;
using SolarDark.Combat;
using SolarDark.Combat.Editor;

// Window > General > Test Runner > EditMode. Builds the boss assets and checks every skill lines up with its sheets.
public class BossDataTests
{
    static IEnumerable<BossSkill> Skills(BossDefinition d)
    {
        var seen = new HashSet<BossSkill>();
        foreach (var p in d.phases)
        {
            if (p.entrySkill != null && seen.Add(p.entrySkill)) yield return p.entrySkill;
            foreach (var s in p.skills) if (s != null && seen.Add(s)) yield return s;
        }
    }

    [Test]
    public void BothBossesBuildWithThreePhases()
    {
        var (serena, child) = BossLibrary.Build();
        foreach (var d in new[] { serena, child })
        {
            Assert.AreEqual(3, d.phases.Count, d.displayName);
            Assert.AreEqual(1f, d.phases[0].enterAtHp);
            Assert.Greater(d.phases[0].enterAtHp, d.phases[1].enterAtHp);
            Assert.Greater(d.phases[1].enterAtHp, d.phases[2].enterAtHp);
            Assert.AreEqual(2, d.PhaseFor(0.1f));
            Assert.AreEqual(0, d.PhaseFor(1f));
            foreach (var p in d.phases)
            {
                Assert.NotNull(p.idleSheet, $"{d.displayName} {p.name} idle body");
                Assert.NotNull(p.castSheet, $"{d.displayName} {p.name} cast body");
                Assert.NotNull(p.hurtSheet, $"{d.displayName} {p.name} hurt body");
                Assert.IsTrue(p.idleSheet.data.loop, $"{d.displayName} {p.name} idle loops");
            }
        }
    }

    [Test]
    public void EverySkillHasItsSheetsAndHitsInsideThem()
    {
        var (serena, child) = BossLibrary.Build();
        int n = 0;
        foreach (var d in new[] { serena, child })
            foreach (var s in Skills(d))
            {
                n++;
                Assert.IsNotEmpty(s.displayName, s.id);
                Assert.Greater(s.cues.Count, 0, s.id);
                foreach (var c in s.cues)
                {
                    if (c.action == CueAction.SpawnIllusions) { Assert.NotNull(c.subSkill, s.id); continue; }
                    if (c.sheet == null && c.hit.damage <= 0 && c.hit.pull > 0) continue;   // an invisible pull area under another cue's effect
                    // a further hit segment of a sibling cue's effect (the rift's stepped boxes): same place, no sheet of its own
                    if (c.sheet == null && s.cues.Exists(o => o.sheet != null && o.anchor == c.anchor && o.offset == c.offset && o.aim == c.aim))
                    {
                        Assert.Greater(c.hit.size.x * c.hit.size.y, 0, $"{s.id}/{c.label}");
                        continue;
                    }
                    Assert.NotNull(c.sheet, $"{s.id}/{c.label} sheet");
                    if (c.hit.shape == HitShape.None) continue;
                    float len = c.loopDuration > 0 ? c.loopDuration : c.sheet.data.TotalSeconds;
                    Assert.LessOrEqual(c.hit.delay, len, $"{s.id}/{c.label}: hit after the effect ends");
                    if (c.hit.shape == HitShape.Circle) Assert.Greater(c.hit.size.x, 0, s.id);
                    else Assert.Greater(c.hit.size.x * c.hit.size.y, 0, s.id);
                }
            }
        Assert.GreaterOrEqual(n, 19, "every attack pattern from the concept sheets");
    }
}
