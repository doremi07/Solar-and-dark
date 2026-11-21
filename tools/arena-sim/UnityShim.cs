// A small stand-in for the parts of UnityEngine the game's runtime code uses, so the real scripts (boss AI,
// skill runner, hit volumes, pixelfx player, arena builder) can run headless in a cloud session and be rendered.
// It follows Unity's semantics where the game relies on them: fake-null destroyed objects, Awake/OnEnable/Start
// order, inactive GameObjects deferring Awake, nested coroutines and WaitForSeconds on scaled time, timeScale 0
// freezing deltaTime, transform parenting. It is NOT Unity: no physics, no Animator, no OnGUI, no real Input.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine
{
    public enum HideFlags { None = 0, HideInHierarchy = 1, DontSave = 52, HideAndDontSave = 61 }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum TextureFormat { RGBA32 = 4 }
    public enum SpriteMeshType { FullRect, Tight }
    public enum CameraClearFlags { Skybox = 1, SolidColor = 2 }
    public enum RuntimePlatform { LinuxEditor = 16, WindowsEditor = 7, OSXEditor = 0 }
    public enum FontStyle { Normal, Bold }

    // ---------------------------------------------------------------- attributes
    [AttributeUsage(AttributeTargets.All)] public class TooltipAttribute : Attribute { public TooltipAttribute(string t) { } }
    [AttributeUsage(AttributeTargets.All)] public class HeaderAttribute : Attribute { public HeaderAttribute(string t) { } }
    [AttributeUsage(AttributeTargets.All)] public class MinAttribute : Attribute { public MinAttribute(float m) { } }
    [AttributeUsage(AttributeTargets.All)] public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    [AttributeUsage(AttributeTargets.All)] public class TextAreaAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.All)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequireComponent : Attribute { public Type[] types; public RequireComponent(params Type[] t) { types = t; } }
    [AttributeUsage(AttributeTargets.Class)]
    public class DefaultExecutionOrder : Attribute { public int order; public DefaultExecutionOrder(int o) { order = o; } }
    [AttributeUsage(AttributeTargets.Class)]
    public class CreateAssetMenuAttribute : Attribute { public string menuName { get; set; } public string fileName { get; set; } }

    // ---------------------------------------------------------------- math
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public static Vector2 right => new Vector2(1, 0);
        public static Vector2 up => new Vector2(0, 1);
        public float sqrMagnitude => x * x + y * y;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public Vector2 normalized { get { float m = magnitude; return m > 1e-5f ? new Vector2(x / m, y / m) : zero; } }
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float k) => new Vector2(a.x * k, a.y * k);
        public static Vector2 operator *(float k, Vector2 a) => new Vector2(a.x * k, a.y * k);
        public static Vector2 operator /(Vector2 a, float k) => new Vector2(a.x / k, a.y / k);
        public static bool operator ==(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 1e-10f;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public override bool Equals(object o) => o is Vector2 v && v == this;
        public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode() * 31;
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
        public override string ToString() => $"({x:F2}, {y:F2})";
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z = 0) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 right => new Vector3(1, 0, 0);
        public float sqrMagnitude => x * x + y * y + z * z;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float k) => new Vector3(a.x * k, a.y * k, a.z * k);
        public static Vector3 operator *(float k, Vector3 a) => a * k;
        public static bool operator ==(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 1e-10f;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);
        public override bool Equals(object o) => o is Vector3 v && v == this;
        public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode() * 31 ^ z.GetHashCode() * 17;
        public override string ToString() => $"({x:F2}, {y:F2}, {z:F2})";
    }

    public struct Quaternion
    {
        public float zDeg;
        public static Quaternion identity => new Quaternion();
        public static Quaternion Euler(float x, float y, float z) => new Quaternion { zDeg = z };
        public Vector3 eulerAngles => new Vector3(0, 0, Mathf.Repeat(zDeg, 360f));
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1, 1);
        public static Color black => new Color(0, 0, 0, 1);
        public static Color cyan => new Color(0, 1, 1, 1);
        public static Color magenta => new Color(1, 0, 1, 1);
        public static Color Lerp(Color a, Color b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
        }
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public struct Matrix4x4
    {
        public static Matrix4x4 TRS(Vector3 p, Quaternion q, Vector3 s) => new Matrix4x4();
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI, Deg2Rad = PI / 180f, Rad2Deg = 180f / PI;
        public static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Max(params float[] v) => v.Max();
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Abs(float v) => Math.Abs(v);
        public static float Sign(float v) => v >= 0 ? 1 : -1;
        public static float Sin(float v) => (float)Math.Sin(v);
        public static float Cos(float v) => (float)Math.Cos(v);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Sqrt(float v) => (float)Math.Sqrt(v);
        public static float Exp(float v) => (float)Math.Exp(v);
        public static float Pow(float a, float b) => (float)Math.Pow(a, b);
        public static int RoundToInt(float v) => (int)Math.Round(v, MidpointRounding.ToEven);
        public static float Repeat(float t, float len) => Clamp(t - (float)Math.Floor(t / len) * len, 0, len);
        public static float PingPong(float t, float len) { t = Repeat(t, len * 2); return len - Math.Abs(t - len); }
        public static float SmoothStep(float a, float b, float t) { t = Clamp01(t); t = -2 * t * t * t + 3 * t * t; return b * t + a * (1 - t); }
    }

    public static class Random
    {
        static readonly System.Random r = new System.Random(7);
        public static float Range(float a, float b) => a + (float)r.NextDouble() * (b - a);
        public static int Range(int a, int b) => r.Next(a, b);
    }

    public static class Time
    {
        public static float deltaTime, unscaledDeltaTime, time, unscaledTime, timeScale = 1;
        public static int frameCount;
    }

    public static class Application
    {
        public static bool isPlaying = true;
        public static string dataPath = "/sim/Assets";
        public static RuntimePlatform platform => RuntimePlatform.LinuxEditor;
        public static void OpenURL(string u) { }
    }

    public static class Debug
    {
        public static List<string> Log_ = new List<string>();
        public static void Log(object m) { Log_.Add("[log] " + m); }
        public static void LogWarning(object m) { Log_.Add("[warn] " + m); Console.Error.WriteLine("[warn] " + m); }
        public static void LogError(object m) { Log_.Add("[error] " + m); Console.Error.WriteLine("[error] " + m); }
    }

    public static class Screen { public static int width = 1110, height = 624; }

    // ---------------------------------------------------------------- objects

    public class Object
    {
        internal bool destroyed;
        public string name = "";
        public HideFlags hideFlags;

        public static bool operator ==(Object a, Object b)
        {
            bool an = a is null || a.destroyed, bn = b is null || b.destroyed;
            if (an || bn) return an && bn;
            return ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public static implicit operator bool(Object o) => !(o is null) && !o.destroyed;
        public override bool Equals(object o) => o is Object ob ? this == ob : (o == null && destroyed);
        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

        public static void Destroy(Object o) => Destroy(o, 0);
        public static void Destroy(Object o, float t)
        {
            if (o is null || o.destroyed) return;
            Engine.ScheduleDestroy(o, t);
        }
        public static void DestroyImmediate(Object o) { if (!(o is null)) Engine.DestroyNow(o); }
        public static void DontDestroyOnLoad(Object o) { }
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new()
        {
            var s = new T();
            Engine.Call(s, "OnEnable");
            return s;
        }
    }

    public class Texture : Object { public virtual int width { get; protected set; } public virtual int height { get; protected set; } }

    public class Texture2D : Texture
    {
        public Color32[] pixels;   // row 0 = bottom, as in Unity
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public Texture2D(int w, int h, TextureFormat f = TextureFormat.RGBA32, bool mip = false) { width = w; height = h; pixels = new Color32[w * h]; }
        public void SetPixels32(Color32[] p) => pixels = (Color32[])p.Clone();
        public void Apply(bool mips = false, bool makeNoLongerReadable = false) { }
        static Texture2D white;
        public static Texture2D whiteTexture
        {
            get
            {
                if (white == null)
                {
                    white = new Texture2D(4, 4) { name = "white" };
                    for (int i = 0; i < 16; i++) white.pixels[i] = new Color32(255, 255, 255, 255);
                }
                return white;
            }
        }
    }

    public class Sprite : Object
    {
        public Texture2D texture;
        public Rect rect;
        public Vector2 pivot;          // pixels from the rect's bottom-left
        public float pixelsPerUnit;
        public static Sprite Create(Texture2D t, Rect r, Vector2 pivotNormalized, float ppu, uint extrude = 0, SpriteMeshType m = SpriteMeshType.FullRect) =>
            new Sprite { texture = t, rect = r, pivot = new Vector2(pivotNormalized.x * r.width, pivotNormalized.y * r.height), pixelsPerUnit = ppu };
    }

    public sealed class GameObject : Object
    {
        internal readonly List<Component> components = new List<Component>();
        internal bool activeSelf = true;
        public string tag = "Untagged";
        public Transform transform { get; }
        public int layer;

        public GameObject(string name = "GameObject")
        {
            this.name = name;
            transform = new Transform();
            transform.go = this;
            components.Add(transform);
            Engine.Register(this);
        }

        public bool activeInHierarchy => !destroyed && activeSelf && (transform.parent == null || transform.parent.gameObject.activeInHierarchy);

        public void SetActive(bool on)
        {
            if (activeSelf == on) return;
            bool was = activeInHierarchy;
            activeSelf = on;
            if (!was && activeInHierarchy) Engine.Activate(this);
            else if (was && !activeInHierarchy) Engine.Deactivate(this);
        }

        public T AddComponent<T>() where T : Component, new() => (T)AddComponent(typeof(T));

        public Component AddComponent(Type t)
        {
            foreach (RequireComponent rc in t.GetCustomAttributes(typeof(RequireComponent), true))
                foreach (var req in rc.types)
                    if (GetComponent(req) == null) AddComponent(req);
            var c = (Component)Activator.CreateInstance(t);
            c.go = this;
            components.Add(c);
            Engine.ComponentAdded(c);
            return c;
        }

        public T GetComponent<T>() => (T)(object)GetComponent(typeof(T));
        public Component GetComponent(Type t) => components.FirstOrDefault(c => !c.destroyed && t.IsInstanceOfType(c));
        public static GameObject FindWithTag(string tag) => Engine.Objects.FirstOrDefault(g => !g.destroyed && g.tag == tag && g.activeInHierarchy);
    }

    public class Component : Object
    {
        internal GameObject go;
        public GameObject gameObject => go;
        public Transform transform => go.transform;
        public string tag { get => go.tag; set => go.tag = value; }
        public T GetComponent<T>() => go.GetComponent<T>();
        public Component GetComponent(Type t) => go.GetComponent(t);
        public bool TryGetComponent<T>(out T c) { c = go.GetComponent<T>(); return c != null && !(c is Object o && o.destroyed); }
        public T GetComponentInChildren<T>()
        {
            var own = go.GetComponent<T>();
            if (own != null) return own;
            foreach (var ch in transform.children)
            {
                var c = ch.GetComponentInChildren<T>();
                if (c != null) return c;
            }
            return default;
        }
        public new string name { get => go.name; set => go.name = value; }
    }

    public class Behaviour : Component
    {
        bool _enabled = true;
        public bool enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                if (go.activeInHierarchy && Engine.Playing) Engine.Call(this, value ? "OnEnable" : "OnDisable");
            }
        }
        public bool isActiveAndEnabled => enabled && go.activeInHierarchy;
    }

    public class Transform : Component
    {
        internal Transform parentT;
        internal readonly List<Transform> children = new List<Transform>();
        public Vector3 localPosition;
        public float localZ;            // rotation, degrees
        public Vector3 localScale = Vector3.one;

        public Transform parent => parentT;
        public int childCount => children.Count;

        public void SetParent(Transform p) => SetParent(p, true);
        public void SetParent(Transform p, bool worldPositionStays)
        {
            var wp = position; var wr = rotation.zDeg; var ws = lossyScale;
            parentT?.children.Remove(this);
            parentT = p;
            p?.children.Add(this);
            if (worldPositionStays)
            {
                position = wp;
                rotation = Quaternion.Euler(0, 0, wr);
                var ps = p != null ? p.lossyScale : Vector3.one;
                localScale = new Vector3(ws.x / ps.x, ws.y / ps.y, 1);
            }
        }

        // world = parent.world * (T * R * S)
        internal void ToWorld(float lx, float ly, out float wx, out float wy)
        {
            float sx = lx * localScale.x, sy = ly * localScale.y;
            float a = localZ * Mathf.Deg2Rad, c = (float)Math.Cos(a), s = (float)Math.Sin(a);
            float rx = sx * c - sy * s + localPosition.x, ry = sx * s + sy * c + localPosition.y;
            if (parentT == null) { wx = rx; wy = ry; return; }
            parentT.ToWorld(rx, ry, out wx, out wy);
        }

        internal void ToLocal(float wx, float wy, out float lx, out float ly)
        {
            float px = wx, py = wy;
            if (parentT != null) parentT.ToLocal(wx, wy, out px, out py);
            px -= localPosition.x; py -= localPosition.y;
            float a = -localZ * Mathf.Deg2Rad, c = (float)Math.Cos(a), s = (float)Math.Sin(a);
            float rx = px * c - py * s, ry = px * s + py * c;
            lx = localScale.x != 0 ? rx / localScale.x : 0;
            ly = localScale.y != 0 ? ry / localScale.y : 0;
        }

        public Vector3 position
        {
            get { ToWorld(0, 0, out var x, out var y); return new Vector3(x, y, localPosition.z); }
            set
            {
                if (parentT == null) { localPosition = value; return; }
                parentT.ToLocal(value.x, value.y, out var lx, out var ly);
                localPosition = new Vector3(lx, ly, value.z);
            }
        }

        public Quaternion rotation
        {
            get { float z = localZ; for (var p = parentT; p != null; p = p.parentT) z += p.localZ; return Quaternion.Euler(0, 0, z); }
            set { float pz = 0; for (var p = parentT; p != null; p = p.parentT) pz += p.localZ; localZ = value.zDeg - pz; }
        }
        public Quaternion localRotation { get => Quaternion.Euler(0, 0, localZ); set => localZ = value.zDeg; }
        public Vector3 eulerAngles => rotation.eulerAngles;
        public Vector3 lossyScale
        {
            get
            {
                float x = localScale.x, y = localScale.y;
                for (var p = parentT; p != null; p = p.parentT) { x *= p.localScale.x; y *= p.localScale.y; }
                return new Vector3(x, y, 1);
            }
        }
        public void SetPositionAndRotation(Vector3 p, Quaternion r) { position = p; rotation = r; }
    }

    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator routine) => Engine.StartCoroutine(this, routine);
        public void StopCoroutine(Coroutine c) { if (c != null) c.stopped = true; }
        public void StopAllCoroutines() => Engine.StopAll(this);
    }

    public class Coroutine
    {
        internal MonoBehaviour owner;
        internal Stack<IEnumerator> stack = new Stack<IEnumerator>();
        internal float waitUntil = -1;
        internal bool stopped, done;
    }

    public class WaitForSeconds { internal float seconds; public WaitForSeconds(float s) { seconds = s; } }

    public class SpriteRenderer : Behaviour
    {
        public Sprite sprite;
        public Color color = Color.white;
        public bool flipX, flipY;
        public int sortingOrder;
        internal long serial = Engine.NextSerial();
    }

    public class Camera : Behaviour
    {
        public bool orthographic;
        public float orthographicSize = 5;
        public CameraClearFlags clearFlags;
        public Color backgroundColor;
        public static Camera main
        {
            get
            {
                var g = GameObject.FindWithTag("MainCamera");
                return g?.GetComponent<Camera>();
            }
        }
        public Vector3 ScreenToWorldPoint(Vector3 p) => p;
    }

    public class Animator : Behaviour { public void SetTrigger(string s) { } }
    public class Rigidbody2D : Component { public void MovePosition(Vector2 p) => transform.position = new Vector3(p.x, p.y, transform.position.z); }

    public static class Gizmos
    {
        public static Color color;
        public static Matrix4x4 matrix;
        public static void DrawWireSphere(Vector3 c, float r) { }
        public static void DrawWireCube(Vector3 c, Vector3 s) { }
    }

    // OnGUI is never called here; these only let ArenaHud compile.
    public class GUIStyleState { public Color textColor; }
    public class GUIStyle { public int fontSize; public FontStyle fontStyle; public GUIStyleState normal = new GUIStyleState(); public GUIStyle() { } public GUIStyle(GUIStyle o) { } }
    public class GUISkin { public GUIStyle label = new GUIStyle(); }
    public static class GUI
    {
        public static GUISkin skin = new GUISkin();
        public static Color color = Color.white;
        public static void Label(Rect r, string s, GUIStyle st) { }
        public static void DrawTexture(Rect r, Texture t) { }
    }

    // ---------------------------------------------------------------- the engine loop

    public static class Engine
    {
        public static bool Playing;
        internal static readonly List<GameObject> Objects = new List<GameObject>();
        static readonly List<Component> awoken = new List<Component>();
        static readonly HashSet<Component> started = new HashSet<Component>();
        static readonly List<Coroutine> coroutines = new List<Coroutine>();
        static readonly List<(Object o, float at)> pendingDestroy = new List<(Object, float)>();
        static long serial;
        public static readonly List<string> Errors = new List<string>();

        internal static long NextSerial() => ++serial;
        internal static void Register(GameObject g) => Objects.Add(g);

        internal static void Call(object target, string method)
        {
            for (var t = target.GetType(); t != null && t != typeof(object); t = t.BaseType)
            {
                var m = t.GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
                if (m == null) continue;
                try { m.Invoke(target, null); }
                catch (TargetInvocationException e) { Report(target, method, e.InnerException); }
                return;
            }
        }

        static void Report(object target, string where, Exception e)
        {
            var msg = $"{target.GetType().Name}.{where}: {e.GetType().Name}: {e.Message}\n{e.StackTrace}";
            Errors.Add(msg);
            Console.Error.WriteLine("[exception] " + msg);
        }

        static int Order(Component c) => c.GetType().GetCustomAttribute<DefaultExecutionOrder>()?.order ?? 0;

        internal static void ComponentAdded(Component c)
        {
            if (!Playing || !c.gameObject.activeInHierarchy) return;
            Wake(c);
        }

        static void Wake(Component c)
        {
            if (awoken.Contains(c)) return;
            awoken.Add(c);
            Call(c, "Awake");
            if (!(c is Behaviour b) || b.enabled) Call(c, "OnEnable");
        }

        /// <summary>Enter play mode: Awake + OnEnable everything active, by execution order then creation order.</summary>
        public static void EnterPlayMode()
        {
            Playing = true;
            var all = Objects.Where(g => g.activeInHierarchy).SelectMany(g => g.components).Where(c => !(c is Transform)).ToList();
            foreach (var c in all.OrderBy(Order)) Wake(c);
        }

        internal static void Activate(GameObject g)
        {
            if (!Playing) return;
            foreach (var c in g.components.ToList())
                if (!(c is Transform))
                {
                    if (!awoken.Contains(c)) Wake(c);
                    else if (!(c is Behaviour b) || b.enabled) Call(c, "OnEnable");
                }
            foreach (var ch in g.transform.children.ToList()) if (ch.gameObject.activeSelf) Activate(ch.gameObject);
        }

        internal static void Deactivate(GameObject g)
        {
            foreach (var c in g.components.ToList()) if (c is Behaviour && awoken.Contains(c)) Call(c, "OnDisable");
            foreach (var co in coroutines) if (co.owner.gameObject == g) co.stopped = true;
        }

        internal static Coroutine StartCoroutine(MonoBehaviour mb, IEnumerator r)
        {
            var co = new Coroutine { owner = mb };
            co.stack.Push(r);
            coroutines.Add(co);
            Step(co);   // like Unity: runs to the first yield right away
            return co;
        }

        internal static void StopAll(MonoBehaviour mb) { foreach (var c in coroutines) if (c.owner == mb) c.stopped = true; }

        static void Step(Coroutine co)
        {
            while (!co.stopped && co.stack.Count > 0)
            {
                if (co.waitUntil >= 0)
                {
                    if (Time.time < co.waitUntil) return;
                    co.waitUntil = -1;
                }
                var top = co.stack.Peek();
                bool more;
                try { more = top.MoveNext(); }
                catch (Exception e) { Report(co.owner, "coroutine", e); co.done = true; return; }
                if (!more) { co.stack.Pop(); continue; }
                var y = top.Current;
                if (y == null) return;
                if (y is IEnumerator nested) { co.stack.Push(nested); continue; }
                if (y is WaitForSeconds w) { co.waitUntil = Time.time + w.seconds; return; }
                if (y is Coroutine other) { co.stack.Push(WaitFor(other)); continue; }
                return;
            }
            co.done = true;
        }

        static IEnumerator WaitFor(Coroutine other) { while (!other.done && !other.stopped) yield return null; }

        /// <summary>One frame: Start, Update, coroutines, LateUpdate, destroys.</summary>
        public static void Tick(float dt)
        {
            Time.unscaledDeltaTime = dt;
            Time.unscaledTime += dt;
            Time.deltaTime = dt * Time.timeScale;
            Time.time += Time.deltaTime;
            Time.frameCount++;

            var live = Behaviours().ToList();
            foreach (var b in live) if (!started.Contains(b) && Alive(b)) { started.Add(b); Call(b, "Start"); }
            foreach (var b in live) if (Alive(b)) Call(b, "Update");
            foreach (var co in coroutines.ToList())
            {
                if (co.done || co.stopped) continue;
                if (co.owner.destroyed || !co.owner.gameObject.activeInHierarchy) { co.stopped = true; continue; }
                Step(co);
            }
            coroutines.RemoveAll(c => c.done || c.stopped);
            foreach (var b in live) if (Alive(b)) Call(b, "LateUpdate");

            float now = Time.time;
            foreach (var (o, at) in pendingDestroy.ToList())
                if (at <= now) { pendingDestroy.Remove((o, at)); DestroyNow(o); }
            Objects.RemoveAll(g => g.destroyed);
        }

        static bool Alive(Behaviour b) => !b.destroyed && b.enabled && b.gameObject.activeInHierarchy && awoken.Contains(b);

        static IEnumerable<Behaviour> Behaviours()
        {
            foreach (var g in Objects.ToList())
                if (g.activeInHierarchy)
                    foreach (var c in g.components.ToList())
                        if (c is MonoBehaviour b) yield return b;
        }

        internal static void ScheduleDestroy(Object o, float t)
        {
            // Unity destroys at the end of the frame (t = 0) or after t seconds of scaled time
            pendingDestroy.Add((o, Time.time + Math.Max(0, t)));
        }

        internal static void DestroyNow(Object o)
        {
            if (o.destroyed) return;
            if (o is GameObject g)
            {
                foreach (var ch in g.transform.children.ToList()) DestroyNow(ch.gameObject);
                if (g.activeInHierarchy) Deactivate(g);
                foreach (var c in g.components) { Call(c, "OnDestroy"); c.destroyed = true; }
                g.destroyed = true;
                g.transform.parentT?.children.Remove(g.transform);
            }
            else if (o is Component c)
            {
                if (c is Behaviour b && b.enabled && c.gameObject.activeInHierarchy) Call(c, "OnDisable");
                c.destroyed = true;
                c.gameObject.components.Remove(c);
            }
            else o.destroyed = true;
        }

        public static IEnumerable<SpriteRenderer> Renderers() =>
            Objects.Where(g => g.activeInHierarchy).SelectMany(g => g.components.OfType<SpriteRenderer>()).Where(r => r.enabled && !r.destroyed);

        public static IEnumerable<T> FindAll<T>() where T : Component =>
            Objects.Where(g => !g.destroyed).SelectMany(g => g.components.OfType<T>()).Where(c => !c.destroyed);
    }
}

namespace UnityEngine.Events
{
    public class UnityEvent
    {
        readonly List<Action> l = new List<Action>();
        public void AddListener(Action a) => l.Add(a);
        public void RemoveAllListeners() => l.Clear();
        public void Invoke() { foreach (var a in l.ToList()) a(); }
    }
    public class UnityEvent<T0>
    {
        readonly List<Action<T0>> l = new List<Action<T0>>();
        public void AddListener(Action<T0> a) => l.Add(a);
        public void RemoveAllListeners() => l.Clear();
        public void Invoke(T0 v) { foreach (var a in l.ToList()) a(v); }
    }
    public class UnityEvent<T0, T1>
    {
        readonly List<Action<T0, T1>> l = new List<Action<T0, T1>>();
        public void AddListener(Action<T0, T1> a) => l.Add(a);
        public void Invoke(T0 a, T1 b) { foreach (var x in l.ToList()) x(a, b); }
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene { public string path; }
}
