#requires -Version 5.1
<#
Build the real Unity project into three submission packages.
Run with Unity closed. -SourceOnly does not need a Unity installation.
Optional publishing uses your existing GitHub CLI login; no credentials belong here.
#>
[CmdletBinding()]
param(
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),
    [string]$UnityPath,
    [string]$OutputPath,
    [switch]$SourceOnly,
    [switch]$SkipBuild,
    [switch]$Publish,
    [string]$Repository = 'PPHuwng/Khung-long',
    [string]$TargetRef = 'codex/dino-city-runner',
    [string]$ReleaseTag = ('submission-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$script:Utf8 = [Text.UTF8Encoding]::new($false)

function Get-FullDirectory([string]$Path) {
    return [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Test-Within([string]$Path, [string]$Parent) {
    return $Path.Equals($Parent, [StringComparison]::OrdinalIgnoreCase) -or
        $Path.StartsWith($Parent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Assert-ProjectClosed {
    $lockPath = Join-Path $script:ProjectRoot 'Temp/UnityLockfile'
    if (Test-Path -LiteralPath $lockPath) {
        try {
            $probe = [IO.File]::Open($lockPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            $probe.Dispose()
        }
        catch {
            throw 'This project is open in Unity. Save your work and close the Editor before exporting. No process was stopped.'
        }
    }
    if ($env:OS -eq 'Windows_NT') {
        $runningEditors = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction SilentlyContinue)
        foreach ($editor in $runningEditors) {
            if ($editor.CommandLine -and $editor.CommandLine.IndexOf($script:ProjectRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw 'This project is open in Unity. Save your work and close the Editor before exporting. No process was stopped.'
            }
        }
    }
}

function Resolve-UnityEditor {
    if ($env:OS -ne 'Windows_NT') {
        throw 'The full submission command runs on Windows with the licensed Unity Hub Editor. -SourceOnly also works on other systems.'
    }
    $candidates = New-Object 'System.Collections.Generic.List[string]'
    if ($UnityPath) {
        $candidate = [IO.Path]::GetFullPath($UnityPath)
        if (Test-Path -LiteralPath $candidate -PathType Container) { $candidate = Join-Path $candidate 'Unity.exe' }
        $candidates.Add($candidate)
    }
    else {
        foreach ($basePath in @($env:ProgramFiles, ${env:ProgramFiles(x86)})) {
            if ($basePath) { $candidates.Add((Join-Path $basePath ('Unity/Hub/Editor/' + $script:EditorVersion + '/Editor/Unity.exe'))) }
        }
    }
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
    }
    throw ('Unity ' + $script:EditorVersion + ' was not found. Install this version in Unity Hub, or pass -UnityPath "D:\Unity\Editor\Unity.exe".')
}

function Quote-NativeArgument([string]$Value) {
    # Windows paths cannot contain quotation marks. The command never goes
    # through cmd.exe or Invoke-Expression, so shell metacharacters are literal.
    if ($Value.Contains('"')) { throw 'An argument contains an unsupported quotation mark.' }
    return '"' + $Value + '"'
}

function Invoke-UnityBuild([string]$Target, [string]$Method, [string]$LogName) {
    $logPath = Join-Path $script:OutputRoot $LogName
    $arguments = @('-batchmode', '-nographics', '-quit', '-projectPath', $script:ProjectRoot,
        '-buildTarget', $Target, '-executeMethod', $Method, '-submissionPath', $script:OutputRoot, '-logFile', $logPath)
    $commandLine = ($arguments | ForEach-Object { Quote-NativeArgument $_ }) -join ' '
    Write-Host ('Building ' + $Target + '. This can take several minutes. Log: ' + $logPath)
    # Wait for this Editor, not persistent Hub/licensing helper descendants.
    $process = Start-Process -FilePath $script:EditorExe -ArgumentList $commandLine -WorkingDirectory $script:ProjectRoot -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        if (Test-Path -LiteralPath $logPath) {
            Get-Content -LiteralPath $logPath -Tail 30 | ForEach-Object { Write-Host $_ }
        }
        throw ('Unity ' + $Target + ' failed with exit code ' + $process.ExitCode + '. Read ' + $logPath + '. Check the Unity license and required Hub build modules.')
    }
}

function Get-SourceFiles([string]$Directory, [bool]$SkipTestCaches = $false) {
    $stack = New-Object 'System.Collections.Generic.Stack[string]'
    $stack.Push($Directory)
    while ($stack.Count -gt 0) {
        $current = $stack.Pop()
        foreach ($item in Get-ChildItem -LiteralPath $current -Force) {
            # Git internals are never Unity source, including an embedded
            # package's nested repository or worktree metadata file.
            if ($item.Name -eq '.git') { continue }
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw ('Source archive refuses a junction or symbolic link: ' + $item.FullName)
            }
            if ($item.PSIsContainer) {
                # A folder called bin/Library can be a valid game asset. Only
                # optional test compiler caches are filtered from the archive.
                if (-not $SkipTestCaches -or $item.Name -notin @('bin', 'obj', '.git', 'Library', 'Temp', 'Logs', 'Build', 'Builds', 'UserSettings')) { $stack.Push($item.FullName) }
            }
            else { Write-Output $item.FullName }
        }
    }
}

function Export-UnitySource {
    $temporaryZip = Join-Path $script:OutputRoot ('.unity-' + [Guid]::NewGuid().ToString('N') + '.zip')
    try {
        $stream = [IO.File]::Open($temporaryZip, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        try {
            $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $true)
            try {
                foreach ($folder in @('Assets', 'Packages', 'ProjectSettings', 'scripts', 'Tests')) {
                    $sourceFolder = Join-Path $script:ProjectRoot $folder
                    if (-not (Test-Path -LiteralPath $sourceFolder -PathType Container)) { continue }
                    foreach ($file in Get-SourceFiles $sourceFolder ($folder -eq 'Tests')) {
                        $relative = $file.Substring($script:ProjectRoot.Length + 1).Replace('\', '/')
                        $entry = $archive.CreateEntry('DINO-CITY-Unity/' + $relative, [IO.Compression.CompressionLevel]::Optimal)
                        $sourceStream = [IO.File]::OpenRead($file)
                        $entryStream = $entry.Open()
                        try { $sourceStream.CopyTo($entryStream) }
                        finally { $entryStream.Dispose(); $sourceStream.Dispose() }
                    }
                }
                foreach ($name in @('README.md', 'BUILDING.md', 'LICENSE', '.gitignore')) {
                    $file = Join-Path $script:ProjectRoot $name
                    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { continue }
                    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file, 'DINO-CITY-Unity/' + $name, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
                }
            }
            finally { $archive.Dispose() }
        }
        finally { $stream.Dispose() }
        Move-Item -LiteralPath $temporaryZip -Destination $script:UnityZip -Force
    }
    finally { if (Test-Path -LiteralPath $temporaryZip) { Remove-Item -LiteralPath $temporaryZip -Force } }
    Write-Host ('Unity source: ' + $script:UnityZip)
}

function Read-ZipEntryNames([string]$Path) {
    $archive = [IO.Compression.ZipFile]::OpenRead($Path)
    try { foreach ($entry in $archive.Entries) { Write-Output $entry.FullName } }
    finally { $archive.Dispose() }
}

function Assert-SubmissionFiles {
    foreach ($file in $script:Artifacts) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or (Get-Item -LiteralPath $file).Length -le 0) {
            throw ('Missing or empty submission file: ' + $file + '. A successful source export alone is not a Windows or Android build.')
        }
    }
    $unityEntries = @(Read-ZipEntryNames $script:UnityZip)
    foreach ($required in @('DINO-CITY-Unity/Assets/CityDino/Scenes/CityRunner.unity', 'DINO-CITY-Unity/Packages/manifest.json', 'DINO-CITY-Unity/ProjectSettings/ProjectVersion.txt')) {
        if ($unityEntries -notcontains $required) { throw ('Unity source ZIP is incomplete: ' + $required) }
    }
    $windowsEntries = @(Read-ZipEntryNames $script:WindowsZip)
    $player = @($windowsEntries | Where-Object { $_ -match '(^|/)DinoCity\.exe$' })
    if ($player.Count -ne 1) { throw 'Windows ZIP must contain exactly one DinoCity.exe and its complete player folder.' }
    $prefix = $player[0].Substring(0, $player[0].Length - 'DinoCity.exe'.Length)
    $hasData = @($windowsEntries | Where-Object { $_.StartsWith($prefix + 'DinoCity_Data/', [StringComparison]::Ordinal) }).Count -gt 0
    if ($windowsEntries -notcontains ($prefix + 'UnityPlayer.dll') -or -not $hasData) {
        throw 'Windows ZIP lacks UnityPlayer.dll or DinoCity_Data. Do not submit an EXE by itself.'
    }
    $androidEntries = @(Read-ZipEntryNames $script:AndroidApk)
    foreach ($required in @('AndroidManifest.xml', 'classes.dex', 'lib/arm64-v8a/libunity.so', 'lib/arm64-v8a/libil2cpp.so')) {
        if ($androidEntries -notcontains $required) { throw ('Android APK is incomplete or not the expected ARM64 player: ' + $required) }
    }
}

function Write-SubmissionManifest {
    $lines = @($script:Artifacts | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($_) })
    [IO.File]::WriteAllLines($script:ChecksumFile, $lines, $script:Utf8)
    $readme = @('DINO CITY - submission packages', ('Unity Editor: ' + $script:EditorVersion),
        ('Built locally at: ' + [DateTime]::UtcNow.ToString('u')), '',
        '1. DINO-CITY-Unity.zip: extract, then add its project folder in Unity Hub.',
        '2. DINO-CITY-Windows.zip: extract the entire archive, then run DinoCity.exe. Keep all DLL and Data files beside it.',
        '3. DINO-CITY-Android.apk: copy to an ARM64 Android phone, allow installation from the file manager when prompted, then install.',
        'The APK uses Unity local/debug signing for testing and class submission, not a Play Store release.',
        'SHA256SUMS.txt records the exact three files. Unity Editor built the player packages; this script does not replace compilation.',
        'Check gameplay, sound, menus and touch on the actual Windows computer and Android phone before submitting.')
    [IO.File]::WriteAllLines($script:SubmissionReadme, $readme, $script:Utf8)
}

function Assert-ExistingHashes {
    if (-not (Test-Path -LiteralPath $script:ChecksumFile -PathType Leaf)) { throw '-SkipBuild requires SHA256SUMS.txt from a completed three-package export.' }
    $recorded = @{}
    foreach ($line in Get-Content -LiteralPath $script:ChecksumFile) {
        if ($line -match '^([a-fA-F0-9]{64})  ([^/\\]+)$') { $recorded[$matches[2]] = $matches[1] }
    }
    foreach ($file in $script:Artifacts) {
        $name = [IO.Path]::GetFileName($file)
        if (-not $recorded.ContainsKey($name) -or $recorded[$name] -ne (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash) {
            throw ('Existing output changed or is incomplete: ' + $name + '. Build all packages again instead of publishing a partial export.')
        }
    }
    if (-not (Test-Path -LiteralPath $script:SubmissionReadme -PathType Leaf)) { throw 'Missing SUBMISSION-README.txt from the completed export.' }
}

function Assert-GitHubReady {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw 'Install GitHub CLI and run gh auth login, then retry with -SkipBuild -Publish if the packages already exist.' }
    & gh auth status --hostname github.com *> $null
    if ($LASTEXITCODE -ne 0) { throw 'GitHub CLI is not logged in. Run gh auth login, then retry with -SkipBuild -Publish. Never put a token in this script.' }
    & gh release view $ReleaseTag --repo $Repository *> $null
    if ($LASTEXITCODE -eq 0) { throw ('Release ' + $ReleaseTag + ' already exists. Choose a new -ReleaseTag; existing assets are never overwritten.') }
}

if ($SourceOnly -and ($SkipBuild -or $Publish)) { throw '-SourceOnly cannot be combined with -SkipBuild or -Publish. Publishing requires all three real packages.' }
if ($Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') { throw 'Repository must have the form owner/name.' }
if ($ReleaseTag -notmatch '^[A-Za-z0-9][A-Za-z0-9._/-]*$') { throw 'ReleaseTag must start with a letter or digit and contain only tag-safe characters.' }
$script:ProjectRoot = Get-FullDirectory $ProjectPath
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
    if (-not (Test-Path -LiteralPath (Join-Path $script:ProjectRoot $folder) -PathType Container)) { throw ('Not a Unity project; missing ' + $folder) }
}
$versionText = Get-Content -LiteralPath (Join-Path $script:ProjectRoot 'ProjectSettings/ProjectVersion.txt') -Raw
if ($versionText -notmatch '(?m)^m_EditorVersion:\s*([^\r\n]+)') { throw 'Cannot read the Unity version from ProjectSettings/ProjectVersion.txt.' }
$script:EditorVersion = $matches[1].Trim()
if (-not $OutputPath) { $OutputPath = Join-Path $script:ProjectRoot 'Builds/Submission' }
$script:OutputRoot = Get-FullDirectory $OutputPath
if ($script:OutputRoot -eq $script:ProjectRoot) { throw 'Use a dedicated output directory, not the project root.' }
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings', 'scripts', 'Tests')) {
    if (Test-Within $script:OutputRoot (Join-Path $script:ProjectRoot $folder)) { throw ('OutputPath cannot be inside source folder ' + $folder) }
}
[IO.Directory]::CreateDirectory($script:OutputRoot) | Out-Null
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$script:UnityZip = Join-Path $script:OutputRoot 'DINO-CITY-Unity.zip'
$script:WindowsZip = Join-Path $script:OutputRoot 'DINO-CITY-Windows.zip'
$script:AndroidApk = Join-Path $script:OutputRoot 'DINO-CITY-Android.apk'
$script:ChecksumFile = Join-Path $script:OutputRoot 'SHA256SUMS.txt'
$script:SubmissionReadme = Join-Path $script:OutputRoot 'SUBMISSION-README.txt'
$script:Artifacts = @($script:UnityZip, $script:WindowsZip, $script:AndroidApk)

if ($SourceOnly) {
    Assert-ProjectClosed
    Export-UnitySource
    $sourceHash = (Get-FileHash -LiteralPath $script:UnityZip -Algorithm SHA256).Hash.ToLowerInvariant() + '  DINO-CITY-Unity.zip'
    [IO.File]::WriteAllText((Join-Path $script:OutputRoot 'SHA256SUMS-Unity.txt'), $sourceHash + [Environment]::NewLine, $script:Utf8)
    Write-Host 'Source archive only. No Windows or Android build was run.'
    return
}
if ($Publish) { Assert-GitHubReady }
if ($SkipBuild) {
    Assert-SubmissionFiles
    Assert-ExistingHashes
}
else {
    Assert-ProjectClosed
    if (-not (Test-Path -LiteralPath (Join-Path $script:ProjectRoot 'Assets/CityDino/Editor/RunnerSubmissionBuild.cs') -PathType Leaf)) {
        throw 'The Unity submission builder is missing. Copy the latest Assets/CityDino folder into this project before building.'
    }
    $script:EditorExe = Resolve-UnityEditor
    if (Test-Path -LiteralPath $script:ChecksumFile) { Remove-Item -LiteralPath $script:ChecksumFile -Force }
    Invoke-UnityBuild 'Win64' 'CityDino.Editor.RunnerSubmissionBuild.BuildWindowsBatch' 'windows-build.log'
    Invoke-UnityBuild 'Android' 'CityDino.Editor.RunnerSubmissionBuild.BuildAndroidBatch' 'android-build.log'
    Export-UnitySource
    Assert-SubmissionFiles
    Write-SubmissionManifest
}

Write-Host ('All three package files verified in ' + $script:OutputRoot)
if ($Publish) {
    $notesPath = Join-Path $script:OutputRoot '.release-notes.md'
    $notes = @(('DINO CITY submission packages built with Unity ' + $script:EditorVersion + '.'), '',
        '- Unity project ZIP, including Assets, Packages and ProjectSettings.',
        '- Windows 64-bit player ZIP. Extract the entire folder and run DinoCity.exe.',
        '- Android ARM64 APK for direct installation and testing (local/debug signing).',
        '- SHA256SUMS.txt and SUBMISSION-README.txt describe and identify the exact outputs.', '',
        'These packages were compiled from the local project used for this export. Test on the intended devices before submission.')
    [IO.File]::WriteAllLines($notesPath, $notes, $script:Utf8)
    $publishFiles = @($script:Artifacts) + @($script:ChecksumFile, $script:SubmissionReadme)
    & gh release create $ReleaseTag @publishFiles --repo $Repository --target $TargetRef --title 'DINO CITY - Submission' --notes-file $notesPath
    if ($LASTEXITCODE -ne 0) { throw 'GitHub upload failed. Local packages are intact; authenticate and retry with -SkipBuild -Publish and a new release tag.' }
    Write-Host ('Published: https://github.com/' + $Repository + '/releases/tag/' + $ReleaseTag)
}
