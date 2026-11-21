using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelFx.Editor
{
    /// <summary>Tools > pixelfx > Create Demo Scene: a scene that plays the selected (or first) pixelfx sheet on a loop.</summary>
    public static class PixelFxDemoScene
    {
        public const string ScenePath = "Assets/PixelFx/Demo/PixelFxDemo.unity";

        [MenuItem("Tools/pixelfx/Create Demo Scene", priority = 10)]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var sheet = Selection.activeObject as PixelFxSheet ?? FirstSheet();
            if (sheet == null)
            {
                EditorUtility.DisplayDialog("pixelfx", "No PixelFxSheet yet. Use Tools > pixelfx > Render & Import Effect… first.", "OK");
                return;
            }
            var scene = Build(sheet);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"pixelfx: demo scene for {sheet.name} saved to {ScenePath}. Press Play.");
        }

        public static UnityEngine.SceneManagement.Scene Build(PixelFxSheet sheet)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            // frame the whole effect: its height in units plus some room
            float units = Mathf.Max(sheet.data.frameHeight, sheet.data.frameWidth * 9f / 16f) / sheet.pixelsPerUnit;
            cam.orthographicSize = units * 0.6f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.07f, 0.12f); // dark: glow is baked as alpha and reads best on dark

            float groundY = -units * 0.25f;
            var demo = new GameObject("pixelfx demo").AddComponent<PixelFxDemo>();
            demo.sheet = sheet;
            demo.groundPoint = new Vector3(0, groundY, 0);
            demo.every = Mathf.Max(1f, sheet.data.loop ? 4f : sheet.data.TotalSeconds + 0.5f);
            return scene;
        }

        static PixelFxSheet FirstSheet()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:PixelFxSheet", new[] { PixelFxSheetPostprocessor.SheetsRoot }))
                return AssetDatabase.LoadAssetAtPath<PixelFxSheet>(AssetDatabase.GUIDToAssetPath(guid));
            return null;
        }
    }
}
