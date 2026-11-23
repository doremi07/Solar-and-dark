using System;
using System.Collections.Generic;
using PixelFx;
using UnityEngine;

namespace SolarDark.Combat
{
    [Serializable]
    public class BossPhase
    {
        public string name = "";
        [Range(0, 1)] [Tooltip("Enter this phase when HP falls to this fraction or below (the first phase uses 1).")]
        public float enterAtHp = 1;
        public List<BossSkill> skills = new List<BossSkill>();
        [Tooltip("Cast once on entering the phase (while invulnerable).")]
        public BossSkill entrySkill;
        [Tooltip("A halo effect over the body. Leave empty when the body sprites draw their own (the concept-art bodies do).")]
        public PixelFxSheet halo;
        [Tooltip("The body's idle loop for this phase (tools/sprites/make.py). Needs a BossBody on the body.")]
        public PixelFxSheet idleSheet;
        [Tooltip("The body's cast; its release frame is timed to each skill's wind-up.")]
        public PixelFxSheet castSheet;
        [Tooltip("The body's flinch when hit (skipped while casting).")]
        public PixelFxSheet hurtSheet;
        public Color haloTint = Color.white;
        public Color bodyTint = Color.white;
        [Tooltip("Seconds between attacks: random in this range.")]
        public Vector2 idleTime = new Vector2(0.8f, 1.4f);
        public float moveSpeed = 2.5f;
    }

    [CreateAssetMenu(menuName = "Solar and Dark/Boss", fileName = "Boss")]
    public class BossDefinition : ScriptableObject
    {
        public string id = "";
        public string displayName = "";
        public string title = "";
        [TextArea] public string quote = "";
        public float maxHp = 1000;
        [Tooltip("Horizontal distance the boss tries to keep from the target between attacks.")]
        public float preferredRange = 5;
        [Tooltip("Halo centre from the feet, facing right; mirrored when the boss faces left. Default: the ring floats just over a 38 px body's head.")]
        public Vector2 haloOffset = new Vector2(0, 3.5f);
        [Tooltip("The boss's hurtbox: radius and centre from the feet (applied to its Hurtbox on Awake).")]
        public float hurtRadius = 0.8f;
        public Vector2 hurtOffset = new Vector2(0, 1.2f);
        public List<BossPhase> phases = new List<BossPhase>();

        /// <summary>The phase for an HP fraction: the last phase whose threshold it has reached.</summary>
        public int PhaseFor(float hpFraction)
        {
            int best = 0;
            for (int i = 0; i < phases.Count; i++)
                if (hpFraction <= phases[i].enterAtHp + 1e-4f) best = i;
            return best;
        }
    }
}
