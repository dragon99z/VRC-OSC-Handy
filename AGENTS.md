# AGENTS.md

Instructions for autonomous coding agents (Codex, Claude Code, Copilot
Workspace, etc.) operating on this repository. See `CLAUDE.md` for a deeper
architectural walkthrough; this file is the quick operational contract.

## Environment reality check

- Windows-only WPF app targeting .NET 10 (`net10.0-windows`) and x64. CefSharp,
  VoiceMeeter, and Whisper use native Windows components, so the application is
  not a cross-platform target. A Windows machine with the .NET 10 SDK / Visual
  Studio 2026 is the canonical build environment.
- A test project already exists (`VRC-OSC-Handy.Tests/`, xunit, wired into
  `VRC-OSC-Handy.sln`), but it only covers pure helper methods (`VRCOSC`'s
  math/formatting helpers, `MicrophoneCapture`'s language dictionaries) —
  nothing that touches OSC/Spotify/VoiceMeeter/Whisper. Add tests there when
  you touch similar pure logic; don't invent a *second* test project, and
  don't try to unit-test the hardware/network-facing integrations. There's
  still no CI to run it automatically.
- Restore is via SDK-style `<PackageReference>` entries in
  `VRC-OSC-Handy/VRC-OSC-Handy.csproj`. Do not reintroduce `packages.config` or
  the old Framework-style project.

## Scope discipline

- One project, one window, mostly static state by design (see `CLAUDE.md`).
  Don't introduce DI containers, MVVM frameworks, or a message bus to fix a
  small bug — match the existing imperative style.
- `MainWindow.xaml.cs` used to be one large, repetitive file (5 duplicated
  VoiceMeeter strip UI blocks mirroring a fixed hardware layout, plus 3
  duplicated parameter-popup methods). Both have since been consolidated into
  single parametrized implementations — see `CLAUDE.md`'s Architecture
  section — and the file is now split across several `partial class
  MainWindow` files by responsibility. Add new code to the file it belongs
  with rather than growing one file back into a god object, and don't
  reintroduce a hand-copied per-strip or per-panel block where a single
  parametrized one now exists.
- The `vrc_config.json` schema on disk still has five separately-named
  `Strip0`..`Strip4` keys — that hasn't changed and *is* still a real
  constraint. Don't collapse `Config/VRCParameterConfig.cs`'s five classes
  into a `List<Strip>`/dictionary without a deliberate migration, even though
  they now share a `Strip` base class internally (safe because the project
  serializes with no `TypeNameHandling` configured, so inheritance doesn't
  change the JSON shape).
- Prefer the smallest diff that fixes the actual defect. This repo has a lot
  of copy-pasted background-polling loops; fix the one you're asked about,
  and only apply the same fix elsewhere if the same bug is demonstrably
  present there too (state clearly which files you changed and why).

## Before changing a background loop or static field

1. Grep for every reader/writer of the static (`MainWindow.spotify`,
   `MainWindow.remoteControle`, `MainWindow.wisper`, `MainWindow.config`,
   etc.) — they're read from multiple background threads via
   `Application.Current.Dispatcher.Invoke`, so a change in one place can
   silently break another loop.
2. Any new long-running loop must be cancellable via a
   `CancellationTokenSource` and torn down in `MainWindow.stopAll()`, or it
   will leak past app shutdown/crash.
3. Any UI mutation from a non-UI thread must go through
   `Dispatcher.CheckAccess()`/`Invoke` — this is not optional, WPF will throw.

## Known unresolved gap (do not silently "fix")

`RemoteControle` (VoiceMeeter) is constructed unconditionally and used
without null checks throughout `MainWindow`'s code (split across several
`MainWindow.*.cs` partial-class files), even though the README describes
VoiceMeeter as optional. The legacy `a-tg.*` NuGet wrappers are no longer
used; the modern project contains a small native wrapper in
`VoiceMeeter/VoiceMeeterRemoteApi.cs`. The optional-installation behavior is
still not guaranteed, because the UI assumes the VoiceMeeter backend is
available. If asked to make VoiceMeeter truly optional, treat it as a larger
change (null-guards plus UI changes), not a quick dependency fix.

## Already-fixed issues (don't reintroduce)

- `SpotifyAuth.runAuth()` must not call `.GetAwaiter().GetResult()` on a task
  that awaits UI-thread-marshaled work — it deadlocks the UI thread. Keep it
  fire-and-forget (`_ = Auth();`).
- Spotify's `track.Item` can be `null` (ads/private sessions) — always check
  `track?.Item != null` before switching on `track.Item.Type`.
- `ByteImageConverter.ByteToImage` must set `BitmapCacheOption.OnLoad` before
  `EndInit()` if the backing stream is going to be disposed, and should
  `Freeze()` the result since it's used from background threads.
- File/network streams (`Wisper.DownloadModel`, etc.) must be wrapped in
  `using` — several were previously left open.
- `MicrophoneCapture`'s Whisper `WhisperProcessor` is not safe for concurrent
  `ProcessAsync` calls; don't remove the `isProcessingChunk` guard in
  `WaveIn_DataAvailable`/`ProcessChunkAsync`.
- `RemoteControle.LogOut()` is idempotent and its constructor hooks
  `AppDomain.ProcessExit`/`SystemEvents.SessionEnding` to release the
  VoiceMeeter Remote API login slot on exit paths other than
  `Window_Closing`. Don't remove these hooks, and don't claim they fix a hard
  kill (Task Manager "End Process", `taskkill /F`, a native crash, power
  loss) — nothing in-process can run once the OS has torn the process down,
  so that case is a genuine, undocumented-by-us VoiceMeeter limitation, not
  something a code change here can close.
- The per-strip VoiceMeeter UI is built by `BuildVmStrip` in
  `MainWindow.VoiceMeeterPanel.cs` from the `vmEditionLayouts`/
  `vmStripRightMargins` tables, not by hand-copying a block per strip.
  Hand-copied per-strip blocks are exactly what caused three real bugs
  before this was consolidated: Strip 0's gain readout always showed
  Strip 1's value, Strip 1's buttons never reflected real state in
  "Standard" mode, and Strip 2's buttons showed Strip 1's initial state in
  "Potato" mode. Don't reintroduce hand-copied per-strip code.
- The three parameter-popup methods (`setVRCParameterMV`/`Spotify`/`Other`)
  are consolidated into `ShowParameterEditor` in
  `MainWindow.ParameterEditor.cs`, parametrized on which `Panel` to use. The
  old copies had a real bug where two of the three used `Other_Controller`'s
  bounds for their own popup's overflow math. Don't reintroduce separate
  per-panel copies of this method.
- `VoiceMeeterPathHelper.GetProgramFolder()`'s registry lookup is a loop over
  a `registryPaths` array, not two copy-pasted lookups — the old version's
  final "not found" exception only ever named the *second* path it checked,
  because both lookups reused the same local variable. Keep the loop so the
  error message (and any future third fallback path) stays accurate.
- `MicrophoneCapture.LANGUAGES`/`TO_LANGUAGE_CODE` now live in
  `NAudio/WhisperLanguages.cs`; `MicrophoneCapture` just forwards to them so
  existing call sites (including the test project) keep compiling.
  `TO_LANGUAGE_CODE` is derived from `LANGUAGES` plus a short synonym list,
  not hand-duplicated — don't hand-write a second reverse dictionary that can
  drift out of sync.
- `CrashHandler.cs`'s three unhandled-exception handlers
  (`OnDispatcherUnhandledException`/`OnDomainUnhandledException`/
  `OnTaskSchedulerUnobservedTaskException`) share one `HandleFatalException`
  (log, notify, stop, save, maybe restart). Don't re-duplicate that sequence
  per handler.

## NuGet packages

This is now an SDK-style project using `<PackageReference>` in
`VRC-OSC-Handy/VRC-OSC-Handy.csproj`. Keep direct dependency versions pinned
there and let NuGet resolve transitive dependencies.

- CefSharp uses `CefSharp.Wpf.NETCore` 152.0.60 rather than the old
  `.NET Framework` `CefSharp.Wpf` package.
- Whisper uses the CPU runtime plus the Windows CUDA and CUDA 12 Windows
  runtime packages. Cross-platform runtimes are intentionally not restored by
  this Windows x64 application.
- The legacy `a-tg.VmrapiDynWrap` / `a-tg.UnmanagedLibWrap` packages are not
  used. Their required subset of the VoiceMeeter Remote API is implemented in
  source under `VoiceMeeter/` using `NativeLibrary`.
- There is no `packages/` directory or `packages.config` restore step — the
  SDK-style project restores via `<PackageReference>` only. A stale
  `VRC-OSC-Handy/packages.config` file is still sitting in the repo from
  before the migration; it isn't read by the build, but delete it rather
  than treating its presence as license to add Framework-style references
  back.

When changing dependencies, prefer direct PackageReference changes and then
perform a clean NuGet restore. Do not add back Framework-only reference paths
or package imports from the old project format.

## Commit hygiene

- Keep C# and doc changes in separate, clearly described commits/PRs where
  practical.
- Update `CLAUDE.md`/this file if you change an architectural assumption
  described here (e.g., if you do make VoiceMeeter truly optional).
