using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelFx.Editor
{
    /// <summary>
    /// Imports every pixelfx sheet (PNG + JSON pair) under <see cref="SheetsRoot"/>:
    /// the PNG gets pixel-art settings (point filter, no compression, no mipmaps, full size), and next to it a
    /// <see cref="PixelFxSheet"/> asset and a prefab (SpriteRenderer + PixelFxPlayer) are created or refreshed.
    /// Drop files in with tools/pixelfx/export.mjs, the Tools/pixelfx menu, or by hand.
    /// </summary>
    public class PixelFxSheetPostprocessor : AssetPostprocessor
    {
        public const string SheetsRoot = "Assets/PixelFx/Sheets";

        static bool InSheets(string path) =>
            path.Replace('\\', '/').StartsWith(SheetsRoot + "/");

        static bool IsSheetPng(string path) =>
            InSheets(path) && path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) &&
            File.Exists(Path.ChangeExtension(path, ".json"));

        void OnPreprocessTexture()
        {
            if (!IsSheetPng(assetPath)) return;
            var ti = (TextureImporter)assetImporter;
            ApplyPixelSettings(ti);
        }

        public static void ApplyPixelSettings(TextureImporter ti)
        {
            ti.textureType = TextureImporterType.Default;
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.filterMode = FilterMode.Point;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.isReadable = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 16384; // pixelfx sheets are often 3000-8000 px wide; never let Unity shrink them
            var def = ti.GetDefaultPlatformTextureSettings();
            def.format = TextureImporterFormat.RGBA32;
            def.textureCompression = TextureImporterCompression.Uncompressed;
            def.maxTextureSize = 16384;
            ti.SetPlatformTextureSettings(def);
        }

        static readonly HashSet<string> Pending = new HashSet<string>();

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var p in imported)
            {
                if (!InSheets(p)) continue;
                string json = null;
                if (p.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase)) json = p;
                else if (IsSheetPng(p)) json = Path.ChangeExtension(p, ".json").Replace('\\', '/');
                if (json != null && File.Exists(Path.ChangeExtension(json, ".png"))) Pending.Add(json);
            }
            if (Pending.Count == 0) return;
            // Creating assets during the import callback is unsafe; do it right after.
            EditorApplication.delayCall -= Flush;
            EditorApplication.delayCall += Flush;
        }

        static void Flush()
        {
            var list = new List<string>(Pending);
            Pending.Clear();
            foreach (var json in list)
            {
                try { BuildAssets(json); }
                catch (System.Exception e) { Debug.LogError($"pixelfx: could not import {json}: {e.Message}"); }
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Create or refresh the PixelFxSheet asset and prefab for one sheet. Returns the sheet.</summary>
        public static PixelFxSheet BuildAssets(string jsonPath)
        {
            jsonPath = jsonPath.Replace('\\', '/');
            var pngPath = Path.ChangeExtension(jsonPath, ".png").Replace('\\', '/');
            var data = PixelFxSheetData.Parse(File.ReadAllText(jsonPath));
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            if (tex == null)
            {
                AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            }
            if (tex == null) throw new IOException("texture not imported: " + pngPath);
            if (tex.width != data.textureWidth || tex.height != data.textureHeight)
                Debug.LogWarning($"pixelfx: {pngPath} is {tex.width}x{tex.height} but its JSON says {data.textureWidth}x{data.textureHeight}. Check the texture's Max Size.");

            var dir = Path.GetDirectoryName(jsonPath).Replace('\\', '/');
            var baseName = Path.GetFileNameWithoutExtension(jsonPath);
            var assetPath = $"{dir}/{baseName}.asset";
            var sheet = AssetDatabase.LoadAssetAtPath<PixelFxSheet>(assetPath);
            if (sheet == null)
            {
                sheet = ScriptableObject.CreateInstance<PixelFxSheet>();
                sheet.pixelsPerUnit = PixelFxSettings.DefaultPixelsPerUnit;
                sheet.texture = tex;
                sheet.data = data;
                AssetDatabase.CreateAsset(sheet, assetPath);
            }
            else
            {
                sheet.texture = tex; // pixelsPerUnit is kept: it is yours to tune
                sheet.data = data;
                sheet.Invalidate();
                EditorUtility.SetDirty(sheet);
            }

            var prefabPath = $"{dir}/{baseName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                var go = new GameObject(baseName);
                try
                {
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 100;
                    var p = go.AddComponent<PixelFxPlayer>();
                    p.sheet = sheet;
                    PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                }
                finally { Object.DestroyImmediate(go); }
            }
            Debug.Log($"pixelfx: imported {data.id} ({data.frames.Count} frames @ {data.fps} fps, {(data.loop ? "loop" : "one-shot")}) -> {assetPath}");
            return sheet;
        }
    }

    /// <summary>Project-wide import defaults, kept in EditorPrefs per project.</summary>
    public static class PixelFxSettings
    {
        static string Key => "PixelFx.PPU." + Application.dataPath.GetHashCode();

        public static float DefaultPixelsPerUnit
        {
            get => EditorPrefs.GetFloat(Key, 16f);
            set => EditorPrefs.SetFloat(Key, value);
        }
    }
}
