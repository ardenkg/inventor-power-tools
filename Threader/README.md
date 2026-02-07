# Threader Add-in for Autodesk Inventor 2026

## Overview

Threader is an Autodesk Inventor 2026 add-in that generates **physical geometric threads** (modeled geometry/coils) from cylindrical faces using ISO Metric Profile standards. Unlike cosmetic threads, this add-in creates actual 3D helical geometry.

<p align="center">
  <img src="../images/ThreaderInterface.png" alt="Threader Interface">
</p>

<p align="center">
  <img src="../images/ThreadExamples.png" alt="Thread Examples">
</p>

## Features

- **Cylindrical Face Selection** — Select any cylindrical face on a part to create threads
- **Automatic Diameter Detection** — Detects the diameter and length of the selected cylinder
- **ISO Metric Profile Standards** — Queries Inventor's internal thread data for ISO Metric threads
- **Smart Thread Matching** — Filters available thread designations based on the cylinder's diameter
- **Real-time Preview** — ClientGraphics-based preview shows thread location before creation
- **Physical Thread Generation** — Creates actual helical coil geometry, not just cosmetic annotations
- **Internal & External** — Supports both holes (cut) and shafts (join)
- **Apply/Done/Close Workflow**:
  - **Apply** — Creates the thread and keeps the dialog open
  - **Done** — Creates the thread and closes the dialog
  - **Close** — Closes without making changes

## Requirements

- Autodesk Inventor 2026
- .NET SDK 8.0 or later (for building from source)
- Windows 10/11 x64

## Installation

### Quick Install

1. Double-click `Install.bat`
2. Restart Inventor

### PowerShell Install

```powershell
.\Install-Threader.ps1
```

#### Options

```powershell
# User installation (default)
.\Install-Threader.ps1 -InstallScope User

# Machine-wide installation (requires admin)
.\Install-Threader.ps1 -InstallScope Machine

# Debug build
.\Install-Threader.ps1 -Configuration Debug

# Skip build step
.\Install-Threader.ps1 -SkipBuild
```

### Manual Installation

1. Build the project:
   ```powershell
   dotnet build -c Release
   ```
2. Copy `Threader.dll` and `Threader.addin` to one of the following:
   - **User:** `%APPDATA%\Autodesk\Inventor 2026\Addins\Threader\`
   - **Machine:** `C:\ProgramData\Autodesk\Inventor Addins\Threader\`

## Uninstallation

- Double-click `Uninstall.bat`
- Or run: `.\Uninstall-Threader.ps1`

## Usage

1. Open a Part document in Inventor
2. Go to the **Power Tools** tab on the ribbon
3. Click the **Threader** button
4. Select a cylindrical face on your part
5. Choose a thread designation from the dropdown
6. Adjust thread length if needed
7. Click **Apply** to create and continue, or **Done** to create and close

### Keyboard Shortcuts

- **Ctrl+L** — Open the Threader dialog
- **Escape** — Close the dialog

## UI Controls

### Cylinder Selection
- Click on any cylindrical face
- The diameter and length are automatically detected
- Internal (holes) and external (shafts) cylinders are both supported

### Thread Designation
- Dropdown shows ISO Metric threads that match the cylinder diameter
- Displays pitch, major diameter, and minor diameter
- Coarse and fine pitch options available

### Options
- **Thread Length** — Adjust the length of threads (defaults to full cylinder length)
- **Right-Hand Thread** — Toggle for left-hand threads
- **Show Preview** — Enable/disable the ClientGraphics preview

### Preview
- Cyan helix shows the thread pitch and location
- Red circle shows the major diameter
- Green circle shows the minor diameter

## Supported Thread Standards

- ISO Metric Coarse (M1–M64)
- ISO Metric Fine (M8x1, M10x1.25, etc.)
- Both internal and external threads

## Technical Details

### Thread Generation Method

The add-in uses Inventor's Coil feature to create the thread geometry:
1. Creates a work axis along the cylinder
2. Creates a sketch with the ISO metric thread profile (60° triangle)
3. Generates a helical coil with the correct pitch and number of revolutions
4. For external threads: Join operation
5. For internal threads: Cut operation

### Preview System

ClientGraphics are used to display:
- Helical path at the pitch diameter
- Major and minor diameter indicator circles
- Preview is cleared automatically when creating the thread or closing

## Project Structure

```
Threader/
├── StandardAddInServer.cs              # Main add-in entry point & ribbon integration
├── Threader.addin                      # Inventor add-in manifest
├── Threader.csproj                     # Project file (.NET 8.0)
├── Core/
│   ├── CylinderAnalyzer.cs             # Analyzes cylindrical faces
│   ├── ThreadDataManager.cs            # Manages ISO Metric thread data
│   ├── ThreadGenerator.cs              # Creates physical thread geometry
│   └── ThreadPreviewManager.cs         # ClientGraphics preview rendering
├── UI/
│   ├── ThreaderForm.cs                 # Form code-behind
│   └── ThreaderForm.Designer.cs        # Form UI layout
├── Properties/
│   └── AssemblyInfo.cs                 # Assembly metadata & GUID
├── Install-Threader.ps1                # PowerShell installer
├── Uninstall-Threader.ps1              # PowerShell uninstaller
├── Install.bat                         # Batch installer wrapper
├── Uninstall.bat                       # Batch uninstaller wrapper
└── README.md
```

## Building from Source

```powershell
cd Threader
dotnet build -c Release
```

## Troubleshooting

### Add-in doesn't appear in ribbon
- Ensure Inventor 2026 is installed
- Check that the add-in is enabled in **Tools → Options → Add-ins**
- Verify files are in the correct Addins folder

### Thread creation fails
- Ensure a valid cylindrical face is selected
- Check that the cylinder diameter matches available thread standards
- Verify sufficient thread length for at least 1.5 pitches

### Preview not showing
- Enable the **Show Preview** checkbox
- Ensure a thread designation is selected
- Try selecting a different face and reselecting

## License

This project is licensed under the [MIT License](../LICENSE).
