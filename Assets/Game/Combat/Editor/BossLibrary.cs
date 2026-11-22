using System.Collections.Generic;
using System.IO;
using PixelFx;
using PixelFx.Editor;
using UnityEditor;
using UnityEngine;

namespace SolarDark.Combat.Editor
{
    /// <summary>
    /// Serena (그림자가 된 수호자) and the Hidden Child (움켜쥔 아이) as data: every attack pattern from the concept
    /// sheets, built into BossSkill / BossDefinition assets under Assets/Game/Bosses.
    /// Units: 16 px = 1 unit, the effects' own scale (a 38 px character is 2.4 units tall). Hit times come from the
    /// effects' own constants (e.g. holy/smite HIT = 0.54 s, holy/holy-pillar HIT = 0.66 s).
    /// Sheets: tools/pixelfx/boss-fx.json → Assets/PixelFx/Sheets/bosses.
    /// </summary>
    public static class BossLibrary
    {
        public const string SheetsDir = "Assets/PixelFx/Sheets/bosses";
        public const string OutDir = "Assets/Game/Bosses";

        static readonly Color Violet = new Color(0.72f, 0.58f, 1f), Gold = new Color(1f, 0.86f, 0.62f), Lavender = new Color(0.86f, 0.8f, 1f);

        [MenuItem("Tools/Solar and Dark/Build Boss Assets (overwrite)", priority = 0)]
        public static void BuildMenu()
        {
            if (!EditorUtility.DisplayDialog("Build boss assets", $"Create or overwrite the boss and skill assets in {OutDir}?\nTweaks made to those assets will be replaced.", "Build", "Cancel")) return;
            Build();
        }

        public static (BossDefinition serena, BossDefinition child) Build()
        {
            PixelFxWindow.ReimportAll(); // make sure every sheet asset exists
            var serena = BuildSerena();
            var child = BuildChild();
            AssetDatabase.SaveAssets();
            Debug.Log($"Solar and Dark: built {serena.displayName} ({Count(serena)} skills) and {child.displayName} ({Count(child)} skills) in {OutDir}");
            return (serena, child);
        }

        static int Count(BossDefinition d)
        {
            var set = new HashSet<BossSkill>();
            foreach (var p in d.phases) foreach (var s in p.skills) set.Add(s);
            return set.Count;
        }

        // ---------------------------------------------------------------- Serena

        static BossDefinition BuildSerena()
        {
            const string B = "Serena";

            var shadowSlash = Skill(B, "serena.shadow-slash", "그림자 베기", 0.45f, 0.5f, 3.5f, 3f, 0, 4.5f,
                "A dark crescent reaps the space in front of her.",
                new SkillCue
                {
                    label = "slash", sheet = S("serena-shadow-slash"), anchor = CueAnchor.Self, offset = new Vector2(0.4f, 0), aim = CueAim.FaceTarget,
                    hit = Box(5f, 3f, 2.2f, 1.5f, 0.14f, 12, knockback: 7, hitStop: 0.12f, shake: 0.15f), telegraphLead = 0.45f,
                });

            var wideSlash = Skill(B, "serena.wide-slash", "광역 슬래시", 0.65f, 0.6f, 6f, 2f, 0, 7f,
                "A low sweep in front, then one behind: jump-or-dash timing.",
                new SkillCue
                {
                    label = "front", sheet = S("serena-wide-slash"), aim = CueAim.FaceTarget,
                    hit = Box(11f, 1.4f, 1f, 0.6f, 0.1f, 10, knockback: 6, hitStop: 0.1f), telegraphLead = 0.65f,
                },
                new SkillCue
                {
                    label = "back", time = 0.5f, sheet = S("serena-wide-slash"), aim = CueAim.AwayFromTarget,
                    hit = Box(11f, 1.4f, 1f, 0.6f, 0.1f, 10, knockback: 6, hitStop: 0.1f), telegraphLead = 0.4f,
                });

            var shadowFall = Skill(B, "serena.shadow-fall", "그림자 낙하", 0.6f, 0.7f, 7f, 2f, 0, 99,
                "Pools of shadow drop where the target stands and tendrils lash up out of them, one after another.",
                new SkillCue
                {
                    label = "pool", sheet = S("serena-shadow-fall"), anchor = CueAnchor.RandomNearTarget, spread = 3f, count = 3, interval = 0.4f,
                    hit = Circle(2f, 0, 1.4f, 0.32f, 8, active: 1.1f, tick: 0.45f, knockback: 3), telegraphLead = 0.75f,
                });

            var chains = Skill(B, "serena.shadow-chains", "그림자 사슬 · 속박", 0.5f, 0.6f, 10f, 1.5f, 0, 99,
                "A sigil under the target; chains lash up and bind them (root) until they shatter.",
                new SkillCue
                {
                    label = "chains", sheet = S("serena-shadow-chains"), anchor = CueAnchor.Target, sortingOrder = 25,
                    hit = Circle(1.3f, 0, 1.2f, 0.78f, 6, root: 1.2f, hitStop: 0.08f), telegraphLead = 0.9f,
                });

            var rift = Skill(B, "serena.rift", "빛과 그림자의 균열", 0.7f, 0.7f, 8f, 1.5f, 0, 12,
                "A crack races along the floor towards the target and shadow spikes burst up behind it.",
                new SkillCue
                {
                    label = "rift", sheet = S("serena-rift"), anchor = CueAnchor.Self, offset = new Vector2(0.6f, 0), aim = CueAim.FaceTarget,
                    hit = Box(10.75f, 3f, 5.4f, 1.5f, 0.18f, 14, active: 0.35f, knockback: 5, hitStop: 0.08f, shake: 0.12f), telegraphLead = 0.7f,
                });

            var riftTwin = Skill(B, "serena.rift-twin", "빛과 그림자의 균열 (양방향)", 0.8f, 0.8f, 9f, 1.5f, 0, 99,
                "Phase 2+: the rift tears both ways at once.",
                new SkillCue
                {
                    label = "front", sheet = S("serena-rift"), offset = new Vector2(0.6f, 0), aim = CueAim.FaceTarget,
                    hit = Box(10.75f, 3f, 5.4f, 1.5f, 0.18f, 14, active: 0.35f, knockback: 5, hitStop: 0.08f), telegraphLead = 0.8f,
                },
                new SkillCue
                {
                    label = "back", sheet = S("serena-rift"), offset = new Vector2(-0.6f, 0), aim = CueAim.AwayFromTarget,
                    hit = Box(10.75f, 3f, 5.4f, 1.5f, 0.18f, 14, active: 0.35f, knockback: 5), telegraphLead = 0.8f,
                });

            var pillar = Skill(B, "serena.shadow-pillar", "그림자 기둥", 0.4f, 0.8f, 10f, 1.2f, 0, 99,
                "Circles draw themselves round the target; a column of shadow fires up out of each.",
                new SkillCue
                {
                    label = "pillar", sheet = S("serena-shadow-pillar"), anchor = CueAnchor.LineAtTarget, spread = 5f, count = 3, interval = 0.35f,
                    hit = Box(1.6f, 12f, 0, 6f, 1.52f, 12, active: 0.9f, tick: 0.3f, knockback: 4, shake: 0.12f), telegraphLead = 1.5f,
                });

            var domain = Skill(B, "serena.holy-domain", "신성의 영역 · 영역 장악", 0.8f, 1.0f, 14f, 1f, 0, 99,
                "She claims the floor round her: standing inside burns, then the whole circle erupts in a column of light.",
                new SkillCue
                {
                    label = "domain", sheet = S("serena-holy-domain"), anchor = CueAnchor.Self, loopDuration = 3.2f, followCaster = false, sortingOrder = 2,
                    hit = Circle(3.6f, 0, 0.6f, 0.3f, 3, active: 2.9f, tick: 0.5f), telegraph = false,
                },
                new SkillCue
                {
                    label = "burst", time = 3.0f, sheet = S("serena-holy-domain-burst"), anchor = CueAnchor.Self,
                    hit = Circle(3.6f, 0, 1.2f, 1.52f, 22, knockback: 9, hitStop: 0.15f, shake: 0.3f), telegraphLead = 1.5f,
                });

            var judgment = Skill(B, "serena.judgment", "심판의 빛", 0.5f, 0.8f, 9f, 1.2f, 0, 99,
                "A star of judgment hangs over the target and follows them, then a bolt of light drops on where they are.",
                new SkillCue
                {
                    label = "mark", sheet = S("serena-judgment-mark"), anchor = CueAnchor.TargetLive, offset = new Vector2(0, 4.2f), loopDuration = 1.1f, followTarget = true, telegraph = false, sortingOrder = 30,
                    hit = new SkillHit(),
                },
                new SkillCue
                {
                    label = "smite", time = 1.1f, sheet = S("serena-judgment"), anchor = CueAnchor.TargetLive,
                    hit = Circle(1.5f, 0, 1f, 0.54f, 22, knockback: 6, hitStop: 0.15f, shake: 0.25f), telegraphLead = 0.54f,
                });

            var judgmentTriple = Skill(B, "serena.judgment-triple", "심판의 빛 (연속)", 0.5f, 1.0f, 12f, 1f, 0, 99,
                "Phase 3: three judgments in a row, each locked where the target stood.",
                new SkillCue
                {
                    label = "smite", sheet = S("serena-judgment"), anchor = CueAnchor.TargetLive, count = 3, interval = 0.7f,
                    hit = Circle(1.5f, 0, 1f, 0.54f, 18, knockback: 5, hitStop: 0.12f, shake: 0.2f), telegraphLead = 0.54f,
                },
                new SkillCue
                {
                    label = "mark", sheet = S("serena-judgment-mark"), anchor = CueAnchor.TargetLive, offset = new Vector2(0, 4.2f), loopDuration = 2.2f, followTarget = true, telegraph = false, sortingOrder = 30,
                    hit = new SkillHit(),
                });

            var awaken = Skill(B, "serena.phase-shift", "정화 — 각성", 0.2f, 0.8f, 0, 0, 0, 99,
                "Phase entry: a column of light pushes everything away.",
                new SkillCue
                {
                    label = "burst", sheet = S("serena-holy-domain-burst"), anchor = CueAnchor.Self,
                    hit = Circle(3f, 0, 1.2f, 1.52f, 8, knockback: 12, shake: 0.3f), telegraphLead = 1.5f,
                });
            var fuse = Skill(B, "serena.phase-fuse", "융합", 0.2f, 0.6f, 0, 0, 0, 99,
                "Phase entry: shadow pillars either side of her.",
                new SkillCue
                {
                    label = "front", sheet = S("serena-shadow-pillar"), anchor = CueAnchor.Self, offset = new Vector2(3f, 0),
                    hit = Box(1.6f, 12f, 0, 6f, 1.52f, 10, active: 0.6f, knockback: 8), telegraphLead = 1.5f,
                },
                new SkillCue
                {
                    label = "back", time = 0.15f, sheet = S("serena-shadow-pillar"), anchor = CueAnchor.Self, offset = new Vector2(-3f, 0),
                    hit = Box(1.6f, 12f, 0, 6f, 1.52f, 10, active: 0.6f, knockback: 8), telegraphLead = 1.5f,
                });

            var halo = S("serena-halo");
            var def = Boss("Serena", "serena", "세레나", "그림자가 된 수호자", "\"…네가, 여기 있었구나.\"", 1400, 4.5f, new Vector2(0.62f, 6.75f),
                new BossPhase
                {
                    name = "그림자", enterAtHp = 1f, halo = halo, haloTint = new Color(0.55f, 0.45f, 0.65f, 0.75f), bodyTint = Color.white, idleSheet = C("serena-p1-idle"), castSheet = C("serena-p1-cast"),
                    skills = { shadowSlash, shadowFall, chains, wideSlash }, idleTime = new Vector2(0.9f, 1.5f), moveSpeed = 2.4f,
                },
                new BossPhase
                {
                    name = "융합", enterAtHp = 0.66f, halo = halo, haloTint = new Color(0.85f, 0.75f, 0.85f, 0.9f), bodyTint = Color.white, idleSheet = C("serena-p2-idle"), castSheet = C("serena-p2-cast"),
                    entrySkill = fuse, skills = { shadowSlash, wideSlash, shadowFall, chains, rift, riftTwin, pillar }, idleTime = new Vector2(0.7f, 1.2f), moveSpeed = 2.8f,
                },
                new BossPhase
                {
                    name = "정화", enterAtHp = 0.33f, halo = halo, haloTint = Color.white, bodyTint = Color.white, idleSheet = C("serena-p3-idle"), castSheet = C("serena-p3-cast"),
                    entrySkill = awaken, skills = { shadowSlash, riftTwin, pillar, domain, judgment, judgmentTriple, chains }, idleTime = new Vector2(0.5f, 1f), moveSpeed = 3.2f,
                });
            return def;
        }

        // ---------------------------------------------------------------- the Hidden Child

        static BossDefinition BuildChild()
        {
            const string B = "HiddenChild";

            var lightFall = Skill(B, "child.light-fall", "빛의 낙하", 0.6f, 0.7f, 6f, 2f, 0, 99,
                "Shards of light rain across the floor round the target.",
                new SkillCue
                {
                    label = "rain", sheet = S("child-light-fall"), anchor = CueAnchor.RandomNearTarget, spread = 4f, count = 2, interval = 0.5f,
                    hit = Circle(2.9f, 0, 0.8f, 0.4f, 6, active: 1.1f, tick: 0.35f), telegraphLead = 0.7f,
                });

            var crystals = Skill(B, "child.countless-crystals", "무수한 결정", 0.4f, 0.8f, 8f, 1.5f, 0, 99,
                "Six crystals bloom round her, hang, then fire at the target one by one.",
                new SkillCue
                {
                    label = "crystals", sheet = S("child-crystal"), anchor = CueAnchor.RingAroundSelf, spread = 2.2f, offset = new Vector2(0, 1.2f), count = 6, interval = 0.08f,
                    loopDuration = 1.3f, telegraph = false, hit = new SkillHit(), sortingOrder = 22,
                },
                new SkillCue
                {
                    label = "lances", time = 1.3f, sheet = S("child-crystal-lance"), anchor = CueAnchor.RingAroundSelf, spread = 2.2f, offset = new Vector2(0, 1.5f), count = 6, interval = 0.12f,
                    aim = CueAim.RotateToTarget, loopDuration = 1.2f, projectileSpeed = 14f, projectileRange = 16f, impactSheet = S("child-sparkle"),
                    hit = Circle(0.45f, 0, 0, 0, 6, active: 1.2f, knockback: 3), telegraph = false,
                });

            var lightBurst = Skill(B, "child.light-burst", "빛의 폭발", 0.7f, 0.6f, 5f, 1.5f, 0, 4.5f,
                "Light gathers in her and blows out in a ring.",
                new SkillCue
                {
                    label = "burst", sheet = S("child-light-burst"), anchor = CueAnchor.Self,
                    hit = Circle(3f, 0, 1.75f, 0.22f, 14, knockback: 9, hitStop: 0.1f, shake: 0.2f), telegraphLead = 0.9f,
                });

            var beam = Skill(B, "child.shadow-beam", "그림자 광선", 0.9f, 0.9f, 8f, 1.5f, 3f, 99,
                "She locks on and fires a shadow beam at where the target stood.",
                new SkillCue
                {
                    // arcane-beam fires from the hand (HX 15, HY -24 px) and is L = 168 px long: rotate round the hand
                    label = "beam", sheet = S("child-shadow-beam"), anchor = CueAnchor.Self, offset = new Vector2(0.6f, 1.6f), aim = CueAim.RotateToTarget,
                    effectAnchor = new Vector2(15 / 16f, 24 / 16f), loopDuration = 1.8f, followCaster = true, sortingOrder = 24,
                    hit = Box(10.5f, 0.9f, 5.25f, 0, 0.05f, 4, active: 1.7f, tick: 0.2f, knockback: 2), telegraphLead = 0.9f,
                });

            var explosion = Skill(B, "child.shadow-explosion", "그림자 폭발", 0.5f, 0.8f, 8f, 1.5f, 0, 99,
                "A black star where the target stood: it gathers, then bursts.",
                new SkillCue
                {
                    label = "blast", sheet = S("child-shadow-explosion"), anchor = CueAnchor.Target,
                    hit = Circle(3f, 0, 2.6f, 0.52f, 20, knockback: 10, hitStop: 0.15f, shake: 0.35f), telegraphLead = 1.0f,
                });

            var dash = Skill(B, "child.shadow-dash", "그림자 돌진", 0.55f, 0.7f, 6f, 1.5f, 2f, 9f,
                "She blurs across the floor through the target, leaving after-images.",
                new SkillCue
                {
                    label = "dash", sheet = S("child-shadow-dash"), anchor = CueAnchor.Self, offset = new Vector2(0.3f, 0), aim = CueAim.FaceTarget, followCaster = true,
                    action = CueAction.Dash, actionValue = 8f, actionDuration = 0.35f,
                    hit = Circle(1.1f, 0.4f, 1.2f, 0, 14, active: 0.4f, knockback: 8, hitStop: 0.1f), telegraph = false,
                });

            var gravity = Skill(B, "child.gravity", "중력 조작", 0.6f, 0.8f, 11f, 1.2f, 0, 99,
                "A void opens over the target and drags everything towards it.",
                new SkillCue
                {
                    label = "well", sheet = S("child-gravity"), anchor = CueAnchor.Target, offset = new Vector2(0, -2.0f), loopDuration = 3f, sortingOrder = 18,
                    hit = Circle(1.6f, 0, 4.7f, 0.2f, 4, active: 2.7f, tick: 0.4f, pull: 2.6f, pullRadius: 7f), telegraphLead = 0.8f,
                });

            var illusionLance = Skill(B, "child.illusion-lance", "환영의 결정", 0.35f, 0.2f, 0, 1, 0, 99,
                "What each illusion casts: one crystal lance at the target.",
                new SkillCue
                {
                    label = "lance", sheet = S("child-crystal-lance"), anchor = CueAnchor.Self, offset = new Vector2(0, 1.4f), aim = CueAim.RotateToTarget,
                    loopDuration = 1.2f, projectileSpeed = 12f, projectileRange = 14f, impactSheet = S("child-sparkle"),
                    hit = Circle(0.45f, 0, 0, 0, 6, active: 1.2f, knockback: 3), telegraph = false,
                });

            var illusions = Skill(B, "child.illusions", "수많은 환영", 0.5f, 1.2f, 12f, 1f, 0, 99,
                "Mirror images flicker in round the target and each throws a crystal.",
                new SkillCue
                {
                    label = "illusions", anchor = CueAnchor.Self, action = CueAction.SpawnIllusions, actionValue = 4, spread = 3.2f,
                    subSkill = illusionLance, impactSheet = S("child-sparkle"), actionDuration = 1.6f, hit = new SkillHit(), telegraph = false,
                });

            var despair = Skill(B, "child.despair", "절망의 영역", 0.8f, 0.8f, 14f, 1f, 0, 99,
                "A field of despair: a black hole hangs over the floor, burning and slowly dragging whoever stands under it.",
                new SkillCue
                {
                    label = "field", sheet = S("child-despair"), anchor = CueAnchor.Target, loopDuration = 5f, sortingOrder = 3,
                    hit = Circle(3.2f, 0, 1f, 0.3f, 3, active: 4.6f, tick: 0.4f, pull: 1.2f, pullRadius: 4.5f), telegraphLead = 0.8f,
                });

            var absorb = Skill(B, "child.light-absorb", "빛의 흡수", 0.5f, 1.0f, 15f, 1f, 0, 99,
                "She drinks the light: everything is pulled in, she heals, and it bursts out of her.",
                new SkillCue
                {
                    // implosion: SUCK until 0.8 s, still at 1.02 s, then it blows out
                    label = "absorb", sheet = S("child-light-absorb"), anchor = CueAnchor.Self, offset = new Vector2(0, 1.6f), followCaster = true,
                    hit = Circle(2.6f, 0, 0, 1.02f, 18, knockback: 11, hitStop: 0.15f, shake: 0.3f), telegraphLead = 1.0f,
                    action = CueAction.HealSelf, actionValue = 0.04f,
                },
                new SkillCue
                {
                    label = "pull", anchor = CueAnchor.Self, offset = new Vector2(0, 1.6f), telegraph = false,
                    hit = Circle(0.1f, 0, 0, 0, 0, active: 0.95f, pull: 5f, pullRadius: 8f),
                });

            var infinite = Skill(B, "child.infinite-light", "무한한 빛", 0.6f, 1.2f, 13f, 1f, 0, 99,
                "Pillars of light come down in a line across the floor, closing on the target.",
                new SkillCue
                {
                    label = "pillars", sheet = S("child-infinite-light"), anchor = CueAnchor.LineAtTarget, spread = 12f, count = 5, interval = 0.22f,
                    hit = Box(1.4f, 12f, 0, 6f, 0.66f, 9, active: 1.3f, tick: 0.35f, knockback: 3, shake: 0.08f), telegraphLead = 0.9f,
                });

            var berserk = Skill(B, "child.phase-berserk", "폭주", 0.2f, 0.8f, 0, 0, 0, 99,
                "Phase entry: a burst of light that throws everything back.",
                new SkillCue
                {
                    label = "burst", sheet = S("child-light-burst"), anchor = CueAnchor.Self,
                    hit = Circle(4f, 0, 1.75f, 0.22f, 8, knockback: 14, shake: 0.3f), telegraphLead = 0.4f,
                });
            var shadowForm = Skill(B, "child.phase-shadow", "완전한 그림자", 0.3f, 1.0f, 0, 0, 0, 99,
                "Phase entry: she drinks the light round her.",
                new SkillCue
                {
                    label = "absorb", sheet = S("child-light-absorb"), anchor = CueAnchor.Self, offset = new Vector2(0, 1.6f),
                    hit = Circle(3f, 0, 0, 1.02f, 10, knockback: 12, shake: 0.3f), telegraphLead = 1.0f,
                    action = CueAction.HealSelf, actionValue = 0.02f,
                },
                new SkillCue
                {
                    label = "pull", anchor = CueAnchor.Self, offset = new Vector2(0, 1.6f), telegraph = false,
                    hit = Circle(0.1f, 0, 0, 0, 0, active: 0.95f, pull: 6f, pullRadius: 9f),
                });

            var halo = S("child-halo");
            return Boss("HiddenChild", "hidden-child", "움켜쥔 아이", "최종 보스 — 빛을 잔뜩 삼킨, 열 살의 슬라", "\"…모두, 다 내 거야.\"", 2000, 6f, new Vector2(0.19f, 4.25f),
                new BossPhase
                {
                    name = "억눌린 빛", enterAtHp = 1f, halo = halo, haloTint = new Color(0.9f, 0.85f, 1f, 0.85f), bodyTint = Color.white, idleSheet = C("child-p1-idle"), castSheet = C("child-p1-cast"),
                    skills = { lightFall, crystals, lightBurst, beam }, idleTime = new Vector2(0.9f, 1.4f), moveSpeed = 2f,
                },
                new BossPhase
                {
                    name = "폭주", enterAtHp = 0.6f, halo = halo, haloTint = new Color(0.8f, 0.65f, 1f, 1f), bodyTint = Color.white, idleSheet = C("child-p2-idle"), castSheet = C("child-p2-cast"),
                    entrySkill = berserk, skills = { lightFall, crystals, beam, explosion, dash, gravity, illusions }, idleTime = new Vector2(0.6f, 1.1f), moveSpeed = 2.8f,
                },
                new BossPhase
                {
                    name = "완전한 그림자", enterAtHp = 0.25f, halo = halo, haloTint = new Color(0.55f, 0.4f, 0.8f, 1f), bodyTint = Color.white, idleSheet = C("child-p3-idle"), castSheet = C("child-p3-cast"),
                    entrySkill = shadowForm, skills = { explosion, dash, gravity, illusions, despair, absorb, infinite, beam }, idleTime = new Vector2(0.4f, 0.9f), moveSpeed = 3.4f,
                });
        }

        // ---------------------------------------------------------------- helpers

        public const string CharactersDir = "Assets/PixelFx/Sheets/characters";

        /// <summary>A character sheet (tools/pixelfx/boss-characters.json).</summary>
        static PixelFxSheet C(string name)
        {
            var path = $"{CharactersDir}/{name}.asset";
            var s = AssetDatabase.LoadAssetAtPath<PixelFxSheet>(path);
            if (s == null) Debug.LogWarning($"Solar and Dark: missing character sheet {path}. Run node tools/pixelfx/export-set.mjs tools/pixelfx/boss-characters.json.");
            return s;
        }

        static PixelFxSheet S(string name)
        {
            var path = $"{SheetsDir}/{name}.asset";
            var s = AssetDatabase.LoadAssetAtPath<PixelFxSheet>(path);
            if (s == null) Debug.LogWarning($"Solar and Dark: missing sheet {path}. Run node tools/pixelfx/export-set.mjs tools/pixelfx/boss-fx.json, then Tools > pixelfx > Reimport All Sheets.");
            return s;
        }

        static SkillHit Circle(float r, float ox, float oy, float delay, float damage, float active = 0, float tick = 0, float knockback = 0,
            float hitStop = 0, float shake = 0, float root = 0, float pull = 0, float pullRadius = 0) =>
            new SkillHit
            {
                shape = HitShape.Circle, size = new Vector2(r, r), offset = new Vector2(ox, oy), delay = delay, active = active, tick = tick,
                damage = damage, knockback = knockback, hitStop = hitStop, shake = shake, root = root, pull = pull, pullRadius = pullRadius,
            };

        static SkillHit Box(float w, float h, float ox, float oy, float delay, float damage, float active = 0, float tick = 0, float knockback = 0,
            float hitStop = 0, float shake = 0, float root = 0) =>
            new SkillHit
            {
                shape = HitShape.Box, size = new Vector2(w, h), offset = new Vector2(ox, oy), delay = delay, active = active, tick = tick,
                damage = damage, knockback = knockback, hitStop = hitStop, shake = shake, root = root,
            };

        static BossSkill Skill(string boss, string id, string name, float windup, float recovery, float cooldown, float weight, float minRange, float maxRange,
            string notes, params SkillCue[] cues)
        {
            var s = ScriptableObject.CreateInstance<BossSkill>();
            s.id = id;
            s.displayName = name;
            s.notes = notes;
            s.windup = windup;
            s.recovery = recovery;
            s.cooldown = cooldown;
            s.weight = weight;
            s.minRange = minRange;
            s.maxRange = maxRange;
            s.animTrigger = id.Substring(id.IndexOf('.') + 1);
            s.castTint = boss == "Serena" ? (id.Contains("judgment") || id.Contains("domain") ? Gold : Violet) : Lavender;
            s.cues = new List<SkillCue>(cues);
            return Save(s, $"{OutDir}/{boss}/Skills/{id.Substring(id.IndexOf('.') + 1)}.asset");
        }

        static BossDefinition Boss(string folder, string id, string name, string title, string quote, float hp, float range, Vector2 haloOffset, params BossPhase[] phases)
        {
            var d = ScriptableObject.CreateInstance<BossDefinition>();
            d.id = id;
            d.displayName = name;
            d.title = title;
            d.quote = quote;
            d.maxHp = hp;
            d.preferredRange = range;
            d.haloOffset = haloOffset;
            // hurtbox over the drawn body: the head's height tells the size (Serena ~7.4 units tall, the child ~4.4)
            d.hurtRadius = haloOffset.y > 5 ? 1.5f : 1.1f;
            d.hurtOffset = new Vector2(haloOffset.x * 0.8f, haloOffset.y * 0.5f);
            d.phases = new List<BossPhase>(phases);
            return Save(d, $"{OutDir}/{folder}/{folder}.asset");
        }

        /// <summary>Write a fresh instance over the asset at path, keeping its GUID so references survive.</summary>
        static T Save<T>(T fresh, string path) where T : ScriptableObject
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(fresh, path);
                return fresh;
            }
            EditorUtility.CopySerialized(fresh, existing);
            existing.name = Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(fresh);
            return existing;
        }
    }
}
