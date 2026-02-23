// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ColorFacesNode.cs - Apply color to selected faces
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Applies a color (RGB) to selected faces on a body.
    /// Uses the same Appearance asset approach as the ColoringTool add-in.
    ///
    /// Color is applied as a PendingOperation during the commit phase because
    /// face appearance requires real Inventor faces (not transient bodies).
    ///
    /// During preview, the target faces are highlighted with the chosen color.
    ///
    /// Workflow: Deconstruct Body → Filter Faces → Color Faces → [Commit]
    /// </summary>
    public class ColorFacesNode : Node
    {
        public override string TypeName => "ColorFaces";
        public override string Title => "Color Faces";
        public override string Category => "Operations";

        public ColorFacesNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("Faces", "Faces", PortDataType.Face, null);
            AddInput("R", "R", PortDataType.Number, 255.0);
            AddInput("G", "G", PortDataType.Number, 0.0);
            AddInput("B", "B", PortDataType.Number, 0.0);
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var bodyVal = GetInput("Body")!.GetEffectiveValue();
            var facesVal = GetInput("Faces")!.GetEffectiveValue();
            int r = Math.Max(0, Math.Min(255, (int)GetInput("R")!.GetDouble(255)));
            int g = Math.Max(0, Math.Min(255, (int)GetInput("G")!.GetDouble(0)));
            int b = Math.Max(0, Math.Min(255, (int)GetInput("B")!.GetDouble(0)));

            if (bodyVal is not BodyData srcBody)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }

            if (facesVal is not FaceListData faceList || faceList.Faces.Count == 0)
            {
                HasError = true;
                ErrorMessage = "No faces selected for coloring";
                return;
            }

            // Build face snapshots for matching after body insertion
            var snapshots = new List<FaceSnapshot>();
            foreach (var faceObj in faceList.Faces)
            {
                try
                {
                    snapshots.Add(FaceSnapshot.FromInventorFace(faceObj));
                }
                catch { /* Snapshot failed — skip */ }
            }

            // Clone the body's metadata and add the pending color operation
            var result = srcBody.CloneMetadata();
            result.Description = $"Color ({r},{g},{b})";
            result.SourceNodeId = Id;
            result.PendingOperations.Add(new PendingColorFaces
            {
                Red = r,
                Green = g,
                Blue = b,
                FaceSnapshots = snapshots,
                LiveFaces = new List<object>(faceList.Faces),
                OriginNodeId = Id
            });

            GetOutput("Body")!.Value = result;
        }

        public override string? GetDisplaySummary()
        {
            int r = (int)(GetInput("R")?.GetEffectiveValue() is double rv ? rv : 255);
            int g = (int)(GetInput("G")?.GetEffectiveValue() is double gv ? gv : 0);
            int b = (int)(GetInput("B")?.GetEffectiveValue() is double bv ? bv : 0);
            return $"RGB({r},{g},{b})";
        }
    }
}
