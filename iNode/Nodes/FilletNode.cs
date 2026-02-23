// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// FilletNode.cs - Fillet edges on a body
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Applies fillet (rounding) to selected edges of a body.
    ///
    /// Fillet cannot be performed on transient bodies (TransientBRep limitation),
    /// so during preview the body is shown WITHOUT the fillet. The fillet is
    /// recorded as a PendingOperation and applied during the Apply phase
    /// when the body is inserted into the part document as a real feature.
    ///
    /// Workflow: Box → Deconstruct Body → Select Edge → Fillet → [Apply]
    /// </summary>
    public class FilletNode : Node
    {
        public override string TypeName => "Fillet";
        public override string Title => "Fillet";
        public override string Category => "Operations";

        public FilletNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("Edges", "Edges", PortDataType.Edge, null);
            AddInput("Radius", "Radius", PortDataType.Number, 1.0);
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var bodyVal = GetInput("Body")!.GetEffectiveValue();
            var edgesVal = GetInput("Edges")!.GetEffectiveValue();
            var radius = GetInput("Radius")!.GetDouble(1.0);

            if (bodyVal is not BodyData srcBody)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }

            if (radius <= 0)
            {
                HasError = true;
                ErrorMessage = "Radius must be positive";
                return;
            }

            if (edgesVal is not EdgeListData edgeList || edgeList.Edges.Count == 0)
            {
                HasError = true;
                ErrorMessage = "No edges selected for fillet";
                return;
            }

            // Build edge snapshots for matching after body insertion
            var snapshots = new List<EdgeSnapshot>();
            foreach (var edgeObj in edgeList.Edges)
            {
                try
                {
                    snapshots.Add(EdgeSnapshot.FromInventorEdge(edgeObj));
                }
                catch { /* Edge snapshot failed — skip */ }
            }

            // Clone the body's metadata and add the pending fillet
            var result = srcBody.CloneMetadata();
            result.Description = $"Fillet (R={radius:F1})";
            result.SourceNodeId = Id;
            result.PendingOperations.Add(new PendingFillet
            {
                Radius = radius,
                EdgeSnapshots = snapshots,
                LiveEdges = new List<object>(edgeList.Edges),
                OriginNodeId = Id
            });

            GetOutput("Body")!.Value = result;
        }
    }
}
