using System;
using System.Collections.Generic;
using PixelFx;
using UnityEngine;

namespace SolarDark.Combat
{
    /// <summary>Where a cue's instances go. Ground anchors stand on the floor under the point; spreads run along x.</summary>
    public enum CueAnchor
    {
        /// <summary>The caster's feet (offset is mirrored by facing).</summary>
        Self,
        /// <summary>The target's feet when the cast began (what the telegraph showed).</summary>
        Target,
        /// <summary>The target's feet at the moment the instance spawns (tracks a moving target).</summary>
        TargetLive,
        /// <summary>Spread evenly along the floor round the target (spread = total width).</summary>
        LineAtTarget,
        /// <summary>Random spots on the floor within spread/2 of the target.</summary>
        RandomNearTarget,
        /// <summary>A circle of radius spread round the caster (offset lifts its centre).</summary>
        RingAroundSelf,
        /// <summary>Evenly at ±spread from the target on the floor (left, right, further left...).</summary>
        FlankTarget,
    }

    public enum CueAim
    {
        None,
        /// <summary>Mirror to face the target (effects face right).</summary>
        FaceTarget,
        /// <summary>Mirror to face away from the target (a back swing).</summary>
        AwayFromTarget,
        /// <summary>Rotate to point at the target (beams, projectiles).</summary>
        RotateToTarget,
    }

    public enum HitShape { None, Circle, Box }

    public enum CueAction
    {
        None,
        /// <summary>The caster dashes towards the target's cast-start position: value = distance, duration = time.</summary>
        Dash,
        /// <summary>The caster heals: value = fraction of max HP.</summary>
        HealSelf,
        /// <summary>Spawn mirror images round the target that each cast <see cref="SkillCue.subSkill"/>: value = count.</summary>
        SpawnIllusions,
    }

    [Serializable]
    public class SkillHit
    {
        public HitShape shape = HitShape.None;
        [Tooltip("Circle: x = radius. Box: width, height (before rotation).")]
        public Vector2 size = new Vector2(1, 1);
        [Tooltip("Centre of the area from the instance's origin, in its own facing (mirrored and rotated with it).")]
        public Vector2 offset;
        [Tooltip("Seconds after the instance spawns that the area becomes live. Match the effect's contact frame.")]
        public float delay;
        [Tooltip("How long it stays live (0 = a single check at delay).")]
        public float active;
        [Tooltip("While live, hit again every this many seconds (0 = once per target).")]
        public float tick;
        public float damage = 10;
        [Tooltip("Knockback speed away from the area's centre (units/s).")]
        public float knockback;
        public float hitStop;
        public float shake;
        [Tooltip("Root the target for this many seconds.")]
        public float root;
        [Tooltip("While live, drag targets within pullRadius towards the centre at this speed (units/s).")]
        public float pull;
        public float pullRadius;
    }

    /// <summary>One beat of a skill: an effect (or several), where it goes, and what it hits.</summary>
    [Serializable]
    public class SkillCue
    {
        public string label = "";
        [Tooltip("Seconds after the wind-up ends.")]
        public float time;
        public PixelFxSheet sheet;
        public CueAnchor anchor = CueAnchor.Self;
        public Vector2 offset;
        [Tooltip("The point of the effect (units from its pivot, facing right) that goes on the planned position and that it " +
                 "rotates round, e.g. the hand a beam fires from. Hit offsets are measured from this point.")]
        public Vector2 effectAnchor;
        [Min(1)] public int count = 1;
        [Tooltip("Seconds between instances.")]
        public float interval;
        [Tooltip("Width / radius for spread anchors.")]
        public float spread;
        public CueAim aim = CueAim.None;
        [Tooltip("Parent the instance to the caster so it moves with them.")]
        public bool followCaster;
        [Tooltip("Parent the instance to the target so it tracks them (marks, curses).")]
        public bool followTarget;
        [Tooltip("Stop a looping effect (and its hit area) after this long. 0 = the sheet's own length.")]
        public float loopDuration;
        public int sortingOrder = 20;
        [Tooltip("Fly towards the aim direction at this speed (units/s). 0 = stay put.")]
        public float projectileSpeed;
        public float projectileRange = 14;
        [Tooltip("Played where a projectile hits something or runs out.")]
        public PixelFxSheet impactSheet;
        public SkillHit hit = new SkillHit();
        [Tooltip("Mark the hit area from the moment the pattern starts (the wind-up) until it goes live, so it can be dodged. " +
                 "Dashes mark their path, projectiles their lane; areas that stay live keep an outline while they hurt.")]
        public bool telegraph = true;
        public CueAction action = CueAction.None;
        public float actionValue;
        public float actionDuration;
        public BossSkill subSkill;
    }

    /// <summary>A boss attack: wind-up, a timeline of cues, recovery. Built for Serena and the Hidden Child by
    /// Tools > Solar and Dark > Build Boss Assets; tune the generated assets freely.</summary>
    [CreateAssetMenu(menuName = "Solar and Dark/Boss Skill", fileName = "BossSkill")]
    public class BossSkill : ScriptableObject
    {
        public string id = "";
        public string displayName = "";
        [TextArea] public string notes = "";
        [Tooltip("Seconds the caster telegraphs before t = 0 of the cues.")]
        public float windup = 0.6f;
        public float recovery = 0.6f;
        public float cooldown = 6;
        [Min(0)] public float weight = 1;
        [Tooltip("Only picked when the target is within this horizontal distance band.")]
        public float minRange = 0, maxRange = 99;
        [Tooltip("Animator trigger fired at the start of the wind-up (if the boss has an Animator).")]
        public string animTrigger = "";
        public Color castTint = new Color(0.8f, 0.7f, 1f, 1f);
        public List<SkillCue> cues = new List<SkillCue>();

        /// <summary>Seconds from the start of the wind-up until the last cue has spawned and its hit is over.</summary>
        public float ActiveLength
        {
            get
            {
                float end = 0;
                foreach (var c in cues)
                {
                    float last = c.time + Mathf.Max(0, c.count - 1) * c.interval;
                    float tail = Mathf.Max(c.hit.shape != HitShape.None ? c.hit.delay + c.hit.active : 0, c.loopDuration, c.actionDuration);
                    end = Mathf.Max(end, last + tail);
                }
                return windup + end;
            }
        }
    }
}
