# CLAUDE.md

Guidance for Claude Code (or Claude in general) working in this repository.

## What this is

VRC-OSC-Handy is a Windows WPF (.NET 10) desktop app that bridges Spotify,
VoiceMeeter, and Whisper speech-to-text into VRChat via OSC. The SDK-style app
project (`VRC-OSC-Handy.csproj`) is under `VRC-OSC-Handy/`; a small xunit test
project (`VRC-OSC-Handy.Tests/`) sits alongside it, covering only the pure
helper methods (`VRCOSC.TranslateValue`/`ReverseTranslateValue`/
`GenerateProgressBar`, `MicrophoneCapture.LANGUAGES`/`TO_LANGUAGE_CODE`) — see
"Tests" below. There is still no CI. The build/runtime target is Windows x64
because of WPF, CefSharp, VoiceMeeter native API access, and Whisper CUDA
runtimes.

`MainWindow` used to be one ~2800-line `MainWindow.xaml.cs` file; it's now
split by responsibility across several sibling `partial class MainWindow`
files (`MainWindow.ConfigIO.cs`, `MainWindow.VoiceMeeterPanel.cs`,
`MainWindow.ParameterEditor.cs`, `MainWindow.SpotifySettings.cs`,
`MainWindow.SttSettings.cs`) — see "Architecture" below for what lives where.

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

## Tests

- `VRC-OSC-Handy.Tests/` (xunit) is included in `VRC-OSC-Handy.sln` and builds
  against the same `net10.0-windows`/x64 target as the app (a test project
  can't target a plain non-`-windows` TFM while referencing a
  `net10.0-windows` project). Run it on Windows with `dotnet test
  VRC-OSC-Handy.sln -p:Platform=x64`.
- Coverage is intentionally narrow: `VRCOSCTests.cs` exercises `VRCOSC`'s pure
  math/formatting helpers, and `LanguageMappingTests.cs` checks the
  `MicrophoneCapture.LANGUAGES`/`TO_LANGUAGE_CODE` dictionaries stay
  consistent. Nothing that touches OSC sockets, Spotify, VoiceMeeter, or
  Whisper is under test — those need real hardware/services. Follow this
  pattern (pure-function unit tests only) rather than trying to mock the
  integrations.
- Still no CI wired up to run this project automatically.

## Architecture (read before changing behavior)

- `MainWindow.xaml.cs` (~160 lines) now only holds shared static state
  (`spotify`, `remoteControle`, `config`, etc.), the constructor, and window
  chrome (drag/close/minimize, particle-canvas rendering). What used to be
  ~2800 lines of mostly-duplicated code in that one file is split across
  sibling `partial class MainWindow` files by responsibility — all in the
  same namespace, so this is a physical split only, not a design change:
  - `MainWindow.ConfigIO.cs` — `genConfig`, `stopAll`, `saveAll`.
  - `MainWindow.VoiceMeeterPanel.cs` — the VoiceMeeter strip-panel builder
    (`LoadVMSettings`/`BuildVmStrip`, driven by the `vmEditionLayouts`/
    `vmStripRightMargins` tables instead of 5 hand-copied per-strip UI
    blocks) plus its event handlers (`VM_Controller_Loaded`,
    `vmValueChange`, `vmToggle`, `GetByUid`).
  - `MainWindow.ParameterEditor.cs` — the right-click "edit this OSC
    parameter path" popup shared by the VM/Spotify/Other panels
    (`ShowParameterEditor`/`HideParameterEditor`, parametrized on which
    `Panel` to use instead of three hand-copied methods).
  - `MainWindow.SpotifySettings.cs` — playback buttons and the client
    ID/secret settings UI.
  - `MainWindow.SttSettings.cs` — Whisper model/language pickers, the
    translate checkbox, and the clock.
  Add new MainWindow-owned UI logic to whichever of these files it belongs
  with, or start a new `MainWindow.<Area>.cs` file for a genuinely new area,
  rather than growing `MainWindow.xaml.cs` back into a god object.
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
  `MainWindow`'s constructor, and dozens of call sites across the
  `MainWindow.*.cs` partial-class files (mostly
  `MainWindow.VoiceMeeterPanel.cs`) call `remoteControle.getBoolParameter(...)`
  etc. with no null checks. The README calls VoiceMeeter "optional", but the
  C# assumes it's always present. The legacy `a-tg.*` NuGet wrappers are now
  replaced by the in-tree native wrapper in `VoiceMeeter/`, but the
  optional-installation behavior is still unverified. A partial null-check
  pass across the many call sites would be a separate deliberate change.
- The `Strip0`..`Strip4` config classes (`Config/VRCParameterConfig.cs`) are
  still five separate C# classes — not a `List<Strip>` — because
  `vrc_config.json` on disk has five separately-named top-level keys
  (`"Strip0": {...}`, `"Strip1": {...}`, ...) and collapsing that into an
  array would be a breaking config-format change. They no longer duplicate
  their field list by hand: all five now derive from one `Strip` base class
  declared in the same file. This is safe because the project serializes
  with plain `JsonConvert.SerializeObject`/`ToObject<T>` and no
  `TypeNameHandling` configured, so inheritance doesn't add `$type` metadata
  or otherwise change the JSON shape. Keep sharing fields via the base
  class, but don't collapse the five classes themselves without a
  deliberate migration.
- The matching UI-generation code in `MainWindow.VoiceMeeterPanel.cs` is
  likewise no longer duplicated per strip: `BuildVmStrip` builds one strip
  from the `vmEditionLayouts`/`vmStripRightMargins` tables. If you add a
  field to the `Strip` base class, wire its UI into `BuildVmStrip` rather
  than hand-copying a new per-strip block.

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
- The old `LoadVMSettings` hand-copied the same ~120 lines of control setup
  once per strip per VoiceMeeter edition (10 copies total). Three real bugs
  came out of that duplication and are now fixed by construction in
  `BuildVmStrip` (`MainWindow.VoiceMeeterPanel.cs`), which computes every
  strip's Uid from its actual index instead of a hand-typed literal:
  - Strip 0's gain-value label always read `Strip[1].Gain` in all three
    editions (display only; the slider itself was correct).
  - In "Standard" mode, Strip 1's A1/B1/Mute buttons were hardcoded green and
    never reflected real VoiceMeeter state.
  - In "Potato" mode, Strip 2's A1–A5 buttons showed Strip 1's on/off state
    on load (right-click editing was unaffected).
- `setVRCParameterMV` and `setVRCParameterSpotify` both computed their
  popup's overflow bounds from `Other_Controller.ActualWidth`/`ActualHeight`
  instead of their own panel's, copied from the third method
  (`setVRCParameterOther`). Fixed by consolidating all three into
  `ShowParameterEditor` (`MainWindow.ParameterEditor.cs`), which takes the
  panel as a parameter.
- `VoiceMeeterPathHelper.GetProgramFolder()`'s final "VoiceMeeter not found"
  exception used to only name the second (`Wow6432Node`) registry path it
  checked, because both lookups reused the same local variable. It now loops
  over an explicit `registryPaths` array and reports all of them.
- `MicrophoneCapture.TO_LANGUAGE_CODE` was a hand-written ~110-line reverse
  of `LANGUAGES` that could silently drift out of sync. Both dictionaries now
  live in `NAudio/WhisperLanguages.cs`; `TO_LANGUAGE_CODE` is computed from
  `LANGUAGES` plus a dozen genuine synonyms, and `MicrophoneCapture` exposes
  both as forwarding properties so existing call sites don't change.
- `CrashHandler.cs`'s three unhandled-exception handlers each duplicated the
  same log/notify/stop/save/restart sequence. Consolidated into one
  `HandleFatalException` helper.

## NuGet packages

Direct dependencies are declared with SDK-style `<PackageReference>` entries in
`VRC-OSC-Handy/VRC-OSC-Handy.csproj`.

The current direct set (see the .csproj for exact pins) includes
BuildSoft.OscCore 1.2.1.1, CefSharp.Wpf.NETCore 152.0.60, EmbedIO 3.5.2,
log4net 3.4.0, NAudio 2.4.0, Newtonsoft.Json 13.0.4, SpotifyAPI.Web/Auth
7.4.2, Unosquare.Swan.Lite 3.1.0, VRCOscLib 1.6.0, and Whisper.net 1.9.1 with
its Windows CPU/CUDA/CUDA12 runtime packages. The old Framework-only `a-tg.*`
VoiceMeeter packages have been replaced by a small in-tree wrapper under
`VoiceMeeter/`.

`RemoteControle.cs` uses `Microsoft.Win32.SystemEvents` and
`VoiceMeeterPathHelper.cs` uses `Microsoft.Win32.Registry`, but neither needs
a `<PackageReference>`: for a `net10.0-windows` WPF app these come from the
Windows Desktop shared framework, not NuGet. Don't add explicit package
references for them.

Do not reintroduce `packages.config`, manual `HintPath` entries, or old-style
CefSharp package references. For a clean restore, remove `bin/` and `obj/` and
run `dotnet restore` on Windows.

If you touch any of the above again, keep the fix local — this codebase
favors small, targeted patches over refactors (see `AGENTS.md` for the same
guidance framed for autonomous agents).
