# CLAUDE.md

Guidance for Claude Code (or Claude in general) working in this repository.

## What this is

VRC-OSC-Handy is a Windows WPF (.NET Framework 4.8.1) desktop app that bridges
Spotify, VoiceMeeter, and Whisper speech-to-text into VRChat via OSC. Single
project (`VRC-OSC-Handy.csproj`) under `VRC-OSC-Handy/`. There is no test
project, no CI, and the build only works on Windows (WPF, CefSharp, native
VoiceMeeter/CUDA interop) — it cannot be built or run in a Linux sandbox.

## Build

- Open `VRC-OSC-Handy.sln` in Visual Studio 2022, restore NuGet packages
  (`packages.config`, old-style `packages/` restore, not PackageReference/SDK-style).
- Build config: Release, platform x64 (required for CefSharp + Whisper CUDA).
- No `dotnet build` support — this is classic .NET Framework with a
  `packages.config`-based csproj, not SDK-style.

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
  This has **not** been changed here because it's unverified whether the
  underlying `a-tg.VmrapiDynWrap` wrapper throws or fails silently when
  VoiceMeeter isn't installed — verify against the actual library before
  touching this, since a partial null-check pass across 100+ call sites is
  easy to get wrong.
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

## NuGet packages (2026 refresh)

`packages.config` and the matching `HintPath`/`Import`/`Error` paths in the
`.csproj` were bumped to latest at the time: NAudio family → 2.3.0,
SpotifyAPI.Web/.Auth → 7.4.2, Whisper.net family → 1.9.1. `BuildSoft.OscCore`,
`VRCOscLib`, `EmbedIO`, `Unosquare.Swan.Lite`, `Newtonsoft.Json`, `log4net`,
and the `a-tg.*` packages were already at their latest published version.
The `CefSharp.Common` / `CefSharp.Wpf` / `chromiumembeddedframework.runtime.win-x64`
/ `win-x86` quartet was deliberately left alone: these four must stay on a
matching exact CEF build, packages.config has no transitive resolution, and
hand-picking four version strings that don't actually match breaks the native
CEF runtime load at startup. Bump that quartet from Visual Studio's NuGet UI
(or `Update-Package -reinstall`) so NuGet's own resolver keeps them in lockstep.

If a restore reports a missing package version that isn't in `packages.config`
at all (a transitive dependency, possibly one of the native/obfuscation-runtime
packages some of these libraries pull in), do a full clean restore rather than
hand-placing files: delete `VRC-OSC-Handy/bin`, `VRC-OSC-Handy/obj`, and the
solution's `packages/` folder, clear NuGet's HTTP cache
(`nuget locals http-cache -clear`), then restore again.

If you touch any of the above again, keep the fix local — this codebase
favors small, targeted patches over refactors (see `AGENTS.md` for the same
guidance framed for autonomous agents).
