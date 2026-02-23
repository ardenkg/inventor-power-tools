# Power Tools for Autodesk Inventor

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Inventor 2022-2026](https://img.shields.io/badge/Inventor-2022--2026-blue.svg)](https://www.autodesk.com/products/inventor)
[![.NET](https://img.shields.io/badge/.NET-Framework%204.8%20%7C%208.0-purple.svg)](https://dotnet.microsoft.com/)

> Missing NX's smart selection in Inventor? This addon brings tangent face selection, boss/pocket filtering, visual node-based parametric editing, and more.

NX-style topology selection, face coloring, physical thread generation, and visual parametric node editing for Autodesk Inventor. All tools live under the **Power Tools** tab in Inventor's ribbon.

<p align="center">
  <img src="images/ClassSelectionInterface.PNG" width="30%" alt="Class Selection Interface">
  <img src="images/ColoringToolInterface.png" width="30%" alt="Coloring Tool Interface">
  <img src="images/ThreaderInterface.png" width="30%" alt="Threader Interface">
</p>

<p align="center">
  <img src="images/BossPocketSelection.png" width="45%" alt="Boss/Pocket Selection">
  <img src="images/ThreadExamples.png" width="45%" alt="Thread Examples">
</p>

<p align="center">
  <img src="images/TypeFIlters.png" width="45%" alt="Selection Filter Types">
</p>

## Supported Versions

| Inventor Version | .NET Runtime | Status |
|------------------|-------------|--------|
| **2026** | .NET 8.0 | Tested |
| **2025** | .NET 8.0 | Tested |
| **2024** | .NET Framework 4.8 | Compatible (untested) |
| **2023** | .NET Framework 4.8 | Compatible (untested) |
| **2022** | .NET Framework 4.8 | Compatible (untested) |

The projects multi-target `net48` and `net8.0-windows`. The installer automatically detects which Inventor versions are installed and deploys the correct build:
- **Inventor 2025-2026**  `net8.0-windows` build
- **Inventor 2022-2024**  `net48` (.NET Framework 4.8) build

## Add-ins

| Add-in | Description | Shortcut |
|--------|-------------|----------|
| [CaseSelection](CaseSelection/) | NX-style smart face selection with topology filters (tangent, boss/pocket, feature, blend) | `Ctrl+J` |
| [ColoringTool](ColoringTool/) | Select faces with topology filters and apply any color via Inventor's appearance system | `Ctrl+K` |
| [Threader](Threader/) | Generate physical helical thread geometry from cylindrical faces using ISO Metric and ANSI Unified standards | `Ctrl+L` |
| [iNode](iNode/) | Visual node-based parametric editor inspired by Grasshopper. Connect nodes to build geometry workflows. **Unfinished prototype - many features are broken or incomplete. Included for reference only.** | `Ctrl+N` |

## Quick Start

### Install All Add-ins

Double-click **`Install-All.bat`** or run:

```powershell
.\Install-All.ps1
```

This builds all three projects and copies them to the Inventor Add-ins folder for every detected Inventor version. Restart Inventor after installing.

### Install a Single Add-in

Each add-in can be installed independently:

```powershell
# Example: install only CaseSelection
.\CaseSelection\Install-CaseSelection.ps1
```

Or double-click the `Install.bat` inside any add-in folder.

### Uninstall

```powershell
.\Uninstall-All.ps1                              # Remove all
.\CaseSelection\Uninstall-CaseSelection.ps1      # Remove one
```

## Requirements

- **Autodesk Inventor 2022, 2023, 2024, 2025, or 2026**
- **[.NET SDK 8.0](https://dotnet.microsoft.com/download)** or later (for building from source)
- **Windows 10/11** x64

## Usage

1. Open a Part or Assembly document in Inventor
2. Go to the **Power Tools** tab on the ribbon
3. Each add-in has its own panel with a button to launch it

## Building from Source

```powershell
# Build all (builds for both net48 and net8.0-windows)
dotnet build CaseSelection\CaseSelection.csproj -c Release
dotnet build ColoringTool\ColoringTool.csproj -c Release
dotnet build Threader\Threader.csproj -c Release
```

The Inventor Interop assembly is auto-detected from any installed Inventor version (2022-2026) via `Directory.Build.props`. No manual path configuration needed.

## Project Structure

```
Inventor-Addins/
+-- Directory.Build.props               # Shared build config (auto-detects Inventor)
+-- Install-All.bat / .ps1              # Install all add-ins at once
+-- Uninstall-All.bat / .ps1            # Uninstall all add-ins
+-- CaseSelection/                      # Smart face selection tool
|   +-- Core/                           # Selection logic & topology algorithms
|   +-- UI/                             # WinForms floating dialog
|   +-- ...
+-- ColoringTool/                       # Face coloring tool
|   +-- Core/                           # Selection & color application logic
|   +-- UI/                             # WinForms dialog with color picker
|   +-- ...
+-- Threader/                           # Physical thread generator
|   +-- Core/                           # Cylinder analysis & thread generation
|   +-- UI/                             # WinForms dialog with thread options
|   +-- ...
+-- iNode/                              # Visual parametric node editor (Prototype)
    +-- Core/                           # Node graph engine & serialization
    +-- Nodes/                          # Node type implementations
    +-- UI/                             # Node editor canvas & forms
    +-- ...
```

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
