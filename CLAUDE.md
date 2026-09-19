# CLAUDE.md

Guidance for Claude Code (or Claude in general) working in this repository.

## What this is

VRC-OSC-Handy is a Windows WPF (.NET 10) desktop app that bridges Spotify,
VoiceMeeter, and Whisper speech-to-text into VRChat via OSC. The single SDK-style
project (`VRC-OSC-Handy.csproj`) is under `VRC-OSC-Handy/`. There is no test
project or CI. The build/runtime target is Windows x64 because of WPF, CefSharp,
VoiceMeeter native API access, and Whisper CUDA runtimes.

## Build

- Open `VRC-OSC-Handy.sln` in Visual Studio 2026 with the .NET desktop
  workload, restore NuGet packages, and build the SDK-style project.
- Build config: Release, platform x64 (required for the native CefSharp and
  Whisper/VoiceMeeter components).
- Command-line build is supported by the modern SDK project; on Windows use:
  `dotnet restore VRC-OSC-Handy.sln` and
  `dotnet build VRC-OSC-Handy.sln -c Release -p:Platform=x64`.
- A Linux sandbox still cannot execute or functionally test the Windows app;
  use Windows for the final build/runtime validation.

## Architecture (read before changing behavior)

- `MainWindow.xaml.cs` (~2800 lines) is the god object: owns config, all
  background loops, and hand-built VoiceMeeter strip UI (5 nearly-identical
  code blocks for Strip0..Strip4). Most business logic lives here rather
  than in the per-feature folders.
- Each integration runs its own polling loop on a background `Task` (no
  shared scheduler): `Update/updateSpotify.cs`, `Update/updateName.cs`,
  `Update/updateProgess.cs` (Spotify), `VoiceMeeter/RemoteControle.cs`
  (VoiceMeeter dirty-parameter polling), `Osc/VRCOSC.cs` (chatbox +
  avatar-parameter dispatch), `Wisper/Wisper.cs` + `NAudio/MicrophoneCapture.cs`
  (Whisper STT). All of them marshal UI updates back via
  `Application.Current.Dispatcher.Invoke`.
- Config: `Config/InterfaceConfig.cs` (`config.json`) and
  `Config/VRCParameterConfig.cs` (`vrc_config.json`), both POCOs deserialized
  from JSON stored at `%AppData%/VRC Handy/`. On first run, `MainWindow.genConfig`
  copies bundled defaults from `resource/config.json` / `resource/vrc_config.json`
  (embedded resources) and thereafter merges in any *new* keys added to the
  bundled defaults on every launch (additive migration, does not remove keys).
- `CrashHandler/CrashHandler.cs` wires global exception handlers
  (Dispatcher, AppDomain, TaskScheduler) that call the static
  `MainWindow.stopAll()` / `MainWindow.saveAll()` before optionally
  restarting. Any new background loop should be added to `stopAll()` or it
  won't be torn down on crash/exit.

## Known design constraints — don't "fix" without understanding why

- `MainWindow.remoteControle`, `spotify`, `wisper`, `config` etc. are static
  fields shared across the whole app (not DI). This is intentional given the
  app's size; don't introduce a DI container for a one-window app.
- `RemoteControle` (VoiceMeeter) is constructed unconditionally in
  `MainWindow`'s constructor and dozens of call sites in `MainWindow.xaml.cs`
  call `remoteControle.getBoolParameter(...)` etc. with no null checks. The
  README calls VoiceMeeter "optional", but the C# assumes it's always present.
  The legacy `a-tg.*` NuGet wrappers are now replaced by the in-tree native
  wrapper in `VoiceMeeter/`, but the optional-installation behavior is still
  unverified. A partial null-check pass across the many call sites would be a
  separate deliberate change.
- The `Strip0`..`Strip4` config classes (`Config/VRCParameterConfig.cs`) and
  the five near-identical UI-generation blocks in `MainWindow.xaml.cs` are
  duplicated on purpose to match VoiceMeeter's fixed 5-strip layout and the
  existing `vrc_config.json` schema on disk. Collapsing them into a
  `List<Strip>` would be a breaking config-format change — do it deliberately
  with a migration, not as a drive-by refactor.

## Conventions already in the codebase

- Background loops use `CancellationTokenSource` + `Task.Run` + a manual
  `while (!ct.IsCancellationRequested) { ...; Thread.Sleep(N)/await Task.Delay(N); }`
  pattern (no `PeriodicTimer`/`System.Reactive`). Match this pattern for new
  loops rather than introducing a new one.
- UI writes from background threads always go through
  `control.Dispatcher.CheckAccess()` / `.Invoke(...)`, or the
  `Application.Current.Dispatcher.Invoke` equivalent for static/global state.
- Logging goes through `Logger/DebugLogger.cs` (`Log`/`LogWarning`/`LogError`),
  which writes to `Logs/log_<date>.txt` next to the executable — use it
  instead of `Console.WriteLine`/`Debug.WriteLine` directly.

## Gotchas fixed here (see git history / diff)

- `SpotifyAuth.runAuth()` used to block the UI thread with
  `Auth().GetAwaiter().GetResult()` while `Auth()` itself awaited
  UI-thread-marshaled work — a guaranteed deadlock on every Spotify login
  (startup and the "Connect Spotify" button). Now fire-and-forget.
- Spotify's `CurrentlyPlayingContext.Item` is `null` during ads and private
  sessions; several polling loops dereferenced it unconditionally. Now
  guarded with `track?.Item != null`.
- `ByteImageConverter.ByteToImage` left its `MemoryStream` open indefinitely
  (required by `BitmapImage` unless `CacheOption = OnLoad`) and returned an
  unfrozen `ImageSource` used from background threads. Fixed and frozen.
- Whisper model downloads (`Wisper.DownloadModel`) didn't dispose the
  download stream or the file handle.
- `MicrophoneCapture.WaveIn_DataAvailable` is `async void`; if Whisper
  inference takes longer than the 5s chunk interval, the next chunk started
  processing concurrently on the same non-reentrant `WhisperProcessor`. Now
  guarded to drop overlapping chunks instead of racing.
- `RemoteControle.LogOut()` is now idempotent (`_loggedOut` guard), and its
  constructor registers `AppDomain.ProcessExit` and `SystemEvents.SessionEnding`
  so the VoiceMeeter Remote API slot is released on exit paths that skip
  `Window_Closing` (`Environment.Exit`, logoff/shutdown). **This is best-effort
  only**: a hard kill (Task Manager "End Process", `taskkill /F`, a native
  crash, power loss) tears the process down before any more managed code can
  run, so no in-process fix can release the slot in that case — VoiceMeeter
  itself must be restarted to reclaim it. Don't present this as fully solving
  the leaked-slot issue; it narrows it to genuinely unrecoverable OS-level kills.

## NuGet packages

Direct dependencies are declared with SDK-style `<PackageReference>` entries in
`VRC-OSC-Handy/VRC-OSC-Handy.csproj`.

The current direct set includes NAudio 2.4.0, SpotifyAPI.Web/Auth 7.4.2,
Whisper.net 1.9.1 with Windows CPU/CUDA/CUDA12 runtimes, Microsoft.Win32.Registry 5.0.0, Microsoft.Win32.SystemEvents 10.0.12, BuildSoft.OscCore,
VRCOscLib, EmbedIO, Unosquare.Swan.Lite, Newtonsoft.Json, log4net, and
CefSharp.Wpf.NETCore 152.0.60. The old Framework-only `a-tg.*` VoiceMeeter
packages have been replaced by a small in-tree wrapper under `VoiceMeeter/`.

Do not reintroduce `packages.config`, manual `HintPath` entries, or old-style
CefSharp package references. For a clean restore, remove `bin/` and `obj/` and
run `dotnet restore` on Windows.

If you touch any of the above again, keep the fix local — this codebase
favors small, targeted patches over refactors (see `AGENTS.md` for the same
guidance framed for autonomous agents).
