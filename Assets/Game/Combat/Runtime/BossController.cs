using System;
using System.Collections;
using System.Collections.Generic;
using PixelFx;
using UnityEngine;
using UnityEngine.Events;

namespace SolarDark.Combat
{
    /// <summary>
    /// Runs a <see cref="BossDefinition"/>: keeps its distance, picks a skill from the current phase (weighted, off
    /// cooldown, in range), casts it through <see cref="SkillRunner"/>, and changes phase as HP falls.
    /// Needs a Health; a Hurtbox (team Enemy) makes it hittable. Hook your animation to <see cref="onSkill"/>.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class BossController : MonoBehaviour, ISkillCaster
    {
        [Serializable] public class SkillEvent : UnityEvent<BossSkill> { }
        [Serializable] public class PhaseEvent : UnityEvent<int, BossPhase> { }

        public BossDefinition definition;
        [Tooltip("Who to fight. Empty: the nearest Player hurtbox.")]
        public Hurtbox target;
        public SpriteRenderer body;
        public bool autoStart = true;
        public int seed = 12345;

        public SkillEvent onSkill = new SkillEvent();
        public PhaseEvent onPhase = new PhaseEvent();

        Health health;
        Animator animator;
        PixelFxPlayer halo;
        readonly Dictionary<BossSkill, float> readyAt = new Dictionary<BossSkill, float>();
        System.Random rng;
        int facing = -1, phase = -1, casts;
        Coroutine brain;
        BossSkill forced;

        public Team Team => Team.Enemy;
        public Hurtbox Target => target != null && (target.Health == null || !target.Health.Dead) ? target : (target = Hurtbox.Nearest(Team.Player, transform.position));
        public int Facing
        {
            get => facing;
            set
            {
                facing = value >= 0 ? 1 : -1;
                if (body != null) body.flipX = facing < 0;
            }
        }
        public SpriteRenderer Body => body;
        public Health Health => health;
        public int Phase => phase;
        public BossPhase CurrentPhase => definition != null && phase >= 0 && phase < definition.phases.Count ? definition.phases[phase] : null;
        public BossSkill Casting { get; private set; }

        void Awake()
        {
            health = GetComponent<Health>();
            animator = GetComponentInChildren<Animator>();
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            rng = new System.Random(seed);
            if (definition != null)
            {
                health.max = definition.maxHp;
                health.current = definition.maxHp;
            }
        }

        void OnEnable()
        {
            if (autoStart && definition != null) brain = StartCoroutine(Brain());
        }

        void OnDisable()
        {
            if (brain != null) StopCoroutine(brain);
            brain = null;
        }

        /// <summary>Cast this next, skipping range and cooldown checks (debug, scripted moments).</summary>
        public void ForceNext(BossSkill skill) => forced = skill;

        /// <summary>Jump to a phase (debug): sets HP to that phase's threshold.</summary>
        public void JumpToPhase(int index)
        {
            if (definition == null || index < 0 || index >= definition.phases.Count) return;
            health.current = Mathf.Min(health.current, definition.phases[index].enterAtHp * health.max - 0.01f);
            if (index == 0) health.current = health.max;
            health.onChanged.Invoke(health.Fraction);
        }

        IEnumerator Brain()
        {
            EnterPhase(definition.PhaseFor(health.Fraction));
            yield return new WaitForSeconds(0.6f);
            while (!health.Dead)
            {
                int want = definition.PhaseFor(health.Fraction);
                if (want != phase)
                {
                    bool deeper = want > phase;   // healing or a debug jump back skips the entry skill
                    EnterPhase(want);
                    var entry = CurrentPhase.entrySkill;
                    if (deeper && entry != null)
                    {
                        health.invulnerable = true;
                        yield return Cast(entry);
                        health.invulnerable = false;
                    }
                    continue;
                }

                // between attacks: drift to the preferred range, facing the target
                float idle = RandomRange(CurrentPhase.idleTime.x, CurrentPhase.idleTime.y), t = 0;
                while (t < idle && forced == null)
                {
                    Approach(Time.deltaTime);
                    t += Time.deltaTime;
                    yield return null;
                }
                var skill = forced != null ? forced : Pick();
                forced = null;
                if (skill == null) { yield return null; continue; }
                yield return Cast(skill);
            }
            Casting = null;
        }

        IEnumerator Cast(BossSkill skill)
        {
            Casting = skill;
            readyAt[skill] = Time.time + skill.windup + skill.cooldown;
            onSkill.Invoke(skill);
            if (animator != null && !string.IsNullOrEmpty(skill.animTrigger)) animator.SetTrigger(skill.animTrigger);
            yield return SkillRunner.Run(skill, this, seed + 7919 * ++casts);
            Casting = null;
        }

        BossSkill Pick()
        {
            var p = CurrentPhase;
            if (p == null || p.skills.Count == 0) return null;
            var tg = Target;
            float dist = tg != null ? Mathf.Abs(tg.Feet.x - transform.position.x) : 0;
            float total = 0;
            var options = new List<BossSkill>();
            foreach (var s in p.skills)
            {
                if (s == null) continue;
                if (readyAt.TryGetValue(s, out var at) && Time.time < at) continue;
                if (dist < s.minRange || dist > s.maxRange) continue;
                options.Add(s);
                total += s.weight;
            }
            if (options.Count == 0) return null;
            double roll = rng.NextDouble() * total;
            foreach (var s in options)
            {
                roll -= s.weight;
                if (roll <= 0) return s;
            }
            return options[options.Count - 1];
        }

        void Approach(float dt)
        {
            var tg = Target;
            if (tg == null) return;
            float dx = tg.Feet.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.05f) Facing = dx > 0 ? 1 : -1;
            float gap = Mathf.Abs(dx) - definition.preferredRange;
            if (Mathf.Abs(gap) < 0.4f) return;
            float step = Mathf.Sign(gap) * Mathf.Sign(dx) * CurrentPhase.moveSpeed * dt;
            transform.position += new Vector3(step, 0, 0);
        }

        void EnterPhase(int index)
        {
            phase = index;
            var p = CurrentPhase;
            if (p == null) return;
            if (body != null) body.color = p.bodyTint;
            if (halo != null) Destroy(halo.gameObject);
            if (p.halo != null)
            {
                halo = PixelFxPlayer.Spawn(p.halo, transform.position + (Vector3)definition.haloOffset, false, transform);
                halo.transform.localPosition = definition.haloOffset;
                var sr = halo.GetComponent<SpriteRenderer>();
                sr.color = p.haloTint;
                sr.sortingOrder = (body != null ? body.sortingOrder : 10) - 1;
            }
            onPhase.Invoke(index, p);
        }

        float RandomRange(float a, float b) => a + (float)rng.NextDouble() * (b - a);
    }

    /// <summary>A mirror image of a caster that casts one skill and vanishes (the Hidden Child's "countless illusions").</summary>
    public class BossIllusion : MonoBehaviour, ISkillCaster
    {
        SpriteRenderer body;
        ISkillCaster owner;
        int facing = 1;

        public Team Team => owner?.Team ?? Team.Enemy;
        public Hurtbox Target => owner?.Target;
        public int Facing { get => facing; set { facing = value >= 0 ? 1 : -1; if (body != null) body.flipX = facing < 0; } }
        public SpriteRenderer Body => body;
        public Health Health => null;

        public static void SpawnRing(ISkillCaster owner, BossSkill skill, int count, float spacing, PixelFxSheet sparkle)
        {
            var tg = owner.Target;
            Vector2 center = tg != null ? tg.Feet : (Vector2)owner.transform.position;
            for (int i = 0; i < count; i++)
            {
                int k = i / 2 + 1, side = i % 2 == 0 ? -1 : 1;
                var go = new GameObject("illusion");
                go.transform.position = center + new Vector2(side * k * Mathf.Max(1, spacing), 0);
                var ill = go.AddComponent<BossIllusion>();
                ill.owner = owner;
                ill.body = go.AddComponent<SpriteRenderer>();
                if (owner.Body != null)
                {
                    ill.body.sprite = owner.Body.sprite;
                    ill.body.sortingOrder = owner.Body.sortingOrder - 1;
                    go.transform.localScale = owner.Body.transform.lossyScale;
                }
                ill.body.color = new Color(0.75f, 0.65f, 1f, 0.7f);
                ill.Facing = center.x >= go.transform.position.x ? 1 : -1;
                ill.StartCoroutine(ill.Life(skill, sparkle, 0.35f + 0.18f * i, 7001 * (i + 1)));
            }
        }

        IEnumerator Life(BossSkill skill, PixelFxSheet sparkle, float delay, int seed)
        {
            if (sparkle != null) PixelFxPlayer.Spawn(sparkle, transform.position + Vector3.up * 1.2f);
            float t = 0;
            while (t < delay) { t += Time.deltaTime; body.color = new Color(0.75f, 0.65f, 1f, 0.7f * Mathf.Clamp01(t / 0.25f)); yield return null; }
            if (skill != null) yield return SkillRunner.Run(skill, this, seed);
            if (sparkle != null) PixelFxPlayer.Spawn(sparkle, transform.position + Vector3.up * 1.2f);
            AfterImage.Spawn(body, body.color, 0.3f);
            Destroy(gameObject);
        }
    }
}
