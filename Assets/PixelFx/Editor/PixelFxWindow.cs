using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PixelFx.Editor
{
    /// <summary>
    /// Tools > pixelfx > Render &amp; Import Effect…: search the pinned pixelfx library (third_party/pixelfx), render an
    /// effect to a sprite sheet with Node, and import it into Assets/PixelFx/Sheets as a sheet asset + prefab.
    /// </summary>
    public class PixelFxWindow : EditorWindow
    {
        string query = "explosion";
        string effectId = "explosions/small-explosion";
        string outName = "";
        string optionsJson = "";
        bool bakeAct;
        Vector2 scroll;
        readonly List<string[]> results = new List<string[]>();
        string lastLog = "";

        static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static string PixelFxRoot => Path.Combine(ProjectRoot, "third_party", "pixelfx");
        static string NodeKey => "PixelFx.Node." + Application.dataPath.GetHashCode();

        static string NodePath
        {
            get => EditorPrefs.GetString(NodeKey, GuessNode());
            set => EditorPrefs.SetString(NodeKey, value);
        }

        static string GuessNode()
        {
            // Unity launched from the Dock/Hub often has a short PATH, so look in the usual places too.
            string[] candidates =
            {
                "/opt/homebrew/bin/node", "/usr/local/bin/node", "/usr/bin/node",
                @"C:\Program Files\nodejs\node.exe",
            };
            foreach (var c in candidates) if (File.Exists(c)) return c;
            return Application.platform == RuntimePlatform.WindowsEditor ? "node.exe" : "node";
        }

        [MenuItem("Tools/pixelfx/Render && Import Effect…", priority = 0)]
        public static void Open() => GetWindow<PixelFxWindow>("pixelfx").Show();

        [MenuItem("Tools/pixelfx/Import Sheet From File…", priority = 1)]
        public static void ImportFromFile()
        {
            var json = EditorUtility.OpenFilePanel("pixelfx sprite sheet JSON", ProjectRoot, "json");
            if (string.IsNullOrEmpty(json)) return;
            var png = Path.ChangeExtension(json, ".png");
            if (!File.Exists(png)) { EditorUtility.DisplayDialog("pixelfx", "No PNG next to " + json, "OK"); return; }
            PixelFxSheetData data;
            try { data = PixelFxSheetData.Parse(File.ReadAllText(json)); }
            catch (System.Exception e) { EditorUtility.DisplayDialog("pixelfx", "Not a pixelfx sheet: " + e.Message, "OK"); return; }
            var cat = data.id.Contains("/") ? data.id.Split('/')[0] : "imported";
            var dir = Path.Combine(ProjectRoot, PixelFxSheetPostprocessor.SheetsRoot, cat);
            Directory.CreateDirectory(dir);
            var name = Path.GetFileNameWithoutExtension(json);
            File.Copy(png, Path.Combine(dir, name + ".png"), true);
            File.Copy(json, Path.Combine(dir, name + ".json"), true);
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/pixelfx/Reimport All Sheets", priority = 2)]
        public static void ReimportAll()
        {
            var root = Path.Combine(ProjectRoot, PixelFxSheetPostprocessor.SheetsRoot);
            if (!Directory.Exists(root)) return;
            int n = 0;
            foreach (var json in Directory.GetFiles(root, "*.json", SearchOption.AllDirectories))
            {
                var rel = "Assets" + json.Substring(Application.dataPath.Length).Replace('\\', '/');
                AssetDatabase.ImportAsset(Path.ChangeExtension(rel, ".png"), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                PixelFxSheetPostprocessor.BuildAssets(rel);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"pixelfx: reimported {n} sheets");
        }

        [MenuItem("Tools/pixelfx/Open Effect Catalog", priority = 20)]
        public static void OpenCatalog() => EditorUtility.OpenWithDefaultApp(Path.Combine(PixelFxRoot, "docs", "CATALOG.md"));

        [MenuItem("Tools/pixelfx/Open Gallery (web)", priority = 21)]
        public static void OpenGallery() => Application.OpenURL("https://patch-gg.github.io/pixelfx/");

        [MenuItem("Tools/pixelfx/Third-Party Notices", priority = 22)]
        public static void OpenNotices() => EditorUtility.OpenWithDefaultApp(Path.Combine(ProjectRoot, "THIRD_PARTY_NOTICES.md"));

        void OnGUI()
        {
            EditorGUILayout.LabelField("pixelfx " + PinnedVersion(), EditorStyles.boldLabel);
            if (!File.Exists(Path.Combine(PixelFxRoot, "tools", "render.mjs")))
                EditorGUILayout.HelpBox("third_party/pixelfx is missing. Run tools/pixelfx/vendor.sh.", MessageType.Error);

            NodePath = EditorGUILayout.TextField("Node (18+)", NodePath);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Find", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                query = EditorGUILayout.TextField(query);
                if (GUILayout.Button("Search", GUILayout.Width(70))) Search();
            }
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(120));
            foreach (var r in results)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(r[0], EditorStyles.miniButton, GUILayout.Width(220))) effectId = r[0];
                    EditorGUILayout.LabelField(r[1], EditorStyles.wordWrappedMiniLabel);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Render", EditorStyles.boldLabel);
            effectId = EditorGUILayout.TextField(new GUIContent("Effect id", "category/name, or a whole category"), effectId);
            outName = EditorGUILayout.TextField(new GUIContent("Save as (optional)", "file name for a recolored variant, e.g. blue-torch"), outName);
            optionsJson = EditorGUILayout.TextField(new GUIContent("Options JSON", "e.g. {\"palette\":[\"#102\",\"#43a\",\"#8cf\",\"#fff\"],\"seed\":3}"), optionsJson);
            bakeAct = EditorGUILayout.Toggle(new GUIContent("Bake act", "include the stock character and target (tools/act.mjs --sheet)"), bakeAct);
            PixelFxSettings.DefaultPixelsPerUnit = EditorGUILayout.FloatField("Pixels per unit (new)", PixelFxSettings.DefaultPixelsPerUnit);

            if (GUILayout.Button("Render & Import", GUILayout.Height(28))) RenderAndImport();
            if (!string.IsNullOrEmpty(lastLog)) EditorGUILayout.HelpBox(lastLog, MessageType.None);
        }

        static string PinnedVersion()
        {
            var f = Path.Combine(ProjectRoot, "tools", "pixelfx", "PIXELFX_VERSION");
            if (!File.Exists(f)) return "";
            foreach (var line in File.ReadAllLines(f)) if (line.StartsWith("version=")) return line.Substring(8);
            return "";
        }

        void Search()
        {
            results.Clear();
            var args = new List<string> { Path.Combine(PixelFxRoot, "tools", "list.mjs") };
            args.AddRange(query.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));
            if (Run(PixelFxRoot, args, out var output) != 0) { lastLog = output; return; }
            foreach (var line in output.Split('\n'))
            {
                var t = line.Trim();
                if (t.Length == 0 || !t.Contains("/") || t.EndsWith(" effects")) continue;
                int sp = t.IndexOf(' ');
                if (sp < 0) continue;
                results.Add(new[] { t.Substring(0, sp), t.Substring(sp).Trim() });
            }
            lastLog = results.Count + " effects";
        }

        void RenderAndImport()
        {
            var args = new List<string> { Path.Combine(ProjectRoot, "tools", "pixelfx", "export.mjs"), effectId.Trim() };
            if (!string.IsNullOrWhiteSpace(outName)) { args.Add("--name"); args.Add(outName.Trim()); }
            if (!string.IsNullOrWhiteSpace(optionsJson)) { args.Add("--opts"); args.Add(optionsJson.Trim()); }
            if (bakeAct) args.Add("--act");
            EditorUtility.DisplayProgressBar("pixelfx", "Rendering " + effectId, 0.5f);
            try
            {
                int code = Run(ProjectRoot, args, out var output);
                lastLog = output.Trim();
                if (code != 0) { Debug.LogError("pixelfx export failed:\n" + output); return; }
                Debug.Log("pixelfx:\n" + output);
            }
            finally { EditorUtility.ClearProgressBar(); }
            AssetDatabase.Refresh(); // the postprocessor builds the sheet asset + prefab
        }

        static int Run(string cwd, List<string> args, out string output)
        {
            var psi = new ProcessStartInfo
            {
                FileName = NodePath,
                WorkingDirectory = cwd,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            var sb = new StringBuilder();
            foreach (var a in args) sb.Append(Quote(a)).Append(' ');
            psi.Arguments = sb.ToString();
            try
            {
                using (var p = Process.Start(psi))
                {
                    var err = p.StandardError.ReadToEndAsync();
                    var outp = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    output = outp + err.Result;
                    return p.ExitCode;
                }
            }
            catch (System.Exception e)
            {
                output = $"Could not run Node ({NodePath}): {e.Message}\nInstall Node 18+ or set its path above.";
                return -1;
            }
        }

        static string Quote(string a)
        {
            // CommandLineToArgvW rules (also what Mono uses): backslashes are literal unless they precede a quote.
            if (a.Length > 0 && a.IndexOfAny(new[] { ' ', '"', '\t' }) < 0) return a;
            var sb = new StringBuilder("\"");
            int slashes = 0;
            foreach (char c in a)
            {
                if (c == '\\') { slashes++; continue; }
                if (c == '"') { sb.Append('\\', slashes * 2 + 1).Append('"'); slashes = 0; continue; }
                sb.Append('\\', slashes).Append(c);
                slashes = 0;
            }
            sb.Append('\\', slashes * 2).Append('"');
            return sb.ToString();
        }
    }
}
