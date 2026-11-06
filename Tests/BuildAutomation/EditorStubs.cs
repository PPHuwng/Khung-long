// Minimal API surface for packaging and orchestration contracts only. This is
// outside Assets and never enters Unity or produces a usable player binary.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public static class Application { public static string dataPath; }
    public static class Debug
    {
        public static readonly List<Exception> TestExceptions = new List<Exception>();
        public static void Log(object value) { }
        public static void LogWarning(object value) { }
        public static void LogException(Exception value) { TestExceptions.Add(value); }
    }
}

namespace UnityEditor.Build
{
    public readonly struct NamedBuildTarget : IEquatable<NamedBuildTarget>
    {
        public string TargetName { get; }
        private NamedBuildTarget(string name) { TargetName = name; }
        public static readonly NamedBuildTarget Standalone = new NamedBuildTarget("Standalone");
        public static readonly NamedBuildTarget Android = new NamedBuildTarget("Android");
        public bool Equals(NamedBuildTarget other) { return TargetName == other.TargetName; }
        public override bool Equals(object other) { return other is NamedBuildTarget target && Equals(target); }
        public override int GetHashCode() { return TargetName == null ? 0 : TargetName.GetHashCode(); }
    }
}

namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown, Succeeded, Failed, Cancelled }
    public struct BuildSummary { public BuildResult result; }
    public sealed class BuildReport { public BuildSummary summary; }
}

namespace UnityEditor.SceneManagement
{
    public static class EditorSceneManager
    {
        public static bool TestSaveConsent = true;
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() { return TestSaveConsent; }
    }
}

namespace UnityEditor
{
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MenuItem : Attribute
    {
        public int priority;
        public MenuItem(string name) { }
    }
    public enum BuildTarget { StandaloneWindows64, Android, StandaloneLinux64 }
    public enum BuildTargetGroup { Standalone, Android }
    public enum StandaloneBuildSubtarget { Default, Player, Server }
    public enum ScriptingImplementation { Mono2x, IL2CPP }
    [Flags] public enum AndroidArchitecture { ARMv7 = 1, ARM64 = 2 }
    [Flags] public enum BuildOptions { None = 0, CompressWithLz4HC = 1 }
    public struct BuildPlayerOptions
    {
        public string[] scenes;
        public BuildTarget target;
        public string locationPathName;
        public BuildOptions options;
        public int subtarget;
    }
    public static class EditorUserBuildSettings
    {
        public static BuildTarget activeBuildTarget;
        public static bool buildAppBundle, exportAsGoogleAndroidProject;
    }
    public static class EditorApplication
    {
        public static bool isPlayingOrWillChangePlaymode;
        public static int? TestExitCode;
        public static void Exit(int code) { TestExitCode = code; }
    }
    public static class EditorUtility
    {
        public static string TestDialogTitle, TestDialogMessage, TestRevealedPath;
        public static void RevealInFinder(string path) { TestRevealedPath = path; }
        public static bool DisplayDialog(string title, string message, string ok)
        { TestDialogTitle = title; TestDialogMessage = message; return true; }
    }
    public static class AssetDatabase
    {
        public static int TestSaves;
        public static void SaveAssets() { TestSaves++; }
    }
    public static class PlayerSettings
    {
        public static string companyName, productName;
        private static readonly Dictionary<NamedBuildTarget, string> identifiers = new Dictionary<NamedBuildTarget, string>();
        private static readonly Dictionary<NamedBuildTarget, ScriptingImplementation> backends = new Dictionary<NamedBuildTarget, ScriptingImplementation>();
        public static string GetApplicationIdentifier(NamedBuildTarget target) { return identifiers[target]; }
        public static void SetApplicationIdentifier(NamedBuildTarget target, string value) { identifiers[target] = value; }
        public static ScriptingImplementation GetScriptingBackend(NamedBuildTarget target) { return backends[target]; }
        public static void SetScriptingBackend(NamedBuildTarget target, ScriptingImplementation value) { backends[target] = value; }
        public static class Android
        {
            public static AndroidArchitecture targetArchitectures;
            public static bool useCustomKeystore, useAPKExpansionFiles;
        }
    }
    public static class BuildPipeline
    {
        public static bool TestModuleSupported = true;
        public static Func<BuildPlayerOptions, BuildReport> TestBuild;
        public static BuildPlayerOptions TestLastOptions;
        public static int TestBuildCalls;
        public static bool IsBuildTargetSupported(BuildTargetGroup group, BuildTarget target)
        { return TestModuleSupported && group == (target == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.Standalone); }
        public static BuildReport BuildPlayer(BuildPlayerOptions options)
        {
            TestBuildCalls++; TestLastOptions = options;
            if (TestBuild == null) throw new InvalidOperationException("No fake test engine configured.");
            return TestBuild(options);
        }
    }
}
