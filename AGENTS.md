# AGENTS.md

Instructions for autonomous coding agents (Codex, Claude Code, Copilot
Workspace, etc.) operating on this repository. See `CLAUDE.md` for a deeper
architectural walkthrough; this file is the quick operational contract.

## Environment reality check

- Windows-only WPF app targeting .NET 10 (`net10.0-windows`) and x64. CefSharp,
  VoiceMeeter, and Whisper use native Windows components, so the application is
  not a cross-platform target. A Windows machine with the .NET 10 SDK / Visual
  Studio 2026 is the canonical build environment.
- No unit tests exist. Don't invent a test project unless asked; there's
  nothing to wire it into and no CI to run.
- Restore is via SDK-style `<PackageReference>` entries in
  `VRC-OSC-Handy/VRC-OSC-Handy.csproj`. Do not reintroduce `packages.config` or
  the old Framework-style project.

## Scope discipline

- One project, one window, mostly static state by design (see `CLAUDE.md`).
  Don't introduce DI containers, MVVM frameworks, or a message bus to fix a
  small bug — match the existing imperative style.
- `MainWindow.xaml.cs` is large and repetitive on purpose (5 duplicated
  VoiceMeeter strip blocks mirroring a fixed hardware layout). Don't collapse
  it into a loop/generic version unless explicitly asked — the `vrc_config.json`
  schema on disk depends on the current `Strip0..Strip4` field names.
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
without null checks throughout `MainWindow.xaml.cs`, even though the README
describes VoiceMeeter as optional. The legacy `a-tg.*` NuGet wrappers are no
longer used; the modern project contains a small native wrapper in
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
- There is no `packages/` directory or `packages.config` restore step.

When changing dependencies, prefer direct PackageReference changes and then
perform a clean NuGet restore. Do not add back Framework-only reference paths
or package imports from the old project format.

## Commit hygiene

- Keep C# and doc changes in separate, clearly described commits/PRs where
  practical.
- Update `CLAUDE.md`/this file if you change an architectural assumption
  described here (e.g., if you do make VoiceMeeter truly optional).
