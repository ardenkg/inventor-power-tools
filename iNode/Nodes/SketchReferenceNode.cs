// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// SketchReferenceNode.cs - References an existing Inventor PlanarSketch
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// References an existing PlanarSketch in the active part document.
    /// Users select a sketch by index (1-based, matching Inventor's display).
    /// The output SketchRefData can be connected to Extrude or Revolve nodes.
    /// </summary>
    public class SketchReferenceNode : Node
    {
        public override string TypeName => "SketchReference";
        public override string Title => "Sketch Reference";
        public override string Category => "Input";

        /// <summary>
        /// 1-based sketch index in the part's PlanarSketches collection.
        /// </summary>
        public int SketchIndex { get; set; } = 1;

        public SketchReferenceNode()
        {
            AddInput("Index", "Sketch #", PortDataType.Number, 1.0);
            AddOutput("Sketch", "Sketch", PortDataType.SketchRef);
            AddOutput("ProfileCount", "Profiles", PortDataType.Number);
        }

        protected override void Compute()
        {
            SketchIndex = (int)GetInput("Index")!.GetDouble(1);
            if (SketchIndex < 1) SketchIndex = 1;

            if (Context?.IsAvailable != true || Context.TargetCompDef == null)
            {
                GetOutput("Sketch")!.Value = new SketchRefData
                {
                    SketchName = $"Sketch {SketchIndex}",
                    ProfileCount = 0
                };
                GetOutput("ProfileCount")!.Value = 0.0;
                return;
            }

            try
            {
                var sketches = Context.TargetCompDef.Sketches;
                if (SketchIndex > sketches.Count)
                {
                    HasError = true;
                    ErrorMessage = $"Sketch #{SketchIndex} not found (part has {sketches.Count} sketch(es))";
                    return;
                }

                dynamic sketch = sketches[SketchIndex]; // 1-based
                int profileCount = 0;
                try { profileCount = sketch.Profiles.Count; } catch { }

                string sketchName = "";
                try { sketchName = sketch.Name; } catch { sketchName = $"Sketch {SketchIndex}"; }

                var refData = new SketchRefData
                {
                    Sketch = sketch,
                    SketchName = sketchName,
                    ProfileCount = profileCount
                };

                GetOutput("Sketch")!.Value = refData;
                GetOutput("ProfileCount")!.Value = (double)profileCount;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Sketch reference failed: {ex.Message}";
            }
        }

        public override string? GetDisplaySummary() => $"#{SketchIndex}";

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?>
            {
                ["Index"] = (double)SketchIndex
            };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("Index", out var val))
            {
                if (val is double d) SketchIndex = Math.Max(1, (int)d);
                else if (val is int i) SketchIndex = Math.Max(1, i);
            }
        }
    }
}
