using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CityDino.Editor
{
    /// <summary>Real Unity player builds and complete source/player archives.</summary>
    public static class RunnerSubmissionBuild
    {
        private const string Scene = "Assets/CityDino/Scenes/CityRunner.unity";
        private const string SourceName = "DINO-CITY-Unity.zip";
        private const string WindowsName = "DINO-CITY-Windows.zip";
        private const string AndroidName = "DINO-CITY-Android.apk";

        public static void BuildWindowsBatch() { RunBatch(BuildWindows); }
        public static void BuildAndroidBatch() { RunBatch(BuildAndroid); }
        public static void ExportUnityBatch() { RunBatch(ExportUnity); }

        [MenuItem("City Dino/Build Submission/1 - Export Unity project ZIP", priority = 20)]
        public static void ExportUnityMenu() { RunMenu(ExportUnity); }

        [MenuItem("City Dino/Build Submission/2 - Build Windows 64-bit ZIP", priority = 21)]
        public static void BuildWindowsMenu() { RunMenu(BuildWindows); }

        [MenuItem("City Dino/Build Submission/3 - Build Android APK", priority = 22)]
        public static void BuildAndroidMenu() { RunMenu(BuildAndroid); }

        [MenuItem("City Dino/Build Submission/Open submission folder", priority = 30)]
        public static void OpenSubmissionFolder()
        {
            string output = OutputRoot();
            Directory.CreateDirectory(output);
            EditorUtility.RevealInFinder(output);
        }

        private static string ProjectRoot()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        private static string OutputRoot()
        {
            string value = Path.Combine(ProjectRoot(), "Builds", "Submission");
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-submissionPath") value = args[i + 1];
            string output = Path.GetFullPath(value);
            string root = Path.GetFullPath(ProjectRoot());
            if (string.Equals(output.TrimEnd(Path.DirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Choose a submission subfolder, not the project root.");
            foreach (string folder in new[] { "Assets", "Packages", "ProjectSettings" })
            {
                string protectedRoot = Path.Combine(root, folder);
                if (string.Equals(output, protectedRoot, StringComparison.OrdinalIgnoreCase) ||
                    output.StartsWith(protectedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Submission output must be outside Assets, Packages and ProjectSettings.");
            }
            return output;
        }

        private static void RequireTarget(BuildTarget target)
        {
            BuildTargetGroup group = target == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.Standalone;
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
                throw new InvalidOperationException("Unity build module is missing for " + target +
                    ". In Unity Hub > Installs > Add modules, install Windows Build Support or Android Build Support (SDK/NDK and OpenJDK).");
            if (EditorUserBuildSettings.activeBuildTarget != target)
                throw new InvalidOperationException("Activate " + target +
                    " in File > Build Profiles first. Batch builds must start a fresh Editor with -buildTarget Win64 or -buildTarget Android.");
            if (!File.Exists(Path.Combine(ProjectRoot(), Scene)))
                throw new FileNotFoundException("DINO CITY scene is missing.", Scene);
        }

        private static void BuildWindows()
        {
            RequireTarget(BuildTarget.StandaloneWindows64);
            string output = OutputRoot(); Directory.CreateDirectory(output);
            string zip = Path.Combine(output, WindowsName);
            if (File.Exists(zip)) File.Delete(zip);
            string work = Path.Combine(output, ".windows-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(work);
            try
            {
                using (var settings = new BuildSettingsScope(NamedBuildTarget.Standalone, false))
                {
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                    BuildPlayerOptions options = Options(BuildTarget.StandaloneWindows64, Path.Combine(work, "DinoCity.exe"));
                    options.subtarget = (int)StandaloneBuildSubtarget.Player;
                    RequireSuccess(BuildPipeline.BuildPlayer(options), options.locationPathName);
                }
                RequireFile(Path.Combine(work, "UnityPlayer.dll"));
                string data = Path.Combine(work, "DinoCity_Data");
                if (!Directory.Exists(data) || Directory.GetFileSystemEntries(data).Length == 0)
                    throw new InvalidOperationException("Windows build is incomplete: DinoCity_Data is missing or empty.");
                File.WriteAllText(Path.Combine(work, "READ-ME.txt"),
                    "DINO CITY - Windows 64-bit\r\nExtract this entire ZIP, then run DinoCity.exe.\r\n" +
                    "Keep UnityPlayer.dll, DinoCity_Data, MonoBleedingEdge and all other generated files beside the EXE.\r\n");
                ZipFolder(work, zip, "DINO-CITY-Windows");
                Debug.Log("Windows submission ready: " + zip);
            }
            finally { RemoveWorkFolder(work); }
        }

        private static void BuildAndroid()
        {
            RequireTarget(BuildTarget.Android);
            string output = OutputRoot(); Directory.CreateDirectory(output);
            string apk = Path.Combine(output, AndroidName);
            if (File.Exists(apk)) File.Delete(apk);
            string work = Path.Combine(output, ".android-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(work);
            try
            {
                string built = Path.Combine(work, AndroidName);
                using (var settings = new BuildSettingsScope(NamedBuildTarget.Android, true))
                {
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                    PlayerSettings.Android.useCustomKeystore = false;
                    PlayerSettings.Android.useAPKExpansionFiles = false;
                    EditorUserBuildSettings.buildAppBundle = false;
                    EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                    RequireSuccess(BuildPipeline.BuildPlayer(Options(BuildTarget.Android, built)), built);
                }
                using (ZipArchive archive = ZipFile.OpenRead(built))
                    foreach (string entry in new[] { "AndroidManifest.xml", "classes.dex", "lib/arm64-v8a/libil2cpp.so" })
                        if (archive.GetEntry(entry) == null)
                            throw new InvalidOperationException("Android output is not a complete ARM64 APK: " + entry + " is missing.");
                File.Move(built, apk);
                Debug.Log("Android submission ready: " + apk);
            }
            finally { RemoveWorkFolder(work); }
        }

        private static BuildPlayerOptions Options(BuildTarget target, string path)
        {
            return new BuildPlayerOptions
            {
                scenes = new[] { Scene }, target = target, locationPathName = path,
                options = BuildOptions.CompressWithLz4HC
            };
        }

        private static void RequireSuccess(BuildReport report, string file)
        {
            if (report == null || report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Unity player build did not succeed. Read the Editor/build log; no submission package was produced.");
            RequireFile(file);
        }

        private static void RequireFile(string file)
        {
            if (!File.Exists(file) || new FileInfo(file).Length == 0)
                throw new InvalidOperationException("Build output is missing or empty: " + file);
        }

        private static void ExportUnity()
        {
            string root = ProjectRoot(), output = OutputRoot();
            Directory.CreateDirectory(output);
            string zip = Path.Combine(output, SourceName), temporary = zip + ".tmp";
            if (File.Exists(temporary)) File.Delete(temporary);
            try
            {
                using (ZipArchive archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
                {
                    foreach (string folder in new[] { "Assets", "Packages", "ProjectSettings" })
                    {
                        string directory = Path.Combine(root, folder);
                        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
                        foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                            AddFile(archive, file, "DINO-CITY-Unity/" + file.Substring(root.Length + 1).Replace('\\', '/'));
                    }
                    foreach (string name in new[] { "README.md", "BUILDING.md" })
                        if (File.Exists(Path.Combine(root, name))) AddFile(archive, Path.Combine(root, name), "DINO-CITY-Unity/" + name);
                    string scripts = Path.Combine(root, "scripts");
                    if (Directory.Exists(scripts))
                        foreach (string file in Directory.GetFiles(scripts, "*", SearchOption.AllDirectories))
                            AddFile(archive, file, "DINO-CITY-Unity/" + file.Substring(root.Length + 1).Replace('\\', '/'));
                }
                if (File.Exists(zip)) File.Delete(zip);
                File.Move(temporary, zip);
                Debug.Log("Unity source submission ready: " + zip);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        private static void ZipFolder(string directory, string zip, string prefix)
        {
            string temporary = zip + ".tmp";
            if (File.Exists(temporary)) File.Delete(temporary);
            try
            {
                using (ZipArchive archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
                    foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                        AddFile(archive, file, prefix + "/" + file.Substring(directory.Length + 1).Replace('\\', '/'));
                if (File.Exists(zip)) File.Delete(zip);
                File.Move(temporary, zip);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        private static void AddFile(ZipArchive archive, string file, string name)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (Stream input = File.OpenRead(file))
            using (Stream destination = entry.Open()) input.CopyTo(destination);
        }

        private static void RemoveWorkFolder(string directory)
        {
            try { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
            catch (IOException) { Debug.LogWarning("Temporary build files remain in " + directory); }
        }

        private static void RunBatch(Action build)
        {
            try { build(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void RunMenu(Action build)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorUtility.DisplayDialog("DINO CITY", "Stop Play Mode before exporting.", "OK"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                AssetDatabase.SaveAssets(); build();
                EditorUtility.DisplayDialog("DINO CITY", "Submission ready in:\n" + OutputRoot(), "OK");
            }
            catch (Exception exception)
            { Debug.LogException(exception); EditorUtility.DisplayDialog("DINO CITY build failed", exception.Message, "OK"); }
        }

        /// <summary>Build identity/backends do not change the working project's saved settings.</summary>
        private sealed class BuildSettingsScope : IDisposable
        {
            private readonly NamedBuildTarget target;
            private readonly bool android;
            private readonly string company, product, identifier;
            private readonly ScriptingImplementation backend;
            private readonly AndroidArchitecture architecture;
            private readonly bool customKey, appBundle, exportProject, expansionFiles;

            public BuildSettingsScope(NamedBuildTarget target, bool android)
            {
                this.target = target; this.android = android;
                company = PlayerSettings.companyName; product = PlayerSettings.productName;
                identifier = PlayerSettings.GetApplicationIdentifier(target); backend = PlayerSettings.GetScriptingBackend(target);
                architecture = PlayerSettings.Android.targetArchitectures; customKey = PlayerSettings.Android.useCustomKeystore;
                expansionFiles = PlayerSettings.Android.useAPKExpansionFiles;
                appBundle = EditorUserBuildSettings.buildAppBundle; exportProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
                PlayerSettings.companyName = "PPHuwng"; PlayerSettings.productName = "DINO CITY";
                PlayerSettings.SetApplicationIdentifier(target, "com.pphuwng.dinocity");
            }

            public void Dispose()
            {
                PlayerSettings.companyName = company; PlayerSettings.productName = product;
                PlayerSettings.SetApplicationIdentifier(target, identifier); PlayerSettings.SetScriptingBackend(target, backend);
                if (android)
                {
                    PlayerSettings.Android.targetArchitectures = architecture; PlayerSettings.Android.useCustomKeystore = customKey;
                    PlayerSettings.Android.useAPKExpansionFiles = expansionFiles;
                    EditorUserBuildSettings.buildAppBundle = appBundle; EditorUserBuildSettings.exportAsGoogleAndroidProject = exportProject;
                }
                AssetDatabase.SaveAssets();
            }
        }
    }
}
