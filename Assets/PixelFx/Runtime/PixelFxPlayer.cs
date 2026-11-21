using System;
using UnityEngine;
using UnityEngine.Events;

namespace PixelFx
{
    /// <summary>
    /// Plays a <see cref="PixelFxSheet"/> on a SpriteRenderer at the sheet's own frame timing.
    /// Put the GameObject where the effect's origin goes (e.g. the feet for a ground effect); flipX mirrors it to face left.
    /// One-shots finish by themselves (and destroy the object if <see cref="destroyOnFinish"/>); <see cref="Stop"/> ends a loop.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class PixelFxPlayer : MonoBehaviour
    {
        public enum LoopMode { FromSheet, Loop, Once }

        [Serializable] public class HitEvent : UnityEvent<PixelFxHit> { }

        public PixelFxSheet sheet;
        public bool playOnEnable = true;
        public LoopMode loopMode = LoopMode.FromSheet;
        [Min(0)] public float speed = 1;
        public bool flipX;
        [Tooltip("Destroy this GameObject when a one-shot ends.")]
        public bool destroyOnFinish = true;
        [Tooltip("Pause this effect for each act hit's freeze time (hit-stop). Freeze your characters too via onHit.")]
        public bool applyHitStop = true;
        [Tooltip("Uses unscaled time, so the effect keeps playing while Time.timeScale is 0.")]
        public bool unscaledTime;

        [Tooltip("Fires at each hit of the effect's act (meta.pixelfx.act.hits): freeze, shake and push the target here.")]
        public HitEvent onHit = new HitEvent();
        public UnityEvent onFinished = new UnityEvent();

        SpriteRenderer sr;
        float time, freezeLeft;
        int nextHit, shown = -1;
        bool playing;

        public bool IsPlaying => playing;
        public float Time => time;
        public bool Looping => loopMode == LoopMode.Loop || (loopMode == LoopMode.FromSheet && sheet != null && sheet.data.loop);

        void Awake() => sr = GetComponent<SpriteRenderer>();

        void OnEnable()
        {
            if (playOnEnable) Play();
        }

        /// <summary>Spawn a one-shot (or loop) at a position. Returns the player so you can Stop() it.</summary>
        public static PixelFxPlayer Spawn(PixelFxSheet sheet, Vector3 position, bool flipLeft = false, Transform parent = null)
        {
            var go = new GameObject(sheet != null ? sheet.name : "pixelfx");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<SpriteRenderer>();
            var p = go.AddComponent<PixelFxPlayer>();
            p.sheet = sheet;
            p.flipX = flipLeft;
            go.SetActive(true);
            return p;
        }

        public void Play()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            time = 0;
            freezeLeft = 0;
            nextHit = 0;
            shown = -1;
            playing = sheet != null && sheet.FrameCount > 0;
            Show();
        }

        /// <summary>Stop now. A looping effect has no natural end, so this is how you end one.</summary>
        public void Stop()
        {
            if (!playing) return;
            playing = false;
            Finish();
        }

        void Update()
        {
            if (!playing) return;
            float dt = (unscaledTime ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime) * speed;
            if (freezeLeft > 0)
            {
                float used = Mathf.Min(freezeLeft, dt);
                freezeLeft -= used;
                dt -= used;
                if (dt <= 0) return;
            }
            time += dt;
            FireHits();
            Show();
        }

        void FireHits()
        {
            var hits = sheet.data.hits;
            if (hits == null || sheet.data.bakedAct) return; // a baked act already has its freezes in the frames
            float period = sheet.data.TotalSeconds;
            while (nextHit < hits.Count)
            {
                var h = hits[nextHit];
                if (time < h.t) break;
                nextHit++;
                if (applyHitStop) freezeLeft += h.freeze;
                onHit.Invoke(h);
            }
            if (Looping && period > 0 && time >= period)
            {
                time %= period;
                nextHit = 0;
            }
        }

        void Show()
        {
            if (sheet == null) return;
            int i = sheet.data.FrameAt(time, Looping, out bool finished);
            if (i >= 0 && i != shown)
            {
                var sprites = sheet.Sprites;
                if (i < sprites.Length) sr.sprite = sprites[i];
                shown = i;
            }
            sr.flipX = flipX;
            if (finished && playing)
            {
                playing = false;
                Finish();
            }
        }

        void Finish()
        {
            onFinished.Invoke();
            if (destroyOnFinish && Application.isPlaying) Destroy(gameObject);
            else if (sr != null) sr.sprite = null;
        }
    }
}
