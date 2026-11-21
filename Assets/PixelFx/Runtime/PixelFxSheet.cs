using UnityEngine;

namespace PixelFx
{
    /// <summary>
    /// An imported pixelfx sprite sheet: the texture plus its frame rects, timing, pivot and act data.
    /// Created and kept up to date by the editor importer for every PNG + JSON pair in Assets/PixelFx/Sheets.
    /// </summary>
    [CreateAssetMenu(menuName = "pixelfx/Sheet", fileName = "PixelFxSheet")]
    public class PixelFxSheet : ScriptableObject
    {
        public Texture2D texture;
        [Tooltip("Texture pixels per world unit. Match your game's sprites so effects come out at the same pixel scale.")]
        public float pixelsPerUnit = 16;
        public PixelFxSheetData data = new PixelFxSheetData();

        [System.NonSerialized] Sprite[] sprites;

        public int FrameCount => data?.frames?.Count ?? 0;

        /// <summary>The frames as sprites, built once from the texture (pivot = the effect's origin).</summary>
        public Sprite[] Sprites
        {
            get
            {
                if (sprites != null && sprites.Length == FrameCount && (sprites.Length == 0 || sprites[0] != null)) return sprites;
                sprites = new Sprite[FrameCount];
                if (texture == null) return sprites;
                data.PivotNormalized(out var px, out var py);
                for (int i = 0; i < sprites.Length; i++)
                {
                    data.UnityRect(i, out var x, out var y, out var w, out var h);
                    var s = Sprite.Create(texture, new Rect(x, y, w, h), new Vector2(px, py), pixelsPerUnit, 0, SpriteMeshType.FullRect);
                    s.name = $"{name} {i}";
                    s.hideFlags = HideFlags.DontSave;
                    sprites[i] = s;
                }
                return sprites;
            }
        }

        /// <summary>Drop the cached sprites (after the texture or data changes).</summary>
        public void Invalidate()
        {
            if (sprites != null)
                foreach (var s in sprites)
                    if (s != null)
                    {
                        if (Application.isPlaying) Destroy(s);
                        else DestroyImmediate(s);
                    }
            sprites = null;
        }

        void OnValidate() => Invalidate();
        void OnDisable() => Invalidate();
    }
}
