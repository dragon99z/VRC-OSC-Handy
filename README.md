# VRC-OSC-Handy

> A Windows desktop companion app that bridges **Spotify**, **VoiceMeeter**, and **Whisper Speech-to-Text** directly into VRChat via the OSC protocol.

[![Platform](https://img.shields.io/badge/platform-Windows-blue?logo=windows)](https://github.com/dragon99z/VRC-OSC-Handy)
[![Framework](https://img.shields.io/badge/.NET-10%20LTS-purple)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/language-C%23-green?logo=csharp)](https://github.com/dragon99z/VRC-OSC-Handy)
[![License](https://img.shields.io/github/license/dragon99z/VRC-OSC-Handy)](https://github.com/dragon99z/VRC-OSC-Handy)
[![DeepWiki](https://img.shields.io/badge/docs-DeepWiki-orange)](https://deepwiki.com/dragon99z/VRC-OSC-Handy)

---

## Overview

**VRC-OSC-Handy** is a Windows WPF application designed to enhance the VRChat experience by connecting external services to the game via **Open Sound Control (OSC)**. It acts as a central hub where multiple background services feed real-time data into a unified OSC engine, letting you:

- 🎵 Display your currently playing Spotify track (with a live progress bar) in the VRChat chatbox
- 🎛️ Control VoiceMeeter gains, mutes, and bus assignments directly from the companion UI
- 🎤 Transcribe your microphone speech using Whisper AI and inject it into the VRChat chatbox in real-time

---

## Features

### 🎵 Spotify Integration
- OAuth2 authentication via an embedded CefSharp browser window
- High-frequency polling of the Spotify playback API to keep the chatbox progress bar in sync
- Formats track name, artist, and progress information for the VRChat chatbox display

### 🎛️ VoiceMeeter Integration
- Uses a small native VoiceMeeter Remote API wrapper to dynamically generate UI controls in the main window
- Supports gain sliders, mute toggles, and bus assignments
- Polls VoiceMeeter for "dirty" parameter changes to reflect hardware state in the UI without manual refresh

### 🎤 Speech-to-Text (STT) Integration
- Powered by `Whisper.net` with GGML model support
- Captures microphone audio at 16 kHz via `NAudio`
- Supports CUDA-accelerated inference on NVIDIA GPUs (x64 build)
- Transcribed text is automatically injected into the VRChat chatbox via the OSC engine

### ⚙️ OSC Engine
- Built on `BuildSoft.OscCore` and `VRCOscLib`
- Transmits UDP packets to VRChat's OSC endpoint
- Supports custom VRChat avatar parameter mappings via `vrc_config.json`

---

## Requirements

| Requirement | Details |
|---|---|
| **OS** | Windows 10 / 11 (64-bit recommended) |
| **.NET** | 10 (LTS) |
| **Build Tool** | Visual Studio 2026 |
| **Architecture** | `x64` required for the native CefSharp/Whisper/VoiceMeeter components |
| **VRChat** | OSC must be enabled in VRChat settings |
| **VoiceMeeter** | Optional — only required for audio control features |
| **Spotify Account** | Required for Spotify integration |
| **NVIDIA GPU** | Optional — enables CUDA-accelerated Whisper inference |

---

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/dragon99z/VRC-OSC-Handy.git
cd VRC-OSC-Handy
```

### 2. Open in Visual Studio

Open `VRC-OSC-Handy.sln` in **Visual Studio 2026**.

### 3. Restore NuGet Packages

Restore the SDK-style NuGet dependencies. In Visual Studio use **Restore NuGet Packages**, or run `dotnet restore VRC-OSC-Handy.sln` from a Windows terminal.

### 4. Set Build Configuration

Set the build configuration to **Release** and the platform to **x64** for full compatibility with native dependencies (CefSharp, Whisper.net CUDA).

### 5. Build and Run

Build the solution and run the executable. On first launch, the application will automatically create the required configuration files in your AppData directory.

For command-line builds on Windows: `dotnet build VRC-OSC-Handy.sln -c Release -p:Platform=x64`.

---

## Configuration

VRC-OSC-Handy stores its configuration in your local AppData directory (next
to the executable, under `VRC Handy/`). Two JSON files control its behaviour.
Both are normally edited through the app's own UI (the Settings panel), which
writes the files back out for you — you don't need to hand-edit them, though
you can since they're plain JSON.

### `config.json` — Application Settings

Stores Spotify credentials and Whisper STT preferences:

```json
{
  "SpotifyConfig": {
    "Enabled": false,
    "ClientID": "your-client-id",
    "ClientSecret": "your-client-secret"
  },
  "STT": {
    "Model": 0,
    "Language": 0,
    "Translate": false
  }
}
```

| Key | Description |
|---|---|
| `SpotifyConfig.Enabled` | Whether Spotify integration is turned on |
| `SpotifyConfig.ClientID` / `ClientSecret` | Your Spotify Developer App credentials — set these from the Settings panel in the app, which unmasks/masks the fields and saves them for you |
| `STT.Model` | Index into the app's Whisper model dropdown (built from the Whisper.net `GgmlType` list — tiny/base/small/medium/large variants); pick it from the UI rather than guessing the index |
| `STT.Language` | Index into the app's language dropdown (`auto`, `en`, `zh`, `de`, `es`, `ru`, `ko`, `fr`, `ja`, `pt`, `tr`, `pl`, `ca`, `nl`, and more) |
| `STT.Translate` | Whether Whisper translates non-English speech to English instead of transcribing it as-is |

On first launch the app copies these defaults out of its embedded resources;
on every later launch it additively merges in any new keys the bundled
defaults have gained, without touching or removing values you've already set.

### `vrc_config.json` — VRChat OSC Parameter Mapping

Maps the app's Spotify/VoiceMeeter/OSC state to VRChat avatar parameter and
chatbox paths. The real schema has a `Spotify` section, an `Other` section
(clock + STT chatbox toggles), and five `Strip0`–`Strip4` sections — one per
VoiceMeeter strip, each with the same set of bus/mute/gain parameters:

```json
{
  "Spotify": {
    "Next": "Handy/Spotify/Next",
    "Last": "Handy/Spotify/Last",
    "PlayPause": "Handy/Spotify/PlayPause",
    "Song": "Handy/Spotify/Song",
    "ProgressBar": "Handy/Spotify/ProgressBar"
  },
  "Other": {
    "Time": "Handy/Other/Time",
    "STT": "Handy/Other/STT"
  },
  "Strip0": {
    "A1": "Handy/Strip0/A1",
    "A2": "Handy/Strip0/A2",
    "A3": "Handy/Strip0/A3",
    "A4": "Handy/Strip0/A4",
    "A5": "Handy/Strip0/A5",
    "B1": "Handy/Strip0/B1",
    "B2": "Handy/Strip0/B2",
    "B3": "Handy/Strip0/B3",
    "Mute": "Handy/Strip0/Mute",
    "Gain": "Handy/Strip0/Gain"
  }
}
```

`Strip1` through `Strip4` repeat the same `A1`–`A5`/`B1`–`B3`/`Mute`/`Gain`
shape. The `Strip0..Strip4` split mirrors VoiceMeeter's fixed 5-strip layout
and is baked into the app's config classes, so it isn't a flexible list —
rename the target avatar-parameter paths, but don't restructure the strips.

---

## Dependencies

| Package | Purpose |
|---|---|
| `NAudio` | Microphone audio capture at 16 kHz |
| `Whisper.net` | AI-powered speech-to-text (GGML models, CUDA support) |
| `CefSharp.Wpf.NETCore` | Embedded Chromium browser for Spotify OAuth2 login |
| `SpotifyAPI.Web` | Spotify playback data polling |
| `BuildSoft.OscCore` | OSC packet encoding |
| `VRCOscLib` | VRChat-specific OSC abstractions |
| In-tree VoiceMeeter wrapper | VoiceMeeter Remote API access via the installed native DLL |

Native dependencies are supplied through NuGet/runtime assets and the application targets Windows x64. The old post-build `lib/` relocation step was removed because modern .NET requires runtime and dependency metadata to remain alongside the executable.

---

## Architecture

The application is structured around a central `MainWindow` orchestration layer that manages the lifecycle of all integrations and synchronises data across threads.

```
┌─────────────────────────────────────────────────────┐
│                    MainWindow                        │
│          (Thread-safe UI dispatcher hub)             │
└────────┬────────────┬──────────────┬────────────────┘
         │            │              │
   ┌─────▼──────┐ ┌───▼────────┐ ┌──▼────────────┐
   │  Spotify   │ │VoiceMeeter │ │  STT (Whisper) │
   │  Module    │ │  Module    │ │    Module      │
   └─────┬──────┘ └───┬────────┘ └──┬────────────┘
         │            │              │
         └────────────▼──────────────┘
                 ┌────────────┐
                 │ VRCOSC     │
                 │ OSC Engine │
                 └─────┬──────┘
                       │ UDP
                 ┌─────▼──────┐
                 │  VRChat    │
                 └────────────┘
```

For a full technical deep-dive into each subsystem, see the [DeepWiki documentation](https://deepwiki.com/dragon99z/VRC-OSC-Handy).

| Wiki Page | Description |
|---|---|
| [Overview](https://deepwiki.com/dragon99z/VRC-OSC-Handy/1-overview) | High-level architecture and component relationships |
| [Getting Started & Setup](https://deepwiki.com/dragon99z/VRC-OSC-Handy/1.1-getting-started-and-setup) | Build environment, NuGet packages, and project structure |
| [Application Lifecycle & Bootstrap](https://deepwiki.com/dragon99z/VRC-OSC-Handy/1.2-application-lifecycle-and-bootstrap) | App startup, CrashHandler, and MainWindow initialisation |
| [Core Architecture](https://deepwiki.com/dragon99z/VRC-OSC-Handy/2-core-architecture) | MainWindow as integration hub and OSC UDP packet transmission |
| [Configuration System](https://deepwiki.com/dragon99z/VRC-OSC-Handy/3-configuration-system) | `config.json` and `vrc_config.json` deep dive |
| [Spotify Integration](https://deepwiki.com/dragon99z/VRC-OSC-Handy/4-spotify-integration) | OAuth2 flow, CefSharp browser, and playback polling loops |
| [VoiceMeeter Integration](https://deepwiki.com/dragon99z/VRC-OSC-Handy/5-voicemeeter-integration) | RemoteControle wrapper and dirty-parameter polling |
| [Speech-to-Text Integration](https://deepwiki.com/dragon99z/VRC-OSC-Handy/6-speech-to-text-(stt)-integration) | Whisper.net pipeline, GGML models, and async audio capture |
| [Infrastructure & Utilities](https://deepwiki.com/dragon99z/VRC-OSC-Handy/7-infrastructure-and-utilities) | DebugLogger, CrashHandler, and ParticleSystem UI |
| [Glossary](https://deepwiki.com/dragon99z/VRC-OSC-Handy/8-glossary) | Key terms and definitions |

---

## VRChat OSC Setup

1. Launch VRChat and navigate to **Settings → OSC**.
2. Enable OSC and ensure the default port (`9000`) is set.
3. Start VRC-OSC-Handy — it will automatically connect to VRChat's OSC endpoint.

> **Tip:** If you use a custom avatar with specific parameters, configure `vrc_config.json` to map those parameters to the application's functions.

---

## Enabling Spotify

1. Go to the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard) and create a new application.
2. Set the redirect URI to exactly `http://localhost:5656/callback` — the app's embedded auth server is hard-coded to that port, so it must match.
3. In VRC-OSC-Handy's Settings panel, enter your **Client ID** and **Client Secret** (this saves them into `config.json` for you).
4. Click **Connect Spotify** — an embedded browser window will open for login.

---

## Speech-to-Text Models

VRC-OSC-Handy uses [Whisper.net](https://github.com/sandrohanea/whisper.net) with GGML models. Download a model and place it in the expected models directory (the app will prompt you on first use).

| Model | Size | Speed | Accuracy |
|---|---|---|---|
| `tiny` | ~75 MB | ⚡⚡⚡ | ★★☆☆☆ |
| `base` | ~142 MB | ⚡⚡ | ★★★☆☆ |
| `small` | ~466 MB | ⚡ | ★★★★☆ |
| `medium` | ~1.5 GB | 🐢 | ★★★★★ |

CUDA acceleration is automatically used when an NVIDIA GPU is detected (x64 build only).

---

## Contributing

Contributions, bug reports, and feature requests are welcome! Please open an [issue](https://github.com/dragon99z/VRC-OSC-Handy/issues) or submit a pull request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

For architecture notes, build caveats, and rules for AI coding agents working
in this repo, see [`CLAUDE.md`](CLAUDE.md) and [`AGENTS.md`](AGENTS.md).

### Known limitation

VoiceMeeter support is constructed unconditionally at startup and used
without null checks throughout `MainWindow.xaml.cs`, despite VoiceMeeter
being documented above as optional. Whether this actually breaks on a
machine without VoiceMeeter installed depends on the underlying native
wrapper's error behavior and hasn't been confirmed. See `AGENTS.md` for
details before attempting to "fix" this.

---

## Documentation

Full technical documentation is available on **DeepWiki**:
👉 [![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/dragon99z/VRC-OSC-Handy)

---

## Acknowledgements

- [Whisper.net](https://github.com/sandrohanea/whisper.net) — .NET bindings for OpenAI Whisper
- [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET) — Spotify Web API wrapper
- [CefSharp](https://github.com/cefsharp/CefSharp) — Embedded Chromium for WPF
- [VRCOscLib](https://github.com/ChanyaVRC/VRCOscLib) — VRChat OSC library
- [NAudio](https://github.com/naudio/NAudio) — .NET audio library
- VoiceMeeter by [VB-Audio](https://vb-audio.com/Voicemeeter/)
