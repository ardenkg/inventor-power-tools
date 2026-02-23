# iNode - Visual Parametric Editor for Autodesk Inventor

> **PROTOTYPE - INCOMPLETE & UNSTABLE**
>
> iNode is an **unfinished prototype**. Many features are partially implemented or broken. Node execution, wiring, and geometry generation may not work correctly or at all. This was a proof-of-concept that didn't get enough development time to reach a usable state.
>
> **Do not rely on this for any real work.** Expect crashes, missing functionality, and unexpected behavior. This add-in is included in the repo for reference only - it is not production-ready and is not actively maintained.

A node-based visual programming add-in for Autodesk Inventor, inspired by Grasshopper for Rhino. Create parametric design workflows by connecting nodes representing parameters, math operations, and geometry.

## Features

- **Node-based editor** - Free-floating, resizable window with a zoomable/pannable canvas
- **Visual wiring** - Connect nodes with bezier curves, color-coded by data type
- **Node library** - Input parameters, math operations, geometry primitives, transforms
- **Inventor integration** - Apply workflows to create geometry in the active part document
- **Save/Load** - Persist workflows as `.inode` (JSON) files
- **CaseSelection-matching style** - White theme, blue accents, Segoe UI fonts

## Node Library

| Category   | Node            | Description                          |
|------------|-----------------|--------------------------------------|
| Input      | Number Slider   | Adjustable slider with min/max range |
| Input      | Number          | Static numeric value                 |
| Input      | Point (X,Y,Z)  | 3D coordinate input                  |
| Math       | Add             | A + B                                |
| Math       | Multiply        | A × B                                |
| Math       | Divide          | A ÷ B (with divide-by-zero handling) |
| Geometry   | Box             | Creates box extrusion in Inventor    |
| Geometry   | Cylinder        | Creates cylindrical extrusion        |
| Geometry   | Sphere          | Creates sphere via revolve           |
| Transform  | Move            | Translates geometry by vector        |
| Transform  | Linear Pattern  | Repeats geometry along an axis       |

## Controls

### Mouse
| Action | Description |
|--------|-------------|
| Left-click | Select node/port |
| Left-drag on node | Move node(s) |
| Left-drag from port | Create connection wire |
| Right-click canvas | Add node menu |
| Right-click node | Delete/Duplicate menu |
| Click on wire | Delete connection |
| Middle-drag / Space+Left-drag | Pan canvas |
| Scroll wheel | Zoom in/out |
| Double-click node | Edit inline value |

### Keyboard
| Key | Action |
|-----|--------|
| Delete | Remove selected nodes |
| Ctrl+A | Select all |
| F | Frame all nodes in view |
| Escape | Close editor |

## Installation

### Prerequisites
- Autodesk Inventor 2022–2026
- .NET SDK 8.0+ (for building)

### Install
```powershell
.\Install-iNode.ps1
```
Or double-click `Install.bat`.

### Uninstall
```powershell
.\Uninstall-iNode.ps1
```

## Usage

1. Open Autodesk Inventor
2. Open or create a Part document
3. Navigate to the **Power Tools** tab on the ribbon
4. Click **iNode Editor**
5. Right-click the canvas to add nodes
6. Connect nodes by dragging between ports
7. Click **▶ Apply Workflow** to generate geometry

## Architecture

```
iNode/
├── Core/                   # Engine
│   ├── Node.cs             # Base node class
│   ├── Port.cs             # Port definitions
│   ├── Connection.cs       # Wire between ports
│   ├── NodeGraph.cs        # Graph + execution engine
│   ├── NodeFactory.cs      # Node type registry
│   ├── PortDataType.cs     # Data type enum
│   └── WorkflowSerializer.cs  # JSON save/load
├── Nodes/                  # Node implementations
│   ├── NumberSliderNode.cs
│   ├── NumberNode.cs
│   ├── Point3DNode.cs
│   ├── AddNode.cs
│   ├── MultiplyNode.cs
│   ├── DivideNode.cs
│   ├── BoxNode.cs
│   ├── CylinderNode.cs
│   ├── SphereNode.cs
│   ├── MoveNode.cs
│   └── PatternLinearNode.cs
├── UI/                     # User interface
│   ├── NodeEditorForm.cs   # Main window
│   ├── NodeEditorCanvas.cs # Canvas rendering + interaction
│   └── NodeSearchPopup.cs  # Quick node search
├── StandardAddInServer.cs  # Inventor add-in entry point
├── Properties/
│   └── AssemblyInfo.cs
├── iNode.addin             # Inventor manifest
├── iNode.csproj            # Project file
└── iNode.sln               # Solution file
```

## Wire Color Legend

| Data Type | Color |
|-----------|-------|
| Number    | Green |
| Point 3D  | Gold  |
| Geometry  | Red   |

## License

This project is licensed under the MIT License. See the [LICENSE](../LICENSE) file for details.
