# Chronolog

A personal offline journal inspired by Chronolog from the game [Desktop Explorer](https://store.steampowered.com/app/2527160/Desktop_Explorer/). Not affiliated with Recurring Dream.

## Requirements

- **Chronolog Modern** — Windows 10/11 (self-contained, no extra runtime)
- **Chronolog Classic** — Windows 7 SP1+ with [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)
- For building: [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Inno Setup 6](https://jrsoftware.org/isinfo.php)

## Install (release packages)

Build both editions:

```powershell
powershell -ExecutionPolicy Bypass -File .\build-installer.ps1
```

The installer creates **Start Menu** and **Desktop** shortcuts under Skywere Industries (both on by default; you can uncheck them).

### GUI

1. Launch **Chronolog**
2. Create or open a journal, then **Write Journal** for dated entries
3. Search, edit, delete, and attach files per entry
4. **File → Storage Location…** to put the library on a sync folder if you want the same journals on more than one PC

## Repository

```
src/Chronolog/     application (Modern and Classic)
assets/            production icon (installer.ico)
installer/         Inno Setup script
artifacts/         publish payload (generated)
dist/              setup executables (generated)
```

## License
This repository is licensed under the MIT License as provided in the LICENSE file included with the project.
