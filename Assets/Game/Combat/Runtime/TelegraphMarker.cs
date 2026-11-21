using System.Collections.Generic;
using UnityEngine;

namespace SolarDark.Combat
{
    /// <summary>
    /// The danger mark for a hit area before it goes live: a pixel outline that fills up as the hit approaches,
    /// then flashes and goes. Drawn at the game's pixel scale (16 px per unit) so it sits with the effects.
    /// </summary>
    public class TelegraphMarker : MonoBehaviour
    {
        public static Color DangerColor = new Color(0.78f, 0.55f, 1f, 1f);

        static readonly Dictionary<long, Sprite> cache = new Dictionary<long, Sprite>();

        SpriteRenderer outline, fill;
        float t, life;
        Hurtbox follow;
        Vector2 followOffset;
        HitShape shape;
        Vector2 size;

        public static TelegraphMarker Show(HitShape shape, Vector2 center, Vector2 size, float angle, float seconds, Hurtbox follow = null, Vector2 followOffset = default)
        {
            if (shape == HitShape.None || seconds <= 0) return null;
            var go = new GameObject("telegraph");
            go.transform.position = center;
            go.transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            var m = go.AddComponent<TelegraphMarker>();
            m.shape = shape;
            m.size = size;
            m.life = seconds;
            m.follow = follow;
            m.followOffset = followOffset;
            m.fill = Layer(go, "fill", MakeSprite(shape, size, true), 4);
            m.outline = Layer(go, "outline", MakeSprite(shape, size, false), 5);
            m.Tick();
            return m;
        }

        static SpriteRenderer Layer(GameObject parent, string name, Sprite s, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        void Update()
        {
            t += Time.deltaTime;
            if (follow != null) transform.position = follow.Feet + followOffset;
            Tick();
            if (t >= life + 0.1f) Destroy(gameObject);
        }

        void Tick()
        {
            float u = Mathf.Clamp01(t / life);
            bool flash = t >= life;
            var c = DangerColor;
            // the fill grows from the centre and thickens; the outline pulses faster as the hit nears
            float pulse = 0.55f + 0.45f * Mathf.Sin(t * Mathf.Lerp(8f, 26f, u));
            outline.color = flash ? Color.white : new Color(c.r, c.g, c.b, Mathf.Lerp(0.5f, 1f, u) * pulse + 0.2f);
            fill.color = flash ? new Color(1, 1, 1, 0.6f) : new Color(c.r, c.g, c.b, Mathf.Lerp(0.08f, 0.32f, u));
            float s = flash ? 1f : Mathf.Lerp(0.15f, 1f, Mathf.SmoothStep(0, 1, u));
            fill.transform.localScale = shape == HitShape.Box ? new Vector3(1, s, 1) : new Vector3(s, s, 1);
        }

        /// <summary>A pixel-art circle or box, outline or dithered fill, cached by size.</summary>
        static Sprite MakeSprite(HitShape shape, Vector2 size, bool filled)
        {
            const float ppu = SkillRunner.PixelsPerUnit;
            int w, h;
            if (shape == HitShape.Circle) w = h = Mathf.Max(3, Mathf.RoundToInt(size.x * 2 * ppu));
            else { w = Mathf.Max(3, Mathf.RoundToInt(size.x * ppu)); h = Mathf.Max(3, Mathf.RoundToInt(size.y * ppu)); }
            w = Mathf.Min(w, 2048);
            h = Mathf.Min(h, 2048);
            long key = ((long)w << 32) | ((long)h << 2) | ((shape == HitShape.Circle ? 1L : 0L) << 1) | (filled ? 1L : 0L);
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            float cx = (w - 1) / 2f, cy = (h - 1) / 2f, r = Mathf.Min(w, h) / 2f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool inside, edge;
                    if (shape == HitShape.Circle)
                    {
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        inside = d <= r - 0.5f;
                        edge = inside && d > r - 1.6f;
                    }
                    else
                    {
                        inside = true;
                        edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                    }
                    byte a = 0;
                    if (filled) a = inside && ((x + y) % 2 == 0 || (x % 4 == 0 && y % 4 == 0)) ? (byte)255 : (byte)0;
                    else a = edge ? (byte)255 : (byte)0;
                    px[y * w + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
            s.name = $"telegraph {w}x{h}";
            cache[key] = s;
            return s;
        }
    }

    /// <summary>A fading copy of a sprite: dash trails, illusion vanishings.</summary>
    public class AfterImage : MonoBehaviour
    {
        SpriteRenderer sr;
        float life, t;
        Color c;

        public static void Spawn(SpriteRenderer from, Color tint, float seconds)
        {
            if (from == null || from.sprite == null) return;
            var go = new GameObject("afterimage");
            go.transform.SetPositionAndRotation(from.transform.position, from.transform.rotation);
            go.transform.localScale = from.transform.lossyScale;
            var a = go.AddComponent<AfterImage>();
            a.sr = go.AddComponent<SpriteRenderer>();
            a.sr.sprite = from.sprite;
            a.sr.flipX = from.flipX;
            a.sr.sortingOrder = from.sortingOrder - 1;
            a.c = tint;
            a.life = seconds;
            a.sr.color = tint;
        }

        void Update()
        {
            t += Time.deltaTime;
            sr.color = new Color(c.r, c.g, c.b, c.a * (1 - t / life));
            if (t >= life) Destroy(gameObject);
        }
    }
}
