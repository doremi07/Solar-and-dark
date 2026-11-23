using PixelFx;
using UnityEngine;

namespace SolarDark.Combat
{
    /// <summary>
    /// Plays a boss's body from its phase's sprite sheets (tools/sprites/make.py, 128 x 128 from the concept art):
    /// the idle loop; the skill's motion (strike / slam / channel), sped up or slowed so its release frame
    /// (<see cref="castRelease"/> seconds into the sheet) lands exactly when the wind-up ends, which is when the skill's
    /// effect bursts; a channel then holds its lit pose (frames <see cref="holdFrom"/>..<see cref="holdTo"/>, looped) for
    /// as long as the spell runs; and the hurt flinch, which never interrupts an attack.
    /// <see cref="BossController"/> drives it; flipping is the SpriteRenderer's flipX.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BossBody : MonoBehaviour
    {
        public PixelFxSheet idle, hurt, strike, slam, channel;
        [Tooltip("Seconds into a motion sheet where the blow is released (make.py: frame 4 at 12 fps).")]
        public float castRelease = 4f / 12f;
        [Tooltip("The channel sheet's held frames (make.py CHANNEL_HOLD).")]
        public int holdFrom = 5, holdTo = 10;

        enum Mode { Idle, Cast, Hurt }
        SpriteRenderer sr;
        PixelFxSheet cast;
        BodyMotion motion;
        float t, speed = 1, holdLeft;
        Mode mode;

        public bool Casting => mode == Mode.Cast;

        void Awake() => sr = GetComponent<SpriteRenderer>();

        public void SetSheets(PixelFxSheet idleSheet, PixelFxSheet hurtSheet, PixelFxSheet strikeSheet, PixelFxSheet slamSheet, PixelFxSheet channelSheet)
        {
            idle = idleSheet;
            hurt = hurtSheet;
            strike = strikeSheet;
            slam = slamSheet;
            channel = channelSheet;
            if (mode == Mode.Cast) cast = Pick(motion);   // mid-attack (a phase change): carry on in the new look
            else { mode = Mode.Idle; t = 0; }
            Show();
        }

        /// <summary>
        /// Play <paramref name="motion"/> so its release lands after <paramref name="windup"/> seconds; a channel then holds
        /// for <paramref name="hold"/> more seconds (the spell's own length after the wind-up).
        /// </summary>
        public void PlayCast(BodyMotion motion, float windup, float hold)
        {
            this.motion = motion;
            cast = Pick(motion);
            if (cast == null) return;
            speed = Mathf.Clamp(castRelease / Mathf.Max(0.05f, windup), 0.35f, 3f);
            holdLeft = motion == BodyMotion.Channel && cast == channel ? Mathf.Max(0, hold) : 0;
            mode = Mode.Cast;
            t = 0;
            Show();
        }

        /// <summary>Flinch, unless an attack is playing (the attack reads first).</summary>
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
            float dt = Time.deltaTime;
            if (mode == Mode.Cast && t >= castRelease)
            {
                speed = 1;   // after the release the motion plays at its own pace, whatever the wind-up asked
                if (holdLeft > 0)
                {
                    holdLeft -= dt;
                    float fd = 1f / Mathf.Max(1, cast.data.fps), a = holdFrom * fd, b = (holdTo + 1) * fd;
                    if (t + dt >= b) { t = a + (t + dt - b) % (b - a); Show(); return; }   // loop the held, lit frames
                }
            }
            t += dt * (mode == Mode.Idle ? 1 : speed);
            var sheet = Current;
            if (mode != Mode.Idle && (sheet == null || t >= sheet.data.TotalSeconds))
            {
                mode = Mode.Idle;
                t = 0;
            }
            Show();
        }

        PixelFxSheet Pick(BodyMotion m)
        {
            var s = m == BodyMotion.Slam ? slam : m == BodyMotion.Channel ? channel : strike;
            return s != null ? s : strike;
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
