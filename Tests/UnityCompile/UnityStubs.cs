// Compile-only surface of Unity APIs referenced by this repository.
// Signatures catch C# integration errors; affine math and recorded draw calls
// support coordinate-contract tests. No Unity events, rasterization, asset import,
// serialization or actual Editor execution is emulated.
using System;
using System.Collections.Generic;
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
        public static readonly Dictionary<string,Object> TestAssets = new Dictionary<string,Object>();
        public static T Load<T>(string path) where T : Object { Object value; return TestAssets.TryGetValue(path,out value) ? value as T : null; }
        public static T GetBuiltinResource<T>(string path) where T : Object { return default(T); }
    }
    public class Font : Object { }
    public class Texture : Object { public virtual int width { get; set; } public virtual int height { get; set; } }
    public class Texture2D : Texture
    {
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { this.width=width; this.height=height; }
        public static Texture2D whiteTexture = new Texture2D(1,1,TextureFormat.RGBA32,false);
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
        public static Color white=new Color(1,1,1,1), red=new Color(1,0,0,1), clear=new Color(0,0,0,0);
        public static Color Lerp(Color a, Color b, float t) { t=Mathf.Clamp01(t); return new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t); }
    }
    public struct Color32
    {
        private byte r,g,b,a;
        public Color32(byte r, byte g, byte b, byte a) { this.r=r;this.g=g;this.b=b;this.a=a; }
        public static implicit operator Color(Color32 color) { return new Color(color.r/255f,color.g/255f,color.b/255f,color.a/255f); }
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude { get { return (float)Math.Sqrt(x*x+y*y); } }
        public float sqrMagnitude { get { return x*x+y*y; } }
        public static Vector2 zero;
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x+b.x, a.y+b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x-b.x, a.y-b.y); }
        public static Vector2 operator /(Vector2 a, float scale) { return new Vector2(a.x/scale, a.y/scale); }
        public static implicit operator Vector3(Vector2 value) { return new Vector3(value.x,value.y,0); }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 left=new Vector3(-1,0,0),right=new Vector3(1,0,0),up=new Vector3(0,1,0),one=new Vector3(1,1,1);
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); }
        public static Vector3 operator *(Vector3 a, float scale) { return new Vector3(a.x*scale,a.y*scale,a.z*scale); }
        public static implicit operator Vector2(Vector3 value) { return new Vector2(value.x,value.y); }
    }
    public struct Quaternion
    {
        internal System.Numerics.Quaternion Value;
        public static Quaternion identity = new Quaternion { Value=System.Numerics.Quaternion.Identity };
        public static Quaternion Euler(float x,float y,float z)
        {
            const float radians=(float)Math.PI/180;
            return new Quaternion { Value=System.Numerics.Quaternion.CreateFromYawPitchRoll(y*radians,x*radians,z*radians) };
        }
    }
    public struct Matrix4x4
    {
        private System.Numerics.Matrix4x4 value;
        public static Matrix4x4 identity = new Matrix4x4 { value=System.Numerics.Matrix4x4.Identity };
        public static Matrix4x4 TRS(Vector3 position,Quaternion rotation,Vector3 scale)
        {
            return new Matrix4x4 { value=System.Numerics.Matrix4x4.CreateScale(scale.x,scale.y,scale.z)
                *System.Numerics.Matrix4x4.CreateFromQuaternion(rotation.Value)
                *System.Numerics.Matrix4x4.CreateTranslation(position.x,position.y,position.z) };
        }
        // System.Numerics uses row vectors; Unity uses column vectors.
        public static Matrix4x4 operator *(Matrix4x4 a,Matrix4x4 b) { return new Matrix4x4 { value=b.value*a.value }; }
        public Vector3 MultiplyPoint3x4(Vector3 point)
        {
            var transformed=System.Numerics.Vector3.Transform(new System.Numerics.Vector3(point.x,point.y,point.z),value);
            return new Vector3(transformed.X,transformed.Y,transformed.Z);
        }
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x=x; this.y=y; this.width=width; this.height=height; }
        public float xMax { get { return x+width; } }
        public float yMax { get { return y+height; } }
        public Vector2 center { get { return new Vector2(x+width/2,y+height/2); } }
        public bool Contains(Vector2 point) { return point.x>=x && point.x<xMax && point.y>=y && point.y<yMax; }
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
        public static float SmoothStep(float a,float b,float t) { t=Clamp01(t); return a+(b-a)*t*t*(3-2*t); }
        public static float Atan2(float y,float x) { return (float)Math.Atan2(y,x); }
    }
    public sealed class RecordedDraw
    {
        public string Kind,Text;
        public Texture Texture;
        public Rect Position,UV;
        public Color Color;
        public Matrix4x4 Matrix;
        public Vector3 Centre { get { return Matrix.MultiplyPoint3x4(Position.center); } }
    }
    public static class GUI
    {
        public static Color color=Color.white,backgroundColor=Color.white;
        public static Matrix4x4 matrix=Matrix4x4.identity;
        public static bool enabled=true;
        public static GUISkin skin=new GUISkin { label=new GUIStyle(),button=new GUIStyle() };
        public static readonly List<RecordedDraw> DrawCalls=new List<RecordedDraw>();
        private static readonly Stack<Matrix4x4> groups=new Stack<Matrix4x4>();
        public static void DrawTexture(Rect position,Texture texture,ScaleMode scaleMode=ScaleMode.StretchToFill,bool alphaBlend=true)
        { DrawCalls.Add(new RecordedDraw { Kind="Texture",Position=position,Texture=texture,Color=color,Matrix=matrix }); }
        public static void DrawTextureWithTexCoords(Rect position,Texture texture,Rect texCoords,bool alphaBlend=true)
        { DrawCalls.Add(new RecordedDraw { Kind="UV",Position=position,Texture=texture,UV=texCoords,Color=color,Matrix=matrix }); }
        public static void BeginGroup(Rect rect) { groups.Push(matrix); matrix=matrix*Matrix4x4.TRS(new Vector3(rect.x,rect.y,0),Quaternion.identity,Vector3.one); }
        public static void EndGroup() { matrix=groups.Pop(); }
        public static void Label(Rect rect,string text,GUIStyle style)
        { DrawCalls.Add(new RecordedDraw { Kind="Label",Position=rect,Text=text,Color=color,Matrix=matrix }); }
        public static void Box(Rect rect,GUIContent content,GUIStyle style) { }
        public static bool Button(Rect rect,string text,GUIStyle style) { return false; }
    }
    public static class GUIUtility
    {
        // Model the legacy prepend used in Unity 6000.4. Raw virtual pivots are
        // not transformed by the canvas matrix. Regression tests reproduce why
        // this API displaced sprites when called under a scaled GUI.matrix.
        public static void RotateAroundPivot(float angle,Vector2 pivot)
        {
            GUI.matrix=Matrix4x4.TRS(pivot,Quaternion.Euler(0,0,angle),Vector3.one)
                *Matrix4x4.TRS(new Vector3(-pivot.x,-pivot.y,0),Quaternion.identity,Vector3.one)*GUI.matrix;
        }
    }
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
    public class Vector2Control
    {
        // Harness-only value injection; no OS device or event stream is emulated.
        public UnityEngine.Vector2 TestValue;
        public UnityEngine.Vector2 ReadValue() { return TestValue; }
    }
    public class Keyboard
    {
        public static Keyboard current;
        public ButtonControl aKey=new ButtonControl(),dKey=new ButtonControl(),leftArrowKey=new ButtonControl(),rightArrowKey=new ButtonControl(),spaceKey=new ButtonControl(),upArrowKey=new ButtonControl(),wKey=new ButtonControl(),downArrowKey=new ButtonControl(),sKey=new ButtonControl(),escapeKey=new ButtonControl(),mKey=new ButtonControl(),rKey=new ButtonControl();
    }
    public class Mouse { public static Mouse current; public Vector2Control position; public ButtonControl leftButton; }
    public class TouchControl { public ButtonControl press; public Vector2Control position; }
    public class Touchscreen
    {
        public static Touchscreen current;
        public TouchControl primaryTouch;
        // The production package exposes ReadOnlyArray<TouchControl>; the list
        // retains the Count/indexer surface used by the multitouch controller.
        public List<TouchControl> touches = new List<TouchControl>();
    }
}
namespace TMPro { public class TMP_Text : UnityEngine.Behaviour { public string text; } }
