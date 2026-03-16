# ShinySuite

A Shiny Pokémon encounter tracker. Tracks encounters per route with category filtering, sprite display, encounter counts, and shiny logging.

## Download

Grab the latest release from the [Releases](https://github.com/WraithSpace/ShinySuiteWPF/releases) page.

Extract the zip and run `ShinySuite.exe`. No installer required.

> **Note:** Windows may show a SmartScreen warning since the app is unsigned. Click "More info" → "Run anyway".

## Requirements

- Windows 10 or later (x64)

## Building from Source

```bash
git clone https://github.com/WraithSpace/ShinySuiteWPF.git
cd ShinySuiteWPF
dotnet build
```

Output will be in `bin/Debug/net8.0-windows10.0.17763.0/`.

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

## Features

- Per-route encounter tracking with category pills (Walking, Surfing, Fishing, etc.)
- Shiny/normal sprite display
- Encounter counter per Pokémon
- Shiny log with history dialog
- OCR-based auto-detection via screen capture
- Mini player pop-out window per route
