using UnityEngine;

namespace SolarDark.Combat
{
    /// <summary>
    /// Root, knockback and pull for a character. Your movement code reads <see cref="IsRooted"/> /
    /// <see cref="CanAct"/>; knockback and pulls move the transform (or its Rigidbody2D) directly.
    /// </summary>
    [DisallowMultipleComponent]
    public class StatusReceiver : MonoBehaviour
    {
        [Tooltip("How fast knockback dies away (1/s).")]
        public float knockbackDrag = 10f;
        [Tooltip("The floor: knockback and pulls never push the character below this y.")]
        public float floorY = 0f;
        public bool horizontalOnly = true;

        float rootLeft;
        Vector2 velocity;
        Rigidbody2D body;

        public bool IsRooted => rootLeft > 0;
        public float RootLeft => rootLeft;
        public bool CanAct => !IsRooted;

        void Awake() => body = GetComponent<Rigidbody2D>();

        public void Apply(HitInfo hit)
        {
            if (hit.root > 0) Root(hit.root);
            if (hit.knockback != Vector2.zero && !IsRooted) velocity += hit.knockback;
        }

        public void Root(float seconds) => rootLeft = Mathf.Max(rootLeft, seconds);

        /// <summary>Drag the character towards a point (gravity wells, absorbs). Called every frame while it lasts.</summary>
        public void Pull(Vector2 center, float strength, float dt)
        {
            var d = center - (Vector2)transform.position;
            if (horizontalOnly) d.y = 0;
            if (d.sqrMagnitude < 0.0025f) return;
            Move(d.normalized * Mathf.Min(strength * dt, d.magnitude));
        }

        void Update()
        {
            if (rootLeft > 0) rootLeft -= Time.deltaTime;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                Move(velocity * Time.deltaTime);
                velocity *= Mathf.Exp(-knockbackDrag * Time.deltaTime);
            }
        }

        void Move(Vector2 delta)
        {
            if (horizontalOnly) delta.y = 0;
            var p = (Vector2)transform.position + delta;
            p.y = Mathf.Max(floorY, p.y);
            if (body != null) body.MovePosition(p);
            else transform.position = new Vector3(p.x, p.y, transform.position.z);
        }
    }
}
