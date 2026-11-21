using System;

namespace SolarDark.Combat
{
    /// <summary>
    /// Overlap tests for skill hit areas against hurtboxes (circles). Plain floats, no UnityEngine, so it is
    /// unit-testable outside the editor. World units, y up.
    /// </summary>
    public static class SkillGeometry
    {
        public static bool CircleCircle(float ax, float ay, float ar, float bx, float by, float br)
        {
            float dx = bx - ax, dy = by - ay, r = ar + br;
            return dx * dx + dy * dy <= r * r;
        }

        /// <summary>
        /// A box of size w×h centred at (cx, cy), rotated by <paramref name="angle"/> radians (counter-clockwise),
        /// against a circle at (px, py) radius pr.
        /// </summary>
        public static bool BoxCircle(float cx, float cy, float w, float h, float angle, float px, float py, float pr)
        {
            float c = (float)Math.Cos(-angle), s = (float)Math.Sin(-angle);
            float dx = px - cx, dy = py - cy;
            float lx = dx * c - dy * s, ly = dx * s + dy * c;   // circle centre in box space
            float qx = Math.Max(-w / 2, Math.Min(w / 2, lx)), qy = Math.Max(-h / 2, Math.Min(h / 2, ly));
            float ex = lx - qx, ey = ly - qy;
            return ex * ex + ey * ey <= pr * pr;
        }

        /// <summary>Rotate a local offset by an angle and mirror it for a facing of -1 (left).</summary>
        public static void Place(float ox, float oy, int facing, float angle, out float x, out float y)
        {
            ox *= facing < 0 ? -1 : 1;
            float c = (float)Math.Cos(angle), s = (float)Math.Sin(angle);
            x = ox * c - oy * s;
            y = ox * s + oy * c;
        }

        /// <summary>
        /// Where the i-th of n instances goes along the floor, spread over <paramref name="spread"/> units round a
        /// centre: evenly from -spread/2 to +spread/2 (a single one sits on the centre).
        /// </summary>
        public static float LineOffset(int i, int n, float spread) =>
            n <= 1 ? 0 : -spread / 2 + spread * i / (n - 1);

        /// <summary>The i-th of n points on a circle of radius r, starting straight up and going clockwise.</summary>
        public static void RingOffset(int i, int n, float r, out float x, out float y)
        {
            double a = Math.PI / 2 - (n <= 0 ? 0 : 2 * Math.PI * i / n);
            x = (float)(Math.Cos(a) * r);
            y = (float)(Math.Sin(a) * r);
        }
    }
}
