# VRC-OSC-Handy

> A Windows desktop companion app that bridges **Spotify**, **VoiceMeeter**, and **Whisper Speech-to-Text** directly into VRChat via the OSC protocol.

[![Platform](https://img.shields.io/badge/platform-Windows-blue?logo=windows)](https://github.com/dragon99z/VRC-OSC-Handy)
[![Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-purple)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/language-C%23-green?logo=csharp)](https://github.com/dragon99z/VRC-OSC-Handy)
[![License](https://img.shields.io/github/license/dragon99z/VRC-OSC-Handy)](LICENSE)
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
- Wraps the VoiceMeeter Remote API (`VmrapiDynWrap`) to dynamically generate UI controls in the main window
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
| **.NET Framework** | 4.7.2 |
| **Build Tool** | Visual Studio 2022 |
| **Architecture** | `x64` preferred (required for CefSharp & Whisper CUDA) |
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

Open `VRC-OSC-Handy.sln` in **Visual Studio 2022**.

### 3. Restore NuGet Packages

In Visual Studio, go to **Tools → NuGet Package Manager → Manage NuGet Packages for Solution** and restore all dependencies. Alternatively, right-click the solution in Solution Explorer and select **Restore NuGet Packages**.

### 4. Set Build Configuration

Set the build configuration to **Release** and the platform to **x64** for full compatibility with native dependencies (CefSharp, Whisper.net CUDA).

### 5. Build and Run

Build the solution and run the executable. On first launch, the application will automatically create the required configuration files in your AppData directory.

---

## Configuration

VRC-OSC-Handy stores its configuration in your local AppData directory. Two JSON files control its behaviour:

### `config.json` — Application Settings

Stores service credentials and STT preferences:

```json
{
  "spotify_client_id": "YOUR_SPOTIFY_CLIENT_ID",
  "spotify_client_secret": "YOUR_SPOTIFY_CLIENT_SECRET",
  "stt_model": "base",
  "stt_language": "en"
}
```

| Key | Description |
|---|---|
| `spotify_client_id` | Your Spotify Developer App client ID |
| `spotify_client_secret` | Your Spotify Developer App client secret |
| `stt_model` | Whisper GGML model size (`tiny`, `base`, `small`, `medium`, `large`) |
| `stt_language` | Language code for Whisper transcription (e.g. `en`, `de`, `ja`) |

### `vrc_config.json` — VRChat OSC Parameter Mapping

Maps VRChat avatar OSC parameters to application functions:

```json
{
  "parameters": {
    "MuteToggle": "/avatar/parameters/MuteToggle",
    "Volume": "/avatar/parameters/Volume"
  }
}
```

---

## Dependencies

| Package | Purpose |
|---|---|
| `NAudio` | Microphone audio capture at 16 kHz |
| `Whisper.net` | AI-powered speech-to-text (GGML models, CUDA support) |
| `CefSharp.Wpf` | Embedded Chromium browser for Spotify OAuth2 login |
| `SpotifyAPI.Web` | Spotify playback data polling |
| `BuildSoft.OscCore` | OSC packet encoding |
| `VRCOscLib` | VRChat-specific OSC abstractions |
| `a-tg.VmrapiDynWrap` | VoiceMeeter Remote API wrapper |

Native assemblies are resolved from a `lib/` sub-folder to keep the root executable directory clean.

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
2. Set the redirect URI to `http://localhost:5000/callback` (or whatever the app instructs on first run).
3. Copy your **Client ID** and **Client Secret** into `config.json`.
4. Launch VRC-OSC-Handy and click **Connect Spotify** — an embedded browser window will open for login.

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

---

## Documentation

Full technical documentation is available on **DeepWiki**:
👉 [https://deepwiki.com/dragon99z/VRC-OSC-Handy](https://deepwiki.com/dragon99z/VRC-OSC-Handy)

---

## Acknowledgements

- [Whisper.net](https://github.com/sandrohanea/whisper.net) — .NET bindings for OpenAI Whisper
- [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET) — Spotify Web API wrapper
- [CefSharp](https://github.com/cefsharp/CefSharp) — Embedded Chromium for WPF
- [VRCOscLib](https://github.com/ChanyaVRC/VRCOscLib) — VRChat OSC library
- [NAudio](https://github.com/naudio/NAudio) — .NET audio library
- VoiceMeeter by [VB-Audio](https://vb-audio.com/Voicemeeter/)
