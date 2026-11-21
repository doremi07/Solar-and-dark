using UnityEngine;

namespace PixelFx
{
    /// <summary>
    /// The pixelfx check scene: spawns <see cref="sheet"/> on a ground strip every few seconds, mirrored every other
    /// time, and wherever you click. Tools > pixelfx > Create Demo Scene builds a scene with this; press Play.
    /// </summary>
    public class PixelFxDemo : MonoBehaviour
    {
        public PixelFxSheet sheet;
        [Min(0.2f)] public float every = 2.5f;
        public Vector3 groundPoint = new Vector3(0, -2, 0);
        public bool drawGround = true;
        public Color groundColor = new Color(0.2f, 0.18f, 0.24f);

        float next;
        public int Spawned { get; private set; }

        void Start()
        {
            if (!drawGround) return;
            var tex = Texture2D.whiteTexture;
            var g = new GameObject("Ground").AddComponent<SpriteRenderer>();
            g.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 1f), tex.width);
            g.color = groundColor;
            g.sortingOrder = -10;
            g.transform.position = groundPoint;
            g.transform.localScale = new Vector3(200, 50, 1);
        }

        void Update()
        {
            if (sheet == null) return;
            if (Time.time >= next)
            {
                next = Time.time + every;
                Spawn(groundPoint, Spawned % 2 == 1);
            }
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0) && Camera.main != null)
            {
                var p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                p.z = 0;
                Spawn(p, false);
            }
#endif
        }

        void Spawn(Vector3 at, bool flip)
        {
            var fx = PixelFxPlayer.Spawn(sheet, at, flip);
            fx.onHit.AddListener(h => Debug.Log($"pixelfx hit: freeze {h.freeze}s"));
            Spawned++;
        }
    }
}
