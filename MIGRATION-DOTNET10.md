# .NET 10 migration

VRC-OSC-Handy has been converted from the legacy `.NET Framework 4.8.1`
project format to an SDK-style WPF application targeting `.NET 10`
(`net10.0-windows`) and Windows x64.

## Project format

- `VRC-OSC-Handy.csproj` is now SDK-style.
- `packages.config` has been removed.
- NuGet dependencies use `<PackageReference>`.
- The old manual `HintPath` references to Framework/`packages/` assemblies are gone.
- The obsolete `App.config` binding redirects were removed; modern .NET uses the
  generated runtime configuration instead.
- The unused empty `Properties/Settings.settings` and generated Settings class
  were removed.
- The legacy post-build `robocopy` step that moved runtime files into `lib/`
  was removed. Modern .NET runtime/dependency metadata must remain with the
  application output.

## Native dependencies

The application remains Windows x64 because of CefSharp, VoiceMeeter, and
Whisper native components.

### CefSharp

The project now references `CefSharp.Wpf.NETCore` 152.0.60 instead of the old
`.NET Framework` WPF package. Matching Chromium runtime assets are pulled by
NuGet.

### Whisper

The project keeps Whisper.net 1.9.1 but only restores the runtimes relevant to
this application:

- `Whisper.net.Runtime`
- `Whisper.net.Runtime.Cuda.Windows`
- `Whisper.net.Runtime.Cuda12.Windows`

The old all-runtimes, Linux, macOS, CoreML, Metal, OpenVINO, Vulkan, and NoAvx
packages are intentionally not direct dependencies of this Windows x64 app.

### VoiceMeeter

The Framework-only `a-tg.VmrapiDynWrap` and `a-tg.UnmanagedLibWrap` packages
were removed. `VoiceMeeter/VoiceMeeterRemoteApi.cs` implements the subset of the
VoiceMeeter Remote API currently used by the application and dynamically loads
the installed `VoicemeeterRemote64.dll` through `System.Runtime.InteropServices.NativeLibrary`.

`VoiceMeeter/VoiceMeeterPathHelper.cs` retains the previous installation-path
and registry lookup behavior and the existing `AtgDev.Voicemeeter` namespaces,
so the rest of the application does not need a broad API rewrite.

## Build in Visual Studio 2026

Install the **.NET desktop development** workload with the .NET 10 SDK/component,
then open `VRC-OSC-Handy.sln`.

Use **Release / x64** for the normal application build.

Command line, from a Windows terminal:

```powershell
dotnet restore VRC-OSC-Handy.sln
dotnet build VRC-OSC-Handy.sln -c Release -p:Platform=x64
```

## Validation note

This conversion was performed in a non-Windows environment. The project files,
package references, Framework-specific source references, and migration docs
were statically checked here, but the WPF application and native Windows
runtime dependencies require a Windows machine for the final restore/build/run
validation.
