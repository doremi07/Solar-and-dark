using System.Collections;
using System.Collections.Generic;
using PixelFx;
using UnityEngine;

namespace SolarDark.Combat
{
    /// <summary>Anything that can cast a <see cref="BossSkill"/>: the boss itself and its illusions.</summary>
    public interface ISkillCaster
    {
        Transform transform { get; }
        Team Team { get; }
        Hurtbox Target { get; }
        /// <summary>+1 facing right, -1 facing left.</summary>
        int Facing { get; set; }
        SpriteRenderer Body { get; }
        Health Health { get; }
    }

    /// <summary>Plays a skill's timeline for a caster: telegraphs, effects, hit areas, projectiles and actions.</summary>
    public static class SkillRunner
    {
        public const float PixelsPerUnit = 16f;

        struct Planned
        {
            public Vector2 pos;     // where it goes (feet / origin), world
            public float angle;     // radians, for RotateToTarget
            public int facing;      // +1 / -1 after aim
            public bool live;       // position resolved at spawn (TargetLive)
            public float reach;     // distance to the point it is aimed at (RotateToTarget)
        }

        /// <summary>Run <paramref name="skill"/> to the end of its recovery. Yield on it from a coroutine.</summary>
        public static IEnumerator Run(BossSkill skill, ISkillCaster caster, int seed)
        {
            if (skill == null || caster == null) yield break;
            var rng = new System.Random(seed);
            var target = caster.Target;
            Vector2 self = caster.transform.position;
            Vector2 aimAt = target != null ? target.Feet : self + Vector2.right * caster.Facing * 4;
            if (target != null && Mathf.Abs(aimAt.x - self.x) > 0.05f) caster.Facing = aimAt.x >= self.x ? 1 : -1;

            // plan every instance now, so telegraphs show exactly where things will land
            var plans = new List<Planned[]>();
            var timings = new List<SkillTimeline.CueTiming>();
            foreach (var cue in skill.cues)
            {
                var arr = new Planned[Mathf.Max(1, cue.count)];
                for (int i = 0; i < arr.Length; i++) arr[i] = PlanInstance(cue, i, arr.Length, caster, self, aimAt, rng);
                plans.Add(arr);
                timings.Add(new SkillTimeline.CueTiming
                {
                    time = cue.time, interval = cue.interval, count = cue.count, hitDelay = cue.hit.delay,
                    telegraph = cue.telegraph, hasHit = cue.hit.shape != HitShape.None && (cue.hit.damage > 0 || cue.hit.root > 0),
                });
            }
            var events = SkillTimeline.Plan(skill.windup, timings);

            // wind-up: tint the body as a tell
            var body = caster.Body;
            Color baseColor = body != null ? body.color : Color.white;
            if (body != null && body.GetComponent<BossBody>() != null) body = null;   // a drawn body shows its own wind-up: no tint strobe

            float t = 0, end = skill.ActiveLength;
            int next = 0;
            while (next < events.Count || t < end)
            {
                while (next < events.Count && events[next].t <= t)
                {
                    var e = events[next++];
                    var cue = skill.cues[e.cue];
                    var plan = plans[e.cue][e.instance];
                    if (e.kind == SkillTimeline.Kind.Telegraph)
                    {
                        float spawnAt = skill.windup + cue.time + e.instance * cue.interval;
                        Telegraph(cue, plan, spawnAt - e.t, cue.hit.delay, plan.live ? target : null);
                    }
                    else
                    {
                        if (plan.live) plans[e.cue][e.instance] = plan = PlanInstance(cue, e.instance, cue.count, caster, caster.transform.position, target != null ? target.Feet : aimAt, rng);
                        Spawn(skill, cue, plan, caster, target != null ? target.Feet : aimAt);
                    }
                }
                if (body != null)
                {
                    float w = t < skill.windup ? Mathf.PingPong(t * 6f, 1f) : 0;
                    body.color = Color.Lerp(baseColor, skill.castTint, w * 0.8f);
                }
                if (caster.Health != null && caster.Health.Dead) break;
                yield return null;
                t += Time.deltaTime;
            }
            if (body != null) body.color = baseColor;
            float r = 0;
            while (r < skill.recovery) { r += Time.deltaTime; yield return null; }
        }

        static Planned PlanInstance(SkillCue cue, int i, int n, ISkillCaster caster, Vector2 self, Vector2 aimAt, System.Random rng)
        {
            int facing = caster.Facing >= 0 ? 1 : -1;
            Vector2 pos;
            switch (cue.anchor)
            {
                case CueAnchor.Target:
                case CueAnchor.TargetLive:
                    pos = aimAt + new Vector2(cue.offset.x * facing, cue.offset.y);
                    break;
                case CueAnchor.LineAtTarget:
                    pos = aimAt + new Vector2(SkillGeometry.LineOffset(i, n, cue.spread), 0) + cue.offset;
                    break;
                case CueAnchor.RandomNearTarget:
                    pos = aimAt + new Vector2(((float)rng.NextDouble() - 0.5f) * cue.spread, 0) + cue.offset;
                    break;
                case CueAnchor.FlankTarget:
                {
                    int k = i / 2 + 1, side = i % 2 == 0 ? -1 : 1;
                    pos = aimAt + new Vector2(side * k * cue.spread, 0) + cue.offset;
                    break;
                }
                case CueAnchor.RingAroundSelf:
                {
                    SkillGeometry.RingOffset(i, n, cue.spread, out var rx, out var ry);
                    pos = self + new Vector2(cue.offset.x * facing + rx, cue.offset.y + ry);
                    break;
                }
                default:
                    pos = self + new Vector2(cue.offset.x * facing, cue.offset.y);
                    break;
            }
            var p = new Planned { pos = pos, facing = facing, live = cue.anchor == CueAnchor.TargetLive };
            switch (cue.aim)
            {
                case CueAim.FaceTarget: p.facing = aimAt.x >= pos.x ? 1 : -1; if (Mathf.Abs(aimAt.x - pos.x) < 0.05f) p.facing = facing; break;
                case CueAim.AwayFromTarget: p.facing = aimAt.x >= self.x ? -1 : 1; break;
                case CueAim.RotateToTarget:
                {
                    var to = (aimAt + new Vector2(0, 1.2f)) - pos;   // aim at the body, not the feet
                    p.angle = Mathf.Atan2(to.y, to.x);
                    p.reach = to.magnitude;
                    p.facing = 1;
                    break;
                }
            }
            return p;
        }

        /// <summary>
        /// Mark where an instance will hurt, from now until it goes live (<paramref name="untilSpawn"/> + hit delay),
        /// then keep an outline while it stays live. A mark on a live-tracked target follows them until the instance
        /// spawns, then stays where the blow will land. Dashes mark the path, projectiles their lane.
        /// </summary>
        /// <summary>How far a projectile instance flies: its range, or to just past its aim point (projectileOvershoot).</summary>
        static float FlightRange(SkillCue cue, Planned plan) =>
            cue.projectileOvershoot >= 0 && plan.reach > 0 ? Mathf.Min(cue.projectileRange, plan.reach + cue.projectileOvershoot) : cue.projectileRange;

        static void Telegraph(SkillCue cue, Planned plan, float untilSpawn, float hitDelay, Hurtbox follow)
        {
            var h = cue.hit;
            float warn = Mathf.Max(0.05f, untilSpawn + hitDelay);
            SkillGeometry.Place(h.offset.x, h.offset.y, plan.facing, plan.angle, out var ox, out var oy);
            Vector2 at = plan.pos + new Vector2(ox, oy);
            float thick = h.shape == HitShape.Circle ? h.size.x * 2 : h.size.y;

            if (cue.action == CueAction.Dash && cue.actionValue > 0)
            {
                // the body sweeps its hit area along the floor: mark the whole stretch it covers
                float len = cue.actionValue + thick;
                var c = at + new Vector2(plan.facing * cue.actionValue / 2, 0);
                TelegraphMarker.Show(HitShape.Box, c, new Vector2(len, thick), 0, warn, null, default, 0);
                return;
            }
            if (cue.projectileSpeed > 0)
            {
                // the lane it flies down, from where it leaves until it reaches its range or the floor; it stays marked
                // while the lance is in flight
                var dir = new Vector2(Mathf.Cos(plan.angle), Mathf.Sin(plan.angle)) * (plan.facing < 0 && cue.aim != CueAim.RotateToTarget ? -1 : 1);
                float len = FlightRange(cue, plan);
                if (dir.y < -0.01f) len = Mathf.Min(len, (at.y + 0.2f) / -dir.y);
                // from where it leaves to where it stops, plus its own radius at both ends
                var c = at + dir * (len / 2);
                TelegraphMarker.Show(HitShape.Box, c, new Vector2(len + thick, thick), Mathf.Atan2(dir.y, dir.x), warn, null, default, len / cue.projectileSpeed);
                return;
            }
            float hold = cue.action != CueAction.Dash ? h.active : 0;   // an area stays outlined for as long as it can hurt
            TelegraphMarker.Show(h.shape, at, h.size, plan.angle, warn, follow, new Vector2(ox, oy), hold, follow != null ? untilSpawn : 0);
        }

        static void Spawn(BossSkill skill, SkillCue cue, Planned plan, ISkillCaster caster, Vector2 aimAt)
        {
            Transform parent = cue.followCaster ? caster.transform : cue.followTarget && caster.Target != null ? caster.Target.transform : null;
            GameObject root;
            PixelFxPlayer fx = null;
            bool anchored = cue.effectAnchor != Vector2.zero;
            if (cue.sheet != null && !anchored)
            {
                fx = PixelFxPlayer.Spawn(cue.sheet, plan.pos, plan.facing < 0, null);
                root = fx.gameObject;
            }
            else root = new GameObject(cue.label.Length > 0 ? cue.label : "cue");
            if (cue.sheet != null && anchored)
            {
                // the effect hangs off the root so that its anchor point sits on the root: the root is what rotates and hits
                fx = PixelFxPlayer.Spawn(cue.sheet, plan.pos, plan.facing < 0, root.transform);
                fx.transform.localPosition = new Vector3(-cue.effectAnchor.x * plan.facing, -cue.effectAnchor.y, 0);
                fx.destroyOnFinish = false;
            }
            if (fx != null)
            {
                fx.GetComponent<SpriteRenderer>().sortingOrder = cue.sortingOrder;
                if (cue.loopDuration > 0) fx.loopMode = PixelFxPlayer.LoopMode.Loop;
            }
            root.name = $"{skill.id}:{(cue.label.Length > 0 ? cue.label : "cue")}";
            root.transform.position = plan.pos;
            root.transform.rotation = Quaternion.Euler(0, 0, plan.angle * Mathf.Rad2Deg);
            if (parent != null) root.transform.SetParent(parent, true);

            float life = cue.loopDuration > 0 ? cue.loopDuration : (cue.sheet != null ? cue.sheet.data.TotalSeconds : 0);
            if (cue.hit.shape != HitShape.None)
            {
                var hv = root.AddComponent<HitVolume>();
                hv.Init(cue.hit, caster.Team, plan.facing, skill.id, caster as Object);
                life = Mathf.Max(life, cue.hit.delay + cue.hit.active + 0.05f);
            }
            if (cue.projectileSpeed > 0)
            {
                var pr = root.AddComponent<SkillProjectile>();
                pr.Init(new Vector2(Mathf.Cos(plan.angle), Mathf.Sin(plan.angle)) * (plan.facing < 0 && cue.aim != CueAim.RotateToTarget ? -1 : 1),
                    cue.projectileSpeed, FlightRange(cue, plan), cue.impactSheet, caster.Team);
                life = Mathf.Max(life, FlightRange(cue, plan) / cue.projectileSpeed + 0.1f);
            }
            if (fx == null || anchored || cue.loopDuration > 0 || cue.projectileSpeed > 0) Object.Destroy(root, Mathf.Max(0.1f, life));
            else if (fx != null && fx.Looping) Object.Destroy(root, Mathf.Max(0.1f, life));

            switch (cue.action)
            {
                case CueAction.Dash:
                    if (caster is MonoBehaviour mb) mb.StartCoroutine(Dash(caster, plan.facing, cue.actionValue, Mathf.Max(0.05f, cue.actionDuration)));
                    break;
                case CueAction.HealSelf:
                    if (caster.Health != null) caster.Health.Heal(caster.Health.max * cue.actionValue);
                    break;
                case CueAction.SpawnIllusions:
                    BossIllusion.SpawnRing(caster, cue.subSkill, Mathf.Max(1, Mathf.RoundToInt(cue.actionValue)), cue.spread, cue.impactSheet);
                    break;
            }
        }

        static IEnumerator Dash(ISkillCaster caster, int facing, float distance, float duration)
        {
            var tr = caster.transform;
            Vector2 start = tr.position;
            float dir = facing < 0 ? -1 : 1;   // the direction planned (and marked) when the pattern started
            caster.Facing = facing;
            Vector2 end = start + new Vector2(dir * distance, 0);
            float t = 0, ghost = 0;
            while (t < duration && tr != null)
            {
                t += Time.deltaTime;
                float u = 1 - Mathf.Pow(1 - Mathf.Clamp01(t / duration), 3);
                tr.position = new Vector3(Mathf.Lerp(start.x, end.x, u), start.y, tr.position.z);
                if ((ghost -= Time.deltaTime) <= 0 && caster.Body != null)
                {
                    ghost = 0.035f;
                    AfterImage.Spawn(caster.Body, new Color(0.55f, 0.4f, 0.9f, 0.6f), 0.3f);
                }
                yield return null;
            }
        }
    }

    /// <summary>A live hit area: checks hurtboxes of the other team, deals damage, roots, knocks back, pulls.</summary>
    public class HitVolume : MonoBehaviour
    {
        public SkillHit hit;
        public Team team;
        public int facing = 1;
        public string skillId;
        public Object source;

        float t;
        readonly Dictionary<Hurtbox, float> lastHit = new Dictionary<Hurtbox, float>();

        public void Init(SkillHit h, Team owner, int face, string id, Object src)
        {
            hit = h;
            team = owner;
            facing = face;
            skillId = id;
            source = src;
        }

        public Vector2 Center
        {
            get
            {
                float ang = transform.eulerAngles.z * Mathf.Deg2Rad;
                SkillGeometry.Place(hit.offset.x, hit.offset.y, facing, ang, out var ox, out var oy);
                return (Vector2)transform.position + new Vector2(ox, oy);
            }
        }

        bool singleDone;

        /// <summary>An area that only drags (no damage, root or knockback) never counts as a hit, so it costs no i-frames.</summary>
        bool PullOnly => hit.damage <= 0 && hit.root <= 0 && hit.knockback <= 0 && hit.pull > 0;

        /// <summary>A lasting area is live from delay to delay + active. A single hit (active 0) is live on exactly one
        /// frame: the first one at or after its delay, whatever the frame rate.</summary>
        public bool Live => hit.active > 0 ? t >= hit.delay && t <= hit.delay + hit.active : t >= hit.delay && !singleDone;

        void Update()
        {
            t += Time.deltaTime;
            if (hit == null || hit.shape == HitShape.None || !Live) return;
            var c = Center;
            float ang = transform.eulerAngles.z * Mathf.Deg2Rad;
            var list = Hurtbox.All;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var hb = list[i];
                if (hb == null || hb.team == team) continue;
                if (hit.pull > 0 && hb.Status != null && (hb.Center - c).magnitude <= Mathf.Max(hit.pullRadius, 0.01f))
                    hb.Status.Pull(c, hit.pull, Time.deltaTime);
                if (PullOnly || !Overlaps(hb, c, ang)) continue;
                if (lastHit.TryGetValue(hb, out var at) && (hit.tick <= 0 || t - at < hit.tick)) continue;
                lastHit[hb] = t;
                Deal(hb, c);
            }
            if (hit.active <= 0) singleDone = true;
        }

        bool Overlaps(Hurtbox hb, Vector2 c, float ang)
        {
            var p = hb.Center;
            if (hit.shape == HitShape.Circle) return SkillGeometry.CircleCircle(c.x, c.y, hit.size.x, p.x, p.y, hb.radius);
            return SkillGeometry.BoxCircle(c.x, c.y, hit.size.x, hit.size.y, ang, p.x, p.y, hb.radius);
        }

        void Deal(Hurtbox hb, Vector2 c)
        {
            var dir = hb.Center - c;
            dir.y = 0;
            if (dir.sqrMagnitude < 0.0001f) dir = new Vector2(facing, 0);
            var info = new HitInfo { damage = hit.damage, knockback = dir.normalized * hit.knockback, root = hit.root, skillId = skillId, source = source };
            if (hb.Health != null) hb.Health.TakeHit(info);
            else hb.Status?.Apply(info);
            if (hit.hitStop > 0) HitStop.Freeze(hit.hitStop);
            if (hit.shake > 0) ScreenShake.Shake(hit.shake, 0.25f);
        }

        void OnDrawGizmos()
        {
            if (hit == null || hit.shape == HitShape.None) return;
            Gizmos.color = Live ? new Color(1, 0.3f, 0.6f, 0.9f) : new Color(1, 1, 1, 0.25f);
            var c = Center;
            if (hit.shape == HitShape.Circle) Gizmos.DrawWireSphere(c, hit.size.x);
            else
            {
                var m = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(c, Quaternion.Euler(0, 0, transform.eulerAngles.z), Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, hit.size);
                Gizmos.matrix = m;
            }
        }
    }

    /// <summary>Moves a cue's instance in a straight line; ends on the first enemy hurtbox it touches.</summary>
    public class SkillProjectile : MonoBehaviour
    {
        Vector2 dir;
        float speed, range, travelled;
        PixelFxSheet impact;
        Team team;
        HitVolume volume;

        public void Init(Vector2 direction, float spd, float rng, PixelFxSheet impactSheet, Team owner)
        {
            dir = direction.normalized;
            speed = spd;
            range = rng;
            impact = impactSheet;
            team = owner;
            volume = GetComponent<HitVolume>();
        }

        void Update()
        {
            float step = speed * Time.deltaTime;
            transform.position += (Vector3)(dir * step);
            travelled += step;
            bool hitSomething = false;
            if (volume != null && volume.Live)
                foreach (var hb in Hurtbox.All)
                    if (hb.team != team && SkillGeometry.CircleCircle(volume.Center.x, volume.Center.y, volume.hit.size.x, hb.Center.x, hb.Center.y, hb.radius))
                    { hitSomething = true; break; }
            if (hitSomething || travelled >= range || transform.position.y < -0.2f)
            {
                if (impact != null) PixelFxPlayer.Spawn(impact, transform.position, dir.x < 0);
                // let the volume land its hit this frame, then go
                enabled = false;
                Destroy(gameObject, hitSomething ? 0.02f : 0f);
            }
        }
    }
}
