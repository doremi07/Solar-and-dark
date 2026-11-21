using System.Collections;
using UnityEngine;

namespace SolarDark.Combat
{
    /// <summary>Hit-stop: freeze game time for a moment on heavy contact. Overlapping freezes keep the longest.</summary>
    public static class HitStop
    {
        static Runner runner;
        static float until;
        static float restoreScale = 1;

        public static bool Active => runner != null && runner.active;

        public static void Freeze(float seconds)
        {
            if (seconds <= 0 || !Application.isPlaying) return;
            if (runner == null)
            {
                var go = new GameObject("[HitStop]") { hideFlags = HideFlags.HideAndDontSave };
                Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<Runner>();
            }
            float end = Time.unscaledTime + seconds;
            if (!runner.active)
            {
                restoreScale = Time.timeScale > 0 ? Time.timeScale : 1;
                until = end;
                runner.StartCoroutine(runner.Hold());
            }
            else until = Mathf.Max(until, end);
        }

        class Runner : MonoBehaviour
        {
            public bool active;

            public IEnumerator Hold()
            {
                active = true;
                Time.timeScale = 0;
                while (Time.unscaledTime < until) yield return null;
                Time.timeScale = restoreScale;
                active = false;
            }
        }
    }

    /// <summary>Screen shake on the main camera. Adds an offset each frame and takes the last one back, so it
    /// works under any camera-follow script that sets the position earlier in the frame.</summary>
    public class ScreenShake : MonoBehaviour
    {
        float amp, left, total;
        Vector3 applied;

        public static void Shake(float amplitude, float seconds)
        {
            var cam = Camera.main;
            if (cam == null || amplitude <= 0 || seconds <= 0) return;
            var s = cam.GetComponent<ScreenShake>();
            if (s == null) s = cam.gameObject.AddComponent<ScreenShake>();
            if (amplitude >= s.amp * (s.left / Mathf.Max(s.total, 0.0001f)))
            {
                s.amp = amplitude;
                s.left = s.total = seconds;
            }
        }

        void LateUpdate()
        {
            transform.localPosition -= applied;
            applied = Vector3.zero;
            if (left <= 0) return;
            left -= Time.unscaledDeltaTime;
            float k = amp * Mathf.Clamp01(left / total);
            applied = new Vector3(Random.Range(-k, k), Random.Range(-k, k), 0);
            transform.localPosition += applied;
        }
    }
}
