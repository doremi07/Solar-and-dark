using PixelFx;
using UnityEngine;

namespace SolarDark.Combat
{
    /// <summary>
    /// Plays a boss's body from its phase's sprite sheets (tools/sprites/make.py, 128 x 128 from the concept art):
    /// the idle loop; the cast, sped up or slowed so its release frame (<see cref="castRelease"/> seconds into the sheet)
    /// lands exactly when the skill's wind-up ends; and the hurt flinch, which never interrupts a cast.
    /// <see cref="BossController"/> drives it; flipping is the SpriteRenderer's flipX.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BossBody : MonoBehaviour
    {
        public PixelFxSheet idle;
        public PixelFxSheet cast;
        public PixelFxSheet hurt;
        [Tooltip("Seconds into the cast sheet where the blow is released (make.py: frame 4 at 12 fps).")]
        public float castRelease = 4f / 12f;

        enum Mode { Idle, Cast, Hurt }
        SpriteRenderer sr;
        float t, speed = 1;
        Mode mode;

        public bool Casting => mode == Mode.Cast;

        void Awake() => sr = GetComponent<SpriteRenderer>();

        public void SetSheets(PixelFxSheet idleSheet, PixelFxSheet castSheet, PixelFxSheet hurtSheet)
        {
            idle = idleSheet;
            cast = castSheet;
            hurt = hurtSheet;
            mode = Mode.Idle;
            t = 0;
            Show();
        }

        /// <summary>Play the cast so its release lands after <paramref name="windup"/> seconds.</summary>
        public void PlayCast(float windup)
        {
            if (cast == null) return;
            speed = Mathf.Clamp(castRelease / Mathf.Max(0.05f, windup), 0.35f, 3f);
            mode = Mode.Cast;
            t = 0;
            Show();
        }

        /// <summary>Flinch, unless a cast is playing (the attack reads first).</summary>
        public void PlayHurt()
        {
            if (hurt == null || mode == Mode.Cast) return;
            speed = 1;
            mode = Mode.Hurt;
            t = 0;
            Show();
        }

        void Update()
        {
            t += Time.deltaTime * (mode == Mode.Idle ? 1 : speed);
            var sheet = Current;
            if (mode != Mode.Idle && (sheet == null || t >= sheet.data.TotalSeconds))
            {
                mode = Mode.Idle;
                t = 0;
            }
            Show();
        }

        PixelFxSheet Current => mode == Mode.Cast ? cast : mode == Mode.Hurt ? hurt : idle;

        void Show()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            var sheet = Current;
            if (sheet == null || sheet.FrameCount == 0) return;
            int i = sheet.data.FrameAt(t, mode == Mode.Idle, out _);
            var sprites = sheet.Sprites;
            if (i >= 0 && i < sprites.Length) sr.sprite = sprites[i];
        }
    }
}
