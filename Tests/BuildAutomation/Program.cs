// Executes the production Editor packaging code against explicit test doubles.
// Fixture EXEs/APKs are text/ZIP placeholders under a temporary directory, are
// deleted after each case, and are NEVER presented as playable Unity builds.
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using CityDino.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal static class Program
{
    private const string FixtureText = "UNIT TEST FIXTURE ONLY - NOT A UNITY PLAYER OR INSTALLABLE APK";
    private static int passed, failed;
    private static int Main()
    {
        Test("Unity source archive preserves Assets/metas Packages/settings and excludes machine caches", SourceArchive);
        Test("source export fails atomically when a required project folder is missing", SourceMissingFolder);
        Test("Windows build uses real player options and archives the entire engine/data/runtime folder", WindowsArchive);
        Test("missing build modules fail before invoking the engine or creating a submission", MissingModules);
        Test("wrong active targets fail before invoking the engine or creating a submission", WrongTargets);
        Test("failed or cancelled build reports cannot reuse stale Windows or Android submissions", FailedReports);
        Test("a successful Windows report cannot hide missing or empty executable engine or Data files", IncompleteWindows);
        Test("Android uses ARM64 IL2CPP APK flags and validates the required APK entries", AndroidArchive);
        Test("successful Android reports with missing entries or corrupt output produce no APK", IncompleteAndroid);
        Test("settings and identity restore after a thrown Windows or Android engine failure", ThrownBuildRestoration);
        Test("menu export respects Play Mode and scene cancellation and never reports false success", MenuFailure);
        Console.WriteLine();
        Console.WriteLine("Submission automation: " + passed + " passed, " + failed + " failed.");
        Console.WriteLine("Production Editor orchestration/ZIP I/O with explicit fake engine fixtures only; no Unity Editor, EXE or APK compilation/signing was executed.");
        return failed == 0 ? 0 : 1;
    }
    private static void Test(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error); }
    }
    private static void Check(bool ok, string message)
    { if (!ok) throw new InvalidOperationException(message); }

    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "dino-submission-contract-" + Guid.NewGuid().ToString("N"));
        public string Output { get { return Path.Combine(Root, "Builds", "Submission"); } }
        public Fixture()
        {
            Directory.CreateDirectory(Root);
            Write("Assets/CityDino/Scenes/CityRunner.unity", FixtureText);
            Write("Assets/CityDino/Scenes/CityRunner.unity.meta", "guid: 1234567890abcdef1234567890abcdef\n");
            Write("Assets/Art/Character.png", FixtureText);
            Write("Assets/Art/Character.png.meta", "guid: abcdef1234567890abcdef1234567890\n");
            Write("Packages/manifest.json", "{\"dependencies\":{}}\n");
            Write("Packages/packages-lock.json", "{\"dependencies\":{}}\n");
            Write("ProjectSettings/ProjectVersion.txt", "m_EditorVersion: 6000.4.0f1\n");
            Write("ProjectSettings/EditorBuildSettings.asset", FixtureText);
            Write("README.md", "Submission test fixture\n"); Write("BUILDING.md", "Build instructions fixture\n");
            Write("scripts/build-submission.ps1", "# unit test script fixture\n");
            Write("Library/cache.bin", FixtureText); Write(".git/config", FixtureText);
            Write("Temp/worker.tmp", FixtureText); Write("Builds/old/player.exe", FixtureText);
            Write("UserSettings/preferences.asset", FixtureText);
            Application.dataPath = Path.Combine(Root, "Assets");
            ResetEditor();
        }
        public void Write(string relative, string content)
        {
            string path = Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, content);
        }
        public string Artifact(string name) { return Path.Combine(Output, name); }
        public void Dispose()
        {
            BuildPipeline.TestBuild = null;
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }

    private static void ResetEditor()
    {
        PlayerSettings.companyName = "Original Company"; PlayerSettings.productName = "Original Product";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.original.windows");
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.original.android");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.useAPKExpansionFiles = true;
        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        EditorUserBuildSettings.activeBuildTarget = BuildTarget.StandaloneWindows64;
        EditorApplication.TestExitCode = null; EditorApplication.isPlayingOrWillChangePlaymode = false;
        UnityEditor.SceneManagement.EditorSceneManager.TestSaveConsent = true;
        EditorUtility.TestDialogTitle = EditorUtility.TestDialogMessage = EditorUtility.TestRevealedPath = null;
        BuildPipeline.TestBuildCalls = 0; BuildPipeline.TestModuleSupported = true; BuildPipeline.TestBuild = null;
        AssetDatabase.TestSaves = 0; Debug.TestExceptions.Clear();
    }
    private static void Restored()
    {
        Check(PlayerSettings.companyName == "Original Company" && PlayerSettings.productName == "Original Product", "Build identity leaked into the project");
        Check(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone) == "com.original.windows" &&
            PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) == "com.original.android", "Application identifier was not restored");
        Check(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) == ScriptingImplementation.IL2CPP &&
            PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.Mono2x, "Scripting backend was not restored");
        Check(PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARMv7 && PlayerSettings.Android.useCustomKeystore && PlayerSettings.Android.useAPKExpansionFiles &&
            EditorUserBuildSettings.buildAppBundle && EditorUserBuildSettings.exportAsGoogleAndroidProject, "Android architecture/signing/export settings were not restored");
    }
    private static void CleanWork(Fixture fixture)
    {
        if (!Directory.Exists(fixture.Output)) return;
        Check(!Directory.GetDirectories(fixture.Output).Any(p => Path.GetFileName(p).StartsWith(".windows-", StringComparison.Ordinal) ||
            Path.GetFileName(p).StartsWith(".android-", StringComparison.Ordinal)), "Temporary player work directory was left behind");
        Check(Directory.GetFiles(fixture.Output, "*.tmp").Length == 0, "Temporary archive was left behind");
    }
    private static void Success()
    { Check(EditorApplication.TestExitCode == 0 && Debug.TestExceptions.Count == 0, "Batch did not report success"); }
    private static void Failure()
    { Check(EditorApplication.TestExitCode == 1 && Debug.TestExceptions.Count == 1, "Batch failure was hidden or returned success"); }
    private static BuildReport Report(BuildResult result = BuildResult.Succeeded)
    { return new BuildReport { summary = new BuildSummary { result = result } }; }
    private static void Options(BuildPlayerOptions options, BuildTarget target)
    {
        Check(options.target == target && options.scenes.SequenceEqual(new[] { "Assets/CityDino/Scenes/CityRunner.unity" }), "Build target or explicit scene changed");
        Check(options.options == BuildOptions.CompressWithLz4HC, "Submission became a development/debug build or omitted compression");
        Check(PlayerSettings.companyName == "PPHuwng" && PlayerSettings.productName == "DINO CITY", "Submission identity was not applied inside the build");
    }
    private static void FakeWindows(BuildPlayerOptions options, string omitted = null)
    {
        string work = Path.GetDirectoryName(options.locationPathName); Directory.CreateDirectory(work);
        if (omitted != "exe") File.WriteAllText(options.locationPathName, omitted == "empty-exe" ? "" : FixtureText);
        if (omitted != "dll") File.WriteAllText(Path.Combine(work, "UnityPlayer.dll"), FixtureText);
        if (omitted != "data")
        {
            string data = Path.Combine(work, "DinoCity_Data"); Directory.CreateDirectory(data);
            if (omitted != "empty-data") File.WriteAllText(Path.Combine(data, "globalgamemanagers"), FixtureText);
        }
        Directory.CreateDirectory(Path.Combine(work, "MonoBleedingEdge", "EmbedRuntime"));
        File.WriteAllText(Path.Combine(work, "MonoBleedingEdge", "EmbedRuntime", "mono-2.0-bdwgc.dll"), FixtureText);
    }
    private static void FakeApk(BuildPlayerOptions options, string omitted = null)
    {
        using (var archive = ZipFile.Open(options.locationPathName, ZipArchiveMode.Create))
            foreach (string name in new[] { "AndroidManifest.xml", "classes.dex", "lib/arm64-v8a/libil2cpp.so", "lib/arm64-v8a/libunity.so", "assets/bin/Data/boot.config" })
            {
                if (name == omitted) continue;
                using (var writer = new StreamWriter(archive.CreateEntry(name).Open(), Encoding.UTF8)) writer.Write(FixtureText);
            }
    }
    private static void SourceArchive()
    {
        using (var fixture = new Fixture())
        {
            RunnerSubmissionBuild.ExportUnityBatch(); Success();
            using (var archive = ZipFile.OpenRead(fixture.Artifact("DINO-CITY-Unity.zip")))
            {
                string[] names = archive.Entries.Select(e => e.FullName).ToArray();
                foreach (string file in new[] { "Assets/CityDino/Scenes/CityRunner.unity", "Assets/CityDino/Scenes/CityRunner.unity.meta", "Assets/Art/Character.png", "Assets/Art/Character.png.meta", "Packages/manifest.json", "Packages/packages-lock.json", "ProjectSettings/ProjectVersion.txt", "ProjectSettings/EditorBuildSettings.asset", "README.md", "BUILDING.md", "scripts/build-submission.ps1" })
                    Check(names.Contains("DINO-CITY-Unity/" + file), "Source archive omitted " + file);
                Check(!names.Any(n => n.Contains("/Library/") || n.Contains("/.git/") || n.Contains("/Temp/") || n.Contains("/Builds/") || n.Contains("/UserSettings/")), "Source archive included a machine cache or old player");
                using (var reader = new StreamReader(archive.GetEntry("DINO-CITY-Unity/Assets/Art/Character.png.meta").Open()))
                    Check(reader.ReadToEnd() == File.ReadAllText(Path.Combine(fixture.Root, "Assets/Art/Character.png.meta")), "Source export rewrote a meta GUID");
            }
            Restored(); CleanWork(fixture); Check(BuildPipeline.TestBuildCalls == 0, "Source export invoked the fake engine");
        }
    }
    private static void SourceMissingFolder()
    {
        using (var fixture = new Fixture())
        {
            Directory.Delete(Path.Combine(fixture.Root, "Packages"), true);
            RunnerSubmissionBuild.ExportUnityBatch(); Failure();
            Check(!File.Exists(fixture.Artifact("DINO-CITY-Unity.zip")), "Incomplete source package was published");
            CleanWork(fixture); Restored();
        }
    }
    private static void WindowsArchive()
    {
        using (var fixture = new Fixture())
        {
            BuildPipeline.TestBuild = options =>
            {
                Options(options, BuildTarget.StandaloneWindows64);
                Check(options.subtarget == (int)StandaloneBuildSubtarget.Player, "Windows accidentally built a dedicated server");
                Check(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) == ScriptingImplementation.Mono2x, "Windows submission is not Mono");
                Check(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone) == "com.pphuwng.dinocity", "Windows submission has the wrong application ID");
                FakeWindows(options); return Report();
            };
            RunnerSubmissionBuild.BuildWindowsBatch(); Success(); Restored();
            using (var archive = ZipFile.OpenRead(fixture.Artifact("DINO-CITY-Windows.zip")))
                foreach (string file in new[] { "DinoCity.exe", "UnityPlayer.dll", "DinoCity_Data/globalgamemanagers", "MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll", "READ-ME.txt" })
                    Check(archive.GetEntry("DINO-CITY-Windows/" + file) != null, "Player ZIP omitted " + file);
            Check(BuildPipeline.TestBuildCalls == 1 && AssetDatabase.TestSaves == 1, "Build or restoration did not execute exactly once"); CleanWork(fixture);
        }
    }
    private static void MissingModules()
    {
        foreach (bool android in new[] { false, true }) using (var fixture = new Fixture())
        {
            BuildPipeline.TestModuleSupported = false; EditorUserBuildSettings.activeBuildTarget = android ? BuildTarget.Android : BuildTarget.StandaloneWindows64;
            if (android) RunnerSubmissionBuild.BuildAndroidBatch(); else RunnerSubmissionBuild.BuildWindowsBatch();
            Failure(); Restored(); Check(BuildPipeline.TestBuildCalls == 0 && !Directory.Exists(fixture.Output), "Missing module still attempted or packaged a build");
        }
    }
    private static void WrongTargets()
    {
        foreach (bool android in new[] { false, true }) using (var fixture = new Fixture())
        {
            EditorUserBuildSettings.activeBuildTarget = BuildTarget.StandaloneLinux64;
            if (android) RunnerSubmissionBuild.BuildAndroidBatch(); else RunnerSubmissionBuild.BuildWindowsBatch();
            Failure(); Restored(); Check(BuildPipeline.TestBuildCalls == 0 && !Directory.Exists(fixture.Output), "Wrong target silently built or packaged a different platform");
        }
    }
    private static void FailedReports()
    {
        foreach (bool android in new[] { false, true }) foreach (BuildResult result in new[] { BuildResult.Failed, BuildResult.Cancelled, BuildResult.Unknown }) using (var fixture = new Fixture())
        {
            EditorUserBuildSettings.activeBuildTarget = android ? BuildTarget.Android : BuildTarget.StandaloneWindows64;
            string artifact = fixture.Artifact(android ? "DINO-CITY-Android.apk" : "DINO-CITY-Windows.zip");
            Directory.CreateDirectory(fixture.Output); File.WriteAllText(artifact, "STALE UNIT TEST ARTIFACT");
            BuildPipeline.TestBuild = options => { if (android) FakeApk(options); else FakeWindows(options); return Report(result); };
            if (android) RunnerSubmissionBuild.BuildAndroidBatch(); else RunnerSubmissionBuild.BuildWindowsBatch();
            Failure(); Restored(); CleanWork(fixture); Check(!File.Exists(artifact), "Failed build left a stale package that looks successful");
        }
    }
    private static void IncompleteWindows()
    {
        foreach (string missing in new[] { "exe", "empty-exe", "dll", "data", "empty-data" }) using (var fixture = new Fixture())
        {
            BuildPipeline.TestBuild = options => { FakeWindows(options, missing); return Report(); };
            RunnerSubmissionBuild.BuildWindowsBatch(); Failure(); Restored(); CleanWork(fixture);
            Check(!File.Exists(fixture.Artifact("DINO-CITY-Windows.zip")), "Success report hid an incomplete Windows player: " + missing);
        }
    }
    private static void AndroidArchive()
    {
        using (var fixture = new Fixture())
        {
            EditorUserBuildSettings.activeBuildTarget = BuildTarget.Android;
            BuildPipeline.TestBuild = options =>
            {
                Options(options, BuildTarget.Android);
                Check(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP && PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64, "Android architecture/backend changed");
                Check(!EditorUserBuildSettings.buildAppBundle && !EditorUserBuildSettings.exportAsGoogleAndroidProject && !PlayerSettings.Android.useCustomKeystore && !PlayerSettings.Android.useAPKExpansionFiles, "APK/OBB flags or classroom signing settings are wrong");
                Check(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) == "com.pphuwng.dinocity", "Android submission has the wrong app ID");
                FakeApk(options); return Report();
            };
            RunnerSubmissionBuild.BuildAndroidBatch(); Success(); Restored(); CleanWork(fixture);
            using (var archive = ZipFile.OpenRead(fixture.Artifact("DINO-CITY-Android.apk")))
                Check(archive.GetEntry("lib/arm64-v8a/libil2cpp.so") != null, "Verified ARM64 APK was not moved into the submission folder");
        }
    }
    private static void IncompleteAndroid()
    {
        foreach (string missing in new[] { "AndroidManifest.xml", "classes.dex", "lib/arm64-v8a/libil2cpp.so", "file", "corrupt" }) using (var fixture = new Fixture())
        {
            EditorUserBuildSettings.activeBuildTarget = BuildTarget.Android;
            BuildPipeline.TestBuild = options =>
            {
                if (missing == "corrupt") File.WriteAllText(options.locationPathName, FixtureText);
                else if (missing != "file") FakeApk(options, missing);
                return Report();
            };
            RunnerSubmissionBuild.BuildAndroidBatch(); Failure(); Restored(); CleanWork(fixture);
            Check(!File.Exists(fixture.Artifact("DINO-CITY-Android.apk")), "Incomplete/corrupt APK was presented as a successful submission: " + missing);
        }
    }
    private static void ThrownBuildRestoration()
    {
        foreach (bool android in new[] { false, true }) using (var fixture = new Fixture())
        {
            EditorUserBuildSettings.activeBuildTarget = android ? BuildTarget.Android : BuildTarget.StandaloneWindows64;
            BuildPipeline.TestBuild = options => { throw new InvalidOperationException("Explicit test engine/toolchain failure"); };
            if (android) RunnerSubmissionBuild.BuildAndroidBatch(); else RunnerSubmissionBuild.BuildWindowsBatch();
            Failure(); Restored(); CleanWork(fixture); Check(AssetDatabase.TestSaves == 1, "Failure skipped settings restoration/save");
            Check(!File.Exists(fixture.Artifact(android ? "DINO-CITY-Android.apk" : "DINO-CITY-Windows.zip")), "Thrown build created a false submission");
        }
    }
    private static void MenuFailure()
    {
        using (var fixture = new Fixture())
        {
            EditorApplication.isPlayingOrWillChangePlaymode = true;
            RunnerSubmissionBuild.ExportUnityMenu();
            Check(EditorUtility.TestDialogMessage != null && EditorUtility.TestDialogMessage.Contains("Stop Play Mode") && !Directory.Exists(fixture.Output), "Menu exported while the editor was playing");
            EditorApplication.isPlayingOrWillChangePlaymode = false;
            UnityEditor.SceneManagement.EditorSceneManager.TestSaveConsent = false;
            RunnerSubmissionBuild.ExportUnityMenu();
            Check(!Directory.Exists(fixture.Output) && AssetDatabase.TestSaves == 0, "Declining scene save still exported the project");
            UnityEditor.SceneManagement.EditorSceneManager.TestSaveConsent = true; BuildPipeline.TestModuleSupported = false;
            RunnerSubmissionBuild.BuildWindowsMenu();
            Check(EditorUtility.TestDialogTitle == "DINO CITY build failed" && Debug.TestExceptions.Count == 1 && !Directory.Exists(fixture.Output), "Menu displayed a false build success");
            Check(EditorApplication.TestExitCode == null, "A menu failure terminated the user's editor");
        }
    }
}
