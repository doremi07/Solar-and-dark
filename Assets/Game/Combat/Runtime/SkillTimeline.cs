using System;
using System.Collections.Generic;

namespace SolarDark.Combat
{
    /// <summary>
    /// The schedule of a skill, from the start of its wind-up: when each telegraph appears and each instance spawns.
    /// Plain C#, no UnityEngine, so it can be tested outside the editor.
    /// </summary>
    public static class SkillTimeline
    {
        public enum Kind { Telegraph, Spawn }

        public struct CueTiming
        {
            public float time, interval, hitDelay;
            public int count;
            public bool hasHit, telegraph;
        }

        public struct Event
        {
            public float t;
            public Kind kind;
            public int cue, instance;
            public override string ToString() => $"{t:F2} {kind} cue{cue}#{instance}";
        }

        public static List<Event> Plan(float windup, IList<CueTiming> cues)
        {
            var list = new List<Event>();
            for (int c = 0; c < cues.Count; c++)
            {
                var q = cues[c];
                for (int i = 0; i < Math.Max(1, q.count); i++)
                {
                    float spawn = windup + q.time + i * q.interval;
                    list.Add(new Event { t = spawn, kind = Kind.Spawn, cue = c, instance = i });
                    if (q.telegraph && q.hasHit)
                    {
                        // every mark shows as the pattern starts: the player sees the whole attack and has the full
                        // wind-up (and the time until each hit) to get out of it
                        list.Add(new Event { t = 0, kind = Kind.Telegraph, cue = c, instance = i });
                    }
                }
            }
            // telegraphs before spawns at the same instant, then by time
            list.Sort((a, b) => a.t != b.t ? a.t.CompareTo(b.t) : a.kind.CompareTo(b.kind));
            return list;
        }
    }
}
