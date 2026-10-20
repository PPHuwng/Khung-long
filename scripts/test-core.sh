#!/usr/bin/env bash
set -euo pipefail
repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
if command -v dotnet >/dev/null 2>&1; then
  dotnet_command="$(command -v dotnet)"
elif [[ -x /workspace/.tools/dotnet/dotnet ]]; then
  dotnet_command=/workspace/.tools/dotnet/dotnet
else
  printf '%s\n' 'Install .NET SDK 8 to run the gameplay core checks.' >&2
  exit 1
fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_CLI_HOME="${TMPDIR:-/tmp}/city-dino-dotnet-home"
export NUGET_PACKAGES="${TMPDIR:-/tmp}/city-dino-nuget"
# Named .NET mutexes need a writable per-user runtime directory on Linux.
if [[ -z "${XDG_RUNTIME_DIR:-}" ]]; then
  export XDG_RUNTIME_DIR="${TMPDIR:-/tmp}/city-dino-runtime-${UID}"
  mkdir -p "$XDG_RUNTIME_DIR"
  chmod 700 "$XDG_RUNTIME_DIR"
fi
export XDG_DATA_HOME="${XDG_DATA_HOME:-${TMPDIR:-/tmp}/city-dino-xdg-data}"
if [[ "${1:-}" == "--compile-unity" ]]; then
  printf '%s\n' 'Compile-only Unity API stub check; this does not run the Unity Editor.'
  exec "$dotnet_command" build "$repo_dir/Tests/UnityCompile/UnityCompile.csproj" --configuration Release --nologo
fi
exec "$dotnet_command" run --project "$repo_dir/Tests/RunnerCore.Tests.csproj" --configuration Release -- "$@"
