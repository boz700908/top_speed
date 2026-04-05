# Build Scripts

This folder contains repo-local build helpers for native dependencies that are shipped with the game.

## `Build-WindowsNativeDeps.ps1`

Rebuilds and stages the Windows native DLLs that are maintained from source in or alongside this repository.

Current targets:
- `MiniAudioEx` -> `MiniAudioExNET/runtimes/win-x64/native/miniaudioex.dll`

### Requirements

- PowerShell
- CMake
- Ninja
- a working MSVC x64 toolchain

### Usage

```powershell
powershell -ExecutionPolicy Bypass -File .\top_speed_net\scripts\Build-WindowsNativeDeps.ps1 `
  -MsvcRoot <path-to-msvc-root> `
  -MiniAudioExSource <path-to-miniaudioex-source>
```

### Parameters

- `-MsvcRoot`
  - required
  - path containing `setup_x64.bat`
- `-MiniAudioExSource`
  - required unless `-SkipMiniAudioEx` is used
  - path to the external `miniaudioex` source tree
- `-Configuration`
  - optional
  - default: `Release`
- `-SkipMiniAudioEx`
  - optional
  - skip `MiniAudioEx`

### Notes

- The script stages rebuilt DLLs directly into the repo locations used by the client build.
