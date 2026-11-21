using System.Collections.Generic;
using UnityEngine;

namespace SolarDark.Combat
{
    /// <summary>
    /// A circle that skills can hit. Kept in a registry instead of using physics colliders, so skills work in any
    /// project without layer or collider setup. Position is the feet; <see cref="offset"/> lifts the circle onto the body.
    /// </summary>
    [DisallowMultipleComponent]
    public class Hurtbox : MonoBehaviour
    {
        static readonly List<Hurtbox> all = new List<Hurtbox>();
        public static IReadOnlyList<Hurtbox> All => all;

        public Team team = Team.Player;
        [Min(0.05f)] public float radius = 0.6f;
        public Vector2 offset = new Vector2(0, 1.2f);

        Health health;
        public Health Health => health != null ? health : health = GetComponent<Health>();
        public StatusReceiver Status => GetComponent<StatusReceiver>();
        public Vector2 Center => (Vector2)transform.position + offset;
        public Vector2 Feet => transform.position;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        /// <summary>The nearest live hurtbox on <paramref name="team"/>, or null.</summary>
        public static Hurtbox Nearest(Team team, Vector2 from)
        {
            Hurtbox best = null;
            float bd = float.MaxValue;
            foreach (var h in all)
            {
                if (h.team != team || (h.Health != null && h.Health.Dead)) continue;
                float d = (h.Center - from).sqrMagnitude;
                if (d < bd) { bd = d; best = h; }
            }
            return best;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = team == Team.Player ? Color.cyan : Color.magenta;
            Gizmos.DrawWireSphere(Center, radius);
        }
    }
}
