using UnityEngine;

namespace SolarDark.Combat
{
    /// <summary>
    /// A stand-in player for testing boss fights: ←/→ (or A/D) to move, Shift to dash, Z/J to strike the boss.
    /// Respects root from skills. Replace with the real player controller; keep Health + Hurtbox + StatusReceiver.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(Hurtbox), typeof(StatusReceiver))]
    public class PlayerDummy : MonoBehaviour
    {
        public float speed = 5f;
        public float dashDistance = 3f;
        public float strikeDamage = 40f;
        public float strikeRange = 2.2f;
        public float arenaHalfWidth = 11f;

        StatusReceiver status;
        SpriteRenderer sr;
        float dashCd, strikeCd, flash;
        float autoT;
        [Tooltip("No keyboard (or for a soak test): walk back and forth and strike on its own.")]
        public bool autoPilot;

        void Awake()
        {
            status = GetComponent<StatusReceiver>();
            sr = GetComponentInChildren<SpriteRenderer>();
            GetComponent<Health>().onHit.AddListener(_ => flash = 0.15f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            dashCd -= dt;
            strikeCd -= dt;
            if (sr != null)
            {
                flash -= dt;
                sr.color = flash > 0 ? new Color(1, 0.45f, 0.6f) : status.IsRooted ? new Color(0.7f, 0.6f, 1f) : Color.white;
            }
            if (GetComponent<Health>().Dead) return;

            float move = 0;
            bool dash = false, strike = false;
#if ENABLE_LEGACY_INPUT_MANAGER
            move = Input.GetAxisRaw("Horizontal");
            dash = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K);
            strike = Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.J);
#endif
            if (autoPilot)
            {
                autoT += dt;
                move = Mathf.Sin(autoT * 0.9f) > 0 ? 1 : -1;
                strike = strikeCd <= 0 && Mathf.Repeat(autoT, 1.3f) < dt;
            }
            if (!status.CanAct) return;
            if (move != 0 && sr != null) sr.flipX = move < 0;
            var p = transform.position;
            p.x += move * speed * dt;
            if (dash && dashCd <= 0 && move != 0)
            {
                p.x += Mathf.Sign(move) * dashDistance;
                dashCd = 0.6f;
                AfterImage.Spawn(sr, new Color(0.6f, 0.9f, 1f, 0.6f), 0.25f);
            }
            p.x = Mathf.Clamp(p.x, -arenaHalfWidth, arenaHalfWidth);
            transform.position = p;
            if (strike && strikeCd <= 0)
            {
                strikeCd = 0.35f;
                var boss = Hurtbox.Nearest(Team.Enemy, transform.position);
                if (boss != null && Mathf.Abs(boss.Feet.x - p.x) <= strikeRange && boss.Health != null)
                    boss.Health.TakeHit(new HitInfo { damage = strikeDamage, skillId = "player-strike", source = this });
            }
        }
    }

    /// <summary>Arena HUD and debug keys: HP bars, phase and skill names; 1-3 jump phase, N casts the next skill in the list.</summary>
    public class ArenaHud : MonoBehaviour
    {
        public BossController boss;
        public Health player;
        int debugIndex;
        GUIStyle big, small;

        void Update()
        {
            if (boss == null || boss.definition == null) return;
#if ENABLE_LEGACY_INPUT_MANAGER
            for (int i = 0; i < 3; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) boss.JumpToPhase(i);
            if (Input.GetKeyDown(KeyCode.N)) CastNext();
            if (Input.GetKeyDown(KeyCode.R) && player != null) { player.ResetFull(); boss.Health.ResetFull(); }
#endif
        }

        /// <summary>Queue the next skill of the current phase (cycles through them all).</summary>
        public void CastNext()
        {
            var p = boss.CurrentPhase;
            if (p == null || p.skills.Count == 0) return;
            boss.ForceNext(p.skills[debugIndex++ % p.skills.Count]);
        }

        void OnGUI()
        {
            if (boss == null || boss.definition == null) return;
            if (big == null)
            {
                big = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
                small = new GUIStyle(GUI.skin.label) { fontSize = 13 };
                big.normal.textColor = new Color(0.95f, 0.9f, 1f);
                small.normal.textColor = new Color(0.85f, 0.8f, 0.95f);
            }
            var d = boss.definition;
            float w = Screen.width * 0.6f, x = (Screen.width - w) / 2;
            GUI.Label(new Rect(x, 8, w, 28), $"{d.displayName} — {d.title}", big);
            Bar(new Rect(x, 38, w, 12), boss.Health.Fraction, new Color(0.6f, 0.4f, 0.95f));
            var ph = boss.CurrentPhase;
            GUI.Label(new Rect(x, 52, w, 20), $"{boss.Phase + 1}페이즈 — {ph?.name}    {(boss.Casting != null ? "▶ " + boss.Casting.displayName : "")}", small);
            if (player != null)
            {
                GUI.Label(new Rect(16, Screen.height - 58, 300, 20), "Player", small);
                Bar(new Rect(16, Screen.height - 38, 260, 10), player.Fraction, new Color(0.5f, 0.85f, 1f));
            }
            GUI.Label(new Rect(Screen.width - 380, Screen.height - 40, 370, 30), "←/→ move  Shift dash  Z strike  1-3 phase  N next skill  R reset", small);
        }

        static void Bar(Rect r, float k, Color c)
        {
            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, (r.width - 2) * Mathf.Clamp01(k), r.height - 2), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }

    /// <summary>Placeholder pixel sprites for characters until real art exists (24×38 px, the effects' scale).</summary>
    public static class PlaceholderSprites
    {
        // rows top to bottom; . empty, o outline, h hair, s skin, d dress dark, l dress light, g gold, e eye
        static readonly string[] Serena =
        {
            "........hhhhhh..........", ".......hhhhhhhh.........", "......hhhsssshhh........", "......hhsesseshh........",
            "......hhhsssshhh........", ".....hhhhhsshhhhh.......", ".....hhhdddddddhhh......", "....hhhddglllgddhhh.....",
            "....hhdddglllgdddhh.....", "....hhddddlllddddhh.....", "...hhsddddlllddddshh....", "...hhs.ddddddddd.shh....",
            "...hh..ddddgdddd..hh....", "...hh..dddllldddd.hh....", "...h..dddlllllddd..h....", "......dddlllllldddd.....",
            ".....ddddlllllldddd.....", ".....dddllllllllddd.....", "....ddddllllllllddddd...", "....dddlllllllllldddd...",
            "...ddddlllllllllldddd...", "...dddllllllllllllddd...", "..ddddllllllllllllddddd.", "..dddlllllllllllllldddd.",
            "..ddddddddddddddddddddd.", "...dddd..dddddd..dddd...", "........dd....dd........", "........dd....dd........",
            "........dd....dd........", "........dd....dd........", "........dd....dd........", "........dd....dd........",
            "........dd....dd........", "........dd....dd........", "........dd....dd........", ".......ddd....ddd.......",
            "......dddd....dddd......", "........................",
        };

        static readonly string[] Child =
        {
            "........................", "........................", "........................", "........................",
            "........................", "........................", "........................", "........hhhhhhh.........",
            ".......hhhhhhhhh........", "......hhhsssssshh.......", "......hhsesssseshh......", "......hhhssssssshh......",
            ".....hhhhhssssshhhh.....", ".....hhhhllllllhhhh.....", "....hhhhlllollllhhhh....", "....hhhllllllllllhhh....",
            "....hh.sllllllllls.hh...", "...hh..sllllllllls..hh..", "...h...llllllllll...h...", ".......lllllllllll......",
            "......llllllllllll......", "......lllllllllllll.....", ".....llllllllllllll.....", ".....lllllllllllllll....",
            "....dlllllllllllllld....", "....ddddddddddddddd.....", ".......ss.....ss........", ".......ss.....ss........",
            ".......ss.....ss........", ".......ss.....ss........", ".......ss.....ss........", ".......ss.....ss........",
            ".......ss.....ss........", ".......ss.....ss........", ".......ss.....ss........", "......ddd.....ddd.......",
            ".....dddd.....dddd......", "........................",
        };

        static readonly string[] Hero =
        {
            "........................", "........................", "........................", "........................",
            ".........hhhhh..........", "........hhhhhhh.........", "........hssssshh........", "........hseseshh........",
            "........hsssssh.........", ".........sssss..........", "........ddddddd.........", ".......ddlllllddd........",
            "......dddlllllddds......", "......ss.lllll..ss......", "......ss.lllll..ss......", "......ss.lllll..ss......",
            ".........ddddd..........", ".........dd.dd..........", ".........dd.dd..........", ".........dd.dd..........",
            ".........dd.dd..........", ".........dd.dd..........", ".........dd.dd..........", "........ddd.ddd.........",
        };

        public static Sprite SerenaSprite() => Make(Serena, "serena", new Color32(212, 206, 236, 255), new Color32(238, 222, 214, 255), new Color32(36, 26, 58, 255), new Color32(222, 214, 240, 255));
        public static Sprite ChildSprite() => Make(Child, "child", new Color32(214, 204, 240, 255), new Color32(240, 226, 222, 255), new Color32(52, 38, 82, 255), new Color32(236, 232, 250, 255));
        public static Sprite HeroSprite() => Make(Hero, "hero", new Color32(90, 70, 60, 255), new Color32(236, 206, 180, 255), new Color32(40, 60, 90, 255), new Color32(120, 170, 210, 255));

        static Sprite Make(string[] rows, string name, Color32 hair, Color32 skin, Color32 dark, Color32 light)
        {
            int h = rows.Length, w = 24;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = name };
            var px = new Color32[w * h];
            var gold = new Color32(232, 192, 138, 255);
            for (int r = 0; r < h; r++)
                for (int c = 0; c < w; c++)
                {
                    char ch = c < rows[r].Length ? rows[r][c] : '.';
                    Color32 col = ch switch
                    {
                        'h' => hair, 's' => skin, 'd' => dark, 'l' => light, 'g' => gold,
                        'e' => new Color32(60, 40, 90, 255), 'o' => new Color32(20, 12, 30, 255), _ => new Color32(0, 0, 0, 0),
                    };
                    px[(h - 1 - r) * w + c] = col;
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0), SkillRunner.PixelsPerUnit, 0, SpriteMeshType.FullRect);
            s.name = name;
            return s;
        }
    }
}

namespace SolarDark.Combat
{
    /// <summary>Puts a placeholder sprite on this SpriteRenderer at runtime if it has none (scene files can't hold generated sprites).</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(-100)]
    public class PlaceholderBody : UnityEngine.MonoBehaviour
    {
        public enum Kind { Serena, HiddenChild, Hero }
        public Kind kind;

        void Awake()
        {
            var sr = GetComponent<UnityEngine.SpriteRenderer>();
            if (sr.sprite != null) return;
            sr.sprite = kind == Kind.Serena ? PlaceholderSprites.SerenaSprite() : kind == Kind.HiddenChild ? PlaceholderSprites.ChildSprite() : PlaceholderSprites.HeroSprite();
        }
    }
}

namespace SolarDark.Combat
{
    /// <summary>A plain arena backdrop built at runtime: a floor strip at y = 0 and a dim back wall with ruined pillars.</summary>
    public class ArenaBackdrop : UnityEngine.MonoBehaviour
    {
        public float halfWidth = 14f;
        public UnityEngine.Color floor = new UnityEngine.Color(0.16f, 0.13f, 0.22f);
        public UnityEngine.Color wall = new UnityEngine.Color(0.1f, 0.08f, 0.15f);

        void Start()
        {
            var white = UnityEngine.Texture2D.whiteTexture;
            UnityEngine.Sprite Box() => UnityEngine.Sprite.Create(white, new UnityEngine.Rect(0, 0, white.width, white.height), new UnityEngine.Vector2(0.5f, 1f), white.width);
            Make("Floor", Box(), floor, new UnityEngine.Vector3(0, 0, 0), new UnityEngine.Vector3(halfWidth * 2 + 8, 6, 1), -20);
            Make("FloorEdge", Box(), new UnityEngine.Color(0.32f, 0.27f, 0.42f), new UnityEngine.Vector3(0, 0.0625f, 0), new UnityEngine.Vector3(halfWidth * 2 + 8, 0.0625f, 1), -19);
            for (int i = -3; i <= 3; i++)
            {
                float h = 4f + (i * 37 % 5 + 5) % 5 * 0.8f;
                Make("Pillar", Box(), wall, new UnityEngine.Vector3(i * 4.2f, h, 0), new UnityEngine.Vector3(1.4f, h, 1), -30);
            }
        }

        void Make(string name, UnityEngine.Sprite s, UnityEngine.Color c, UnityEngine.Vector3 pos, UnityEngine.Vector3 scale, int order)
        {
            var go = new UnityEngine.GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<UnityEngine.SpriteRenderer>();
            sr.sprite = s;
            sr.color = c;
            sr.sortingOrder = order;
        }
    }
}
