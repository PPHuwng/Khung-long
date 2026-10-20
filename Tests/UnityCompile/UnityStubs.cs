// Compile-only surface of Unity APIs referenced by this repository.
// These signatures catch C# integration/type errors; they do not emulate Unity,
// execute rendering, validate serialization, or replace an Editor play test.
using System;
namespace UnityEngine
{
    public class Object
    {
        public string name;
        public HideFlags hideFlags;
        public static void Destroy(Object value) { }
        public static T FindFirstObjectByType<T>() where T : Object { return default(T); }
        public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation) { return null; }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform;
        public T GetComponent<T>() where T : Component { return default(T); }
        public T GetComponentInParent<T>() where T : Component { return default(T); }
    }
    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { }
    public class GameObject : Object
    {
        public Transform transform;
        public GameObject(string name, params Type[] components) { }
        public void SetActive(bool active) { }
        public T GetComponent<T>() where T : Component { return default(T); }
        public T GetComponentInParent<T>() where T : Component { return default(T); }
        public T GetComponentInChildren<T>() where T : Component { return default(T); }
        public T AddComponent<T>() where T : Component { return default(T); }
    }
    public class Transform : Component
    {
        public Vector3 position, localScale;
        public void Translate(Vector3 displacement) { }
        public void Translate(Vector3 displacement, Space space) { }
    }
    public class Camera : Behaviour
    {
        public static Camera main;
        public bool orthographic;
        public float orthographicSize, aspect;
        public CameraClearFlags clearFlags;
        public Color backgroundColor;
        public Vector3 ViewportToWorldPoint(Vector3 point) { return default(Vector3); }
    }
    public class AudioListener : Behaviour { }
    public class AudioSource : Behaviour
    {
        public bool playOnAwake;
        public float volume;
        public void PlayOneShot(AudioClip clip) { }
    }
    public class AudioClip : Object
    {
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) { return null; }
        public bool SetData(float[] data, int offsetSamples) { return true; }
    }
    public class Rigidbody2D : Component { public Vector2 linearVelocity, position; }
    public class Animator : Behaviour { public void SetFloat(string name, float value) { } public void SetBool(string name, bool value) { } }
    public class Collider2D : Behaviour { public Bounds bounds; }
    public struct Bounds { public Vector3 min; }
    public class Collision2D { public GameObject gameObject; }
    public struct LayerMask { }
    public static class Physics2D { public static Collider2D OverlapCircle(Vector2 point, float radius, LayerMask layerMask) { return null; } }
    public static class Gizmos { public static Color color; public static void DrawWireSphere(Vector3 center, float radius) { } }
    public static class Time { public static float timeScale, deltaTime, unscaledDeltaTime, unscaledTime; }
    public static class Application { public static bool isMobilePlatform; public static int targetFrameRate; }
    public static class Screen { public static int width = 1280, height = 720; public static ScreenOrientation orientation; }
    public static class PlayerPrefs
    {
        public static bool HasKey(string key) { return false; }
        public static string GetString(string key) { return ""; }
        public static void SetString(string key, string value) { }
        public static int GetInt(string key, int defaultValue = 0) { return defaultValue; }
        public static void SetInt(string key, int value) { }
        public static void Save() { }
    }
    public static class JsonUtility
    {
        public static T FromJson<T>(string json) { return default(T); }
        public static string ToJson(object value) { return ""; }
    }
    public static class Debug { public static void LogWarning(object value) { } public static void LogError(object value) { } }
    public static class Resources
    {
        public static T Load<T>(string path) where T : Object { return default(T); }
        public static T GetBuiltinResource<T>(string path) where T : Object { return default(T); }
    }
    public class Font : Object { }
    public class Texture : Object { }
    public class Texture2D : Texture
    {
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public static Texture2D whiteTexture;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public void SetPixel(int x, int y, Color color) { }
        public void SetPixels(Color[] colors) { }
        public void Apply(bool updateMipmaps = true, bool makeNoLongerReadable = false) { }
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white, red;
        public static Color Lerp(Color a, Color b, float t) { return default(Color); }
    }
    public struct Color32
    {
        public Color32(byte r, byte g, byte b, byte a) { }
        public static implicit operator Color(Color32 color) { return default(Color); }
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude { get { return 0; } }
        public float sqrMagnitude { get { return 0; } }
        public static Vector2 zero;
        public static Vector2 operator +(Vector2 a, Vector2 b) { return default(Vector2); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return default(Vector2); }
        public static Vector2 operator /(Vector2 a, float scale) { return default(Vector2); }
        public static implicit operator Vector3(Vector2 value) { return default(Vector3); }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 left, right, up, one;
        public static Vector3 operator +(Vector3 a, Vector3 b) { return default(Vector3); }
        public static Vector3 operator *(Vector3 a, float scale) { return default(Vector3); }
        public static implicit operator Vector2(Vector3 value) { return default(Vector2); }
    }
    public struct Quaternion { public static Quaternion identity; }
    public struct Matrix4x4 { public static Matrix4x4 TRS(Vector3 position, Quaternion rotation, Vector3 scale) { return default(Matrix4x4); } }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x=x; this.y=y; this.width=width; this.height=height; }
        public float xMax { get { return x+width; } }
        public float yMax { get { return y+height; } }
        public Vector2 center { get { return new Vector2(x+width/2,y+height/2); } }
        public bool Contains(Vector2 point) { return false; }
    }
    public static class Mathf
    {
        public const float Rad2Deg = 57.29578f;
        public static float Min(float a, float b) { return Math.Min(a,b); }
        public static int Min(int a, int b) { return Math.Min(a,b); }
        public static float Max(float a, float b) { return Math.Max(a,b); }
        public static int Max(int a, int b) { return Math.Max(a,b); }
        public static float Abs(float x) { return Math.Abs(x); }
        public static float Sin(float x) { return (float)Math.Sin(x); }
        public static float Sqrt(float x) { return (float)Math.Sqrt(x); }
        public static float Pow(float x,float y) { return (float)Math.Pow(x,y); }
        public static float Sign(float x) { return Math.Sign(x); }
        public static float Floor(float x) { return (float)Math.Floor(x); }
        public static int FloorToInt(float x) { return (int)Math.Floor(x); }
        public static int CeilToInt(float x) { return (int)Math.Ceiling(x); }
        public static int RoundToInt(float x) { return (int)Math.Round(x); }
        public static float Clamp(float x,float lo,float hi) { return Math.Min(hi,Math.Max(lo,x)); }
        public static float Clamp01(float x) { return Clamp(x,0,1); }
        public static float Lerp(float a,float b,float t) { return a+(b-a)*Clamp01(t); }
        public static float SmoothStep(float a,float b,float t) { return Lerp(a,b,t); }
        public static float Atan2(float y,float x) { return (float)Math.Atan2(y,x); }
    }
    public static class GUI
    {
        public static Color color, backgroundColor;
        public static Matrix4x4 matrix;
        public static bool enabled;
        public static GUISkin skin;
        public static void DrawTexture(Rect position, Texture texture, ScaleMode scaleMode = ScaleMode.StretchToFill, bool alphaBlend = true) { }
        public static void DrawTextureWithTexCoords(Rect position, Texture texture, Rect texCoords, bool alphaBlend = true) { }
        public static void BeginGroup(Rect rect) { }
        public static void EndGroup() { }
        public static void Label(Rect rect,string text,GUIStyle style) { }
        public static void Box(Rect rect,GUIContent content,GUIStyle style) { }
        public static bool Button(Rect rect,string text,GUIStyle style) { return false; }
    }
    public static class GUIUtility { public static void RotateAroundPivot(float angle,Vector2 pivot) { } }
    public class GUISkin { public GUIStyle label, button; }
    public class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle source) { }
        public Font font;
        public int fontSize;
        public FontStyle fontStyle;
        public TextAnchor alignment;
        public bool wordWrap,richText;
        public RectOffset border,padding;
        public GUIStyleState normal=new GUIStyleState(),hover=new GUIStyleState(),active=new GUIStyleState(),focused=new GUIStyleState();
    }
    public class GUIStyleState { public Color textColor; public Texture2D background; }
    public class GUIContent { public static GUIContent none; }
    public class RectOffset { public RectOffset(int left,int right,int top,int bottom) { } }
    public class Event { public static Event current; public EventType type; }
    public enum Space { World }
    public enum CameraClearFlags { SolidColor }
    public enum ScreenOrientation { LandscapeLeft }
    public enum ScaleMode { StretchToFill, ScaleToFit }
    public enum FilterMode { Point, Bilinear }
    public enum TextureWrapMode { Clamp }
    public enum TextureFormat { RGBA32 }
    public enum HideFlags { HideAndDontSave }
    public enum FontStyle { Normal, Bold }
    public enum TextAnchor { UpperLeft, MiddleLeft, MiddleCenter }
    public enum EventType { Repaint }
    public enum RuntimeInitializeLoadType { AfterSceneLoad }
    public sealed class DisallowMultipleComponent : Attribute { }
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { } }
    public sealed class SerializeField : Attribute { }
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public sealed class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    public static class Random { public static float Range(float min,float max) { return min; } public static int Range(int min,int max) { return min; } }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public string name; public int buildIndex; public UnityEngine.GameObject[] GetRootGameObjects() { return Array.Empty<UnityEngine.GameObject>(); } }
    public static class SceneManager { public static Scene GetActiveScene() { return default(Scene); } public static void LoadScene(int sceneBuildIndex) { } }
}
namespace UnityEngine.InputSystem
{
    public class ButtonControl { public bool isPressed, wasPressedThisFrame; }
    public class Vector2Control { public UnityEngine.Vector2 ReadValue() { return default(UnityEngine.Vector2); } }
    public class Keyboard
    {
        public static Keyboard current;
        public ButtonControl aKey=new ButtonControl(),dKey=new ButtonControl(),leftArrowKey=new ButtonControl(),rightArrowKey=new ButtonControl(),spaceKey=new ButtonControl(),upArrowKey=new ButtonControl(),wKey=new ButtonControl(),downArrowKey=new ButtonControl(),sKey=new ButtonControl(),escapeKey=new ButtonControl(),mKey=new ButtonControl(),rKey=new ButtonControl();
    }
    public class Mouse { public static Mouse current; public Vector2Control position; public ButtonControl leftButton; }
    public class TouchControl { public ButtonControl press; public Vector2Control position; }
    public class Touchscreen { public static Touchscreen current; public TouchControl primaryTouch; }
}
namespace TMPro { public class TMP_Text : UnityEngine.Behaviour { public string text; } }
