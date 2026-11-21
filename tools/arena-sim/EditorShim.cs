// Stand-ins for the editor APIs the boss builder and arena builder use. Sheet assets are read straight from the
// exported PNG + JSON in the repository, exactly as the importer would build them.
using System;
using System.Collections.Generic;
using System.IO;
using PixelFx;
using StbImageSharp;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItem : Attribute { public int priority { get; set; } public MenuItem(string path) { } public MenuItem(string path, bool validate, int priority) { } }

    public static class AssetDatabase
    {
        public static string RepoRoot = "";
        static readonly Dictionary<string, UnityEngine.Object> cache = new Dictionary<string, UnityEngine.Object>();

        public static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object
        {
            if (cache.TryGetValue(path, out var o)) return o as T;
            if (typeof(T) == typeof(PixelFxSheet) && path.EndsWith(".asset"))
            {
                var json = Path.Combine(RepoRoot, Path.ChangeExtension(path, ".json"));
                if (!File.Exists(json)) return null;
                var sheet = ScriptableObject.CreateInstance<PixelFxSheet>();
                sheet.name = Path.GetFileNameWithoutExtension(path);
                sheet.data = PixelFxSheetData.Parse(File.ReadAllText(json));
                sheet.texture = LoadPng(Path.ChangeExtension(json, ".png"));
                sheet.pixelsPerUnit = 16;
                cache[path] = sheet;
                return sheet as T;
            }
            return null;   // boss/skill assets are always built fresh
        }

        public static Texture2D LoadPng(string file)
        {
            using var fs = File.OpenRead(file);
            var img = ImageResult.FromStream(fs, ColorComponents.RedGreenBlueAlpha);
            var t = new Texture2D(img.Width, img.Height) { name = Path.GetFileName(file) };
            for (int y = 0; y < img.Height; y++)
                for (int x = 0; x < img.Width; x++)
                {
                    int s = ((img.Height - 1 - y) * img.Width + x) * 4;   // PNG rows go down, Unity's go up
                    t.pixels[y * img.Width + x] = new Color32(img.Data[s], img.Data[s + 1], img.Data[s + 2], img.Data[s + 3]);
                }
            return t;
        }

        public static void CreateAsset(UnityEngine.Object o, string path) { cache[path] = o; o.name = Path.GetFileNameWithoutExtension(path); }
        public static void SaveAssets() { }
    }

    public static class EditorUtility
    {
        public static bool DisplayDialog(string t, string m, string ok, string cancel = "") => true;
        public static void CopySerialized(UnityEngine.Object a, UnityEngine.Object b) => throw new NotSupportedException();
        public static void SetDirty(UnityEngine.Object o) { }
    }
}

namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup s, NewSceneMode m) => new Scene();
        public static bool SaveScene(Scene s, string path) => true;
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() => true;
    }
}

namespace PixelFx.Editor
{
    /// <summary>The real window imports sheets in the editor; here the sheets are read directly, so nothing to do.</summary>
    public static class PixelFxWindow
    {
        public static void ReimportAll() { }
    }
}
