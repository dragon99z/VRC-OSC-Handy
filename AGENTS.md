# AGENTS.md

Instructions for autonomous coding agents (Codex, Claude Code, Copilot
Workspace, etc.) operating on this repository. See `CLAUDE.md` for a deeper
architectural walkthrough; this file is the quick operational contract.

## Environment reality check

- Windows-only WPF app (.NET Framework 4.8.1). CefSharp, VoiceMeeter's native
  wrapper, and Whisper's CUDA runtime mean this **cannot be built or run on
  Linux/macOS CI**. If you're in a non-Windows sandbox, do not attempt
  `dotnet build`/`msbuild` — validate changes by reading the code carefully
  and reasoning about control flow instead of compiling.
- No unit tests exist. Don't invent a test project unless asked; there's
  nothing to wire it into and no CI to run it.
- Restore is via `packages.config`, not `<PackageReference>`. Don't "modernize"
  the csproj to SDK-style as a side effect of an unrelated task.

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
describes VoiceMeeter as optional. Whether this is a real bug depends on
whether `a-tg.VmrapiDynWrap`'s `Login()`/`GetParameter()` throw or fail
silently when VoiceMeeter isn't installed — that behavior isn't verifiable
from this source tree alone. If asked to make VoiceMeeter truly optional,
say so explicitly as a larger, deliberate change (needs null-guards at
~100 call sites plus UI changes to hide the VoiceMeeter tab), not a quick fix.

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

`packages.config` versions are pinned deliberately. If asked to update
packages: bump each package's version string in both `packages.config` and
every matching `HintPath`/`Import`/`Error` path in the `.csproj` (they must
stay in sync since this is a packages.config, not PackageReference, project).
Exception: never hand-pick versions for `CefSharp.Common`, `CefSharp.Wpf`,
`chromiumembeddedframework.runtime.win-x64`, and
`chromiumembeddedframework.runtime.win-x86` independently — they must resolve
to one matching CEF build, and packages.config restore does no transitive
resolution to catch a mismatch for you. Leave that quartet for the person to
update via Visual Studio's NuGet UI, and say so explicitly rather than
guessing four version numbers that might not actually correspond to the same
CEF binary.

## Commit hygiene

- Keep C# and doc changes in separate, clearly described commits/PRs where
  practical.
- Update `CLAUDE.md`/this file if you change an architectural assumption
  described here (e.g., if you do make VoiceMeeter truly optional).
