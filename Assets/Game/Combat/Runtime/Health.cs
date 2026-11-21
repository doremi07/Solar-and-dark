using System;
using UnityEngine;
using UnityEngine.Events;

namespace SolarDark.Combat
{
    public enum Team { Player, Enemy }

    /// <summary>What one hit does. Knockback is a velocity in world units per second.</summary>
    [Serializable]
    public struct HitInfo
    {
        public float damage;
        public Vector2 knockback;
        public float root;
        public string skillId;
        public UnityEngine.Object source;
    }

    public interface IDamageable
    {
        void TakeHit(HitInfo hit);
    }

    /// <summary>Hit points. Bosses read <see cref="Fraction"/> to change phase.</summary>
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, IDamageable
    {
        [Serializable] public class HitEvent : UnityEvent<HitInfo> { }
        [Serializable] public class FloatEvent : UnityEvent<float> { }

        [Min(1)] public float max = 100;
        public float current = 100;
        [Tooltip("Seconds of invulnerability after a hit (0 = none). Ticking areas still respect it.")]
        public float invulnerableAfterHit = 0.25f;
        public bool invulnerable;

        public HitEvent onHit = new HitEvent();
        public FloatEvent onChanged = new FloatEvent();
        public UnityEvent onDied = new UnityEvent();

        float iframes;
        public float Fraction => max > 0 ? Mathf.Clamp01(current / max) : 0;
        public bool Dead => current <= 0;

        void Awake() => current = Mathf.Clamp(current, 0, max);

        void Update()
        {
            if (iframes > 0) iframes -= Time.deltaTime;
        }

        public void TakeHit(HitInfo hit)
        {
            if (Dead || invulnerable || iframes > 0) return;
            current = Mathf.Max(0, current - hit.damage);
            iframes = invulnerableAfterHit;
            if (TryGetComponent<StatusReceiver>(out var st)) st.Apply(hit);
            onHit.Invoke(hit);
            onChanged.Invoke(Fraction);
            if (Dead) onDied.Invoke();
        }

        public void Heal(float amount)
        {
            if (Dead) return;
            current = Mathf.Min(max, current + amount);
            onChanged.Invoke(Fraction);
        }

        public void ResetFull()
        {
            current = max;
            onChanged.Invoke(Fraction);
        }
    }
}
