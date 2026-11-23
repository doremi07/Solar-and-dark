using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SolarDark.Combat.Editor
{
    /// <summary>Tools > Solar and Dark > Create Boss Arena: a playable test scene for each boss (builds the boss assets first if needed).</summary>
    public static class BossArenaScene
    {
        public const string ScenesDir = "Assets/Game/Scenes";

        [MenuItem("Tools/Solar and Dark/Create Boss Arena/세레나 (Serena)", priority = 10)]
        public static void Serena() => Create("Serena", PlaceholderBody.Kind.Serena);

        [MenuItem("Tools/Solar and Dark/Create Boss Arena/움켜쥔 아이 (Hidden Child)", priority = 11)]
        public static void Child() => Create("HiddenChild", PlaceholderBody.Kind.HiddenChild);

        public static void Create(string folder, PlaceholderBody.Kind kind)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var def = AssetDatabase.LoadAssetAtPath<BossDefinition>($"{BossLibrary.OutDir}/{folder}/{folder}.asset");
            if (def == null)
            {
                var built = BossLibrary.Build();
                def = folder == "Serena" ? built.serena : built.child;
            }
            var scene = Build(def, kind);
            System.IO.Directory.CreateDirectory(ScenesDir);
            var path = $"{ScenesDir}/BossArena_{folder}.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"Solar and Dark: arena for {def.displayName} saved to {path}. Press Play.");
        }

        public static UnityEngine.SceneManagement.Scene Build(BossDefinition def, PlaceholderBody.Kind kind)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.transform.position = new Vector3(0, 6f, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.05f, 0.1f);

            new GameObject("Arena").AddComponent<ArenaBackdrop>();

            // the player stand-in
            var player = new GameObject("Player");
            player.transform.position = new Vector3(-5, 0, 0);
            var psr = new GameObject("Body").AddComponent<SpriteRenderer>();
            psr.transform.SetParent(player.transform, false);
            psr.sortingOrder = 10;
            psr.gameObject.AddComponent<PlaceholderBody>().kind = PlaceholderBody.Kind.Hero;
            var ph = player.AddComponent<Health>();
            ph.max = ph.current = 200;
            ph.invulnerableAfterHit = 0.35f;
            var phb = player.AddComponent<Hurtbox>();
            phb.team = Team.Player;
            phb.radius = 0.55f;
            phb.offset = new Vector2(0, 0.8f);
            player.AddComponent<StatusReceiver>();
            player.AddComponent<PlayerDummy>();

            // the boss
            var boss = new GameObject(def.displayName);
            boss.transform.position = new Vector3(4, 0, 0);
            var bsr = new GameObject("Body").AddComponent<SpriteRenderer>();
            bsr.transform.SetParent(boss.transform, false);
            bsr.sortingOrder = 10;
            bsr.gameObject.AddComponent<PlaceholderBody>().kind = kind;   // until the first phase's body sheets take over
            bsr.gameObject.AddComponent<BossBody>();
            var bh = boss.AddComponent<Health>();
            bh.invulnerableAfterHit = 0.1f;
            var bhb = boss.AddComponent<Hurtbox>();
            bhb.team = Team.Enemy;
            bhb.radius = 0.8f;
            bhb.offset = new Vector2(0, 1.2f);
            var bc = boss.AddComponent<BossController>();
            bc.definition = def;
            bc.body = bsr;
            bc.target = phb;

            var hud = new GameObject("HUD").AddComponent<ArenaHud>();
            hud.boss = bc;
            hud.player = ph;
            return scene;
        }
    }
}
