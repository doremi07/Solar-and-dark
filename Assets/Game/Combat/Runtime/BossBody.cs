using PixelFx;
using UnityEngine;

namespace SolarDark.Combat
{
    /// <summary>
    /// Plays a boss's body animation from pixelfx character sheets: the phase's idle loop, and its cast animation timed
    /// so the cut / burst frame (<see cref="castRelease"/> seconds into the sheet) lands exactly when the skill's
    /// wind-up ends. <see cref="BossController"/> drives it; flipping is the SpriteRenderer's flipX.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BossBody : MonoBehaviour
    {
        public PixelFxSheet idle;
        public PixelFxSheet cast;
        [Tooltip("Seconds into the cast sheet where the blow lands (sd/serena-cast and sd/child-cast: 0.35).")]
        public float castRelease = 0.35f;

        SpriteRenderer sr;
        float t, speed = 1;
        bool casting;

        public bool Casting => casting;

        void Awake() => sr = GetComponent<SpriteRenderer>();

        public void SetSheets(PixelFxSheet idleSheet, PixelFxSheet castSheet)
        {
            idle = idleSheet;
            cast = castSheet;
            casting = false;
            t = 0;
            Show();
        }

        /// <summary>Play the cast so its release lands after <paramref name="windup"/> seconds.</summary>
        public void PlayCast(float windup)
        {
            if (cast == null) return;
            speed = Mathf.Clamp(castRelease / Mathf.Max(0.05f, windup), 0.35f, 3f);
            casting = true;
            t = 0;
            Show();
        }

        void Update()
        {
            t += Time.deltaTime * (casting ? speed : 1);
            if (casting && cast != null && t >= cast.data.TotalSeconds)
            {
                casting = false;
                t = 0;
            }
            Show();
        }

        void Show()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            var sheet = casting ? cast : idle;
            if (sheet == null || sheet.FrameCount == 0) return;
            int i = sheet.data.FrameAt(t, !casting, out _);
            var sprites = sheet.Sprites;
            if (i >= 0 && i < sprites.Length) sr.sprite = sprites[i];
        }
    }
}
