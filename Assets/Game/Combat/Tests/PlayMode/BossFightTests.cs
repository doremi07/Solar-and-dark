using System.Collections;
using NUnit.Framework;
using SolarDark.Combat;
using UnityEngine;
using UnityEngine.TestTools;

// Window > General > Test Runner > PlayMode. Runs a skill through the real runner against a target.
public class BossFightTests
{
    [UnityTest]
    public IEnumerator SkillRunnerHitsAndRootsATarget()
    {
        var target = new GameObject("target");
        var hp = target.AddComponent<Health>();
        hp.max = hp.current = 100;
        hp.invulnerableAfterHit = 0;
        var hb = target.AddComponent<Hurtbox>();
        hb.team = Team.Player;
        var st = target.AddComponent<StatusReceiver>();

        var caster = new GameObject("caster");
        caster.transform.position = new Vector3(3, 0, 0);
        caster.AddComponent<Health>();
        var boss = caster.AddComponent<BossController>();
        boss.autoStart = false;
        boss.target = hb;

        var skill = ScriptableObject.CreateInstance<BossSkill>();
        skill.id = "test.bind";
        skill.windup = 0.1f;
        skill.recovery = 0.1f;
        skill.cues.Add(new SkillCue
        {
            anchor = CueAnchor.Target,
            hit = new SkillHit { shape = HitShape.Circle, size = new Vector2(1, 1), offset = new Vector2(0, 1), delay = 0.1f, damage = 15, root = 0.5f },
        });
        yield return SkillRunner.Run(skill, boss, 1);
        Assert.AreEqual(85f, hp.current, 0.01f);
        Assert.IsTrue(st.IsRooted, "the bind should still hold right after the skill");
        Object.Destroy(target);
        Object.Destroy(caster);
    }
}
