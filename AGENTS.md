# AGENTS.md

This file provides guidance to AI coding assistants and autonomous agents working with code in this repository.

## Project Overview

`Jellyfin.Plugin.Hue` is a native Philips Hue integration plugin for **Jellyfin 12+** (.NET 10 / C# 13).
It synchronizes smart lighting with media playback states (Play, Pause, Resume, Stop) across configurable multi-profiles based on users, devices, and media types.

## Architecture

```text
[ Jellyfin Playback Session ]
        │  ISessionManager events: PlaybackStart, PlaybackProgress (pause/resume), PlaybackStopped
        ▼
[ PlaybackListener (IHostedService) ]
        │  Filters duplicate/noise events (debounce/state tracker)
        ▼
[ HueOrchestrator ]
        │  Evaluates matching HueProfile (Users, Devices, Movies vs Episodes)
        ▼
[ HueClient (CLIP API v2) ]
        │  Local HTTPS with self-signed TLS cert bypass
        ▼
[ Philips Hue Bridge (CLIP API v2) ]
   - /api (Pairing link-button exchange)
   - /clip/v2/resource/room
   - /clip/v2/resource/zone
   - /clip/v2/resource/light
   - /clip/v2/resource/grouped_light
```

### Key Decisions & Conventions

- **Language & Style**:
  - All source code, XML doc comments, and documentation must be in **English**.
  - Commit messages follow Conventional Commits (e.g. `feat:`, `fix:`, `chore:`, `ci:`).
  - Code comments should only be written when necessary (avoid repeating obvious code).
- **Target Framework & Versioning**:
  - `master` branch targets **Jellyfin 12+** on `.NET 10` (`net10.0`).
  - Jellyfin packages: `Jellyfin.Controller 12.0.0+` and `Jellyfin.Model 12.0.0+`.
  - Version numbers for Jellyfin 12+ use `12.x.x.x` (initial `12.0.0.0`).
  - Jellyfin 10.11 compatibility is maintained on `v10.x` branches (`10.x.x.x` / `net9.0`).
- **Code Quality & Analyzers**:
  - `TreatWarningsAsErrors` is enabled.
  - `AnalysisMode` is `AllEnabledByDefault`.
  - StyleCop rules and SerilogAnalyzer are strictly enforced.
  - CA1002 / CA2227: Use `Collection<T>` with getter-only properties where applicable or suppress arrays for XML serialization.
- **Testing**:
  - Unit tests use `xUnit` and `Moq`.
  - Parallel test execution is disabled via `[assembly: CollectionBehavior(DisableTestParallelization = true)]` because tests configure the static `Plugin.Instance` singleton.

## Build & Test Commands

```bash
# Build the entire solution
dotnet build

# Run unit tests
dotnet test

# Build with Release configuration
dotnet build -c Release
```

## Repository Structure

- `Jellyfin.Plugin.Hue/`: The main plugin project.
  - `Api/`: REST controller (`HueApiController`) and DTOs for the web configuration UI.
  - `Configuration/`: `PluginConfiguration.cs`, `HueProfile.cs`, and `configPage.html`.
  - `Hue/`: `HueClient.cs`, `HueDiscovery.cs`, `ColorUtils.cs`, and CLIP v2 models.
  - `Services/`: `HueOrchestrator.cs` and `PlaybackListener.cs`.
  - `Plugin.cs`: Base plugin entry point and `IHasWebPages` registration.
  - `PluginServiceRegistrator.cs`: Dependency injection registration.
- `Jellyfin.Plugin.Hue.Tests/`: Unit tests for client, orchestrator, listeners, and models.
- `.github/workflows/`: CI/CD workflows (`build.yaml`, `test.yaml`, `scan-codeql.yaml`, `release.yaml`).
- `build.yaml`: Jellyfin Plugin Repository Manager (JPRM) metadata.
- `manifest.json`: Jellyfin plugin repository catalog entry.
