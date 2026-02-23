// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ChamferNode.cs - Chamfer edges on a body
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Applies chamfer (bevel) to selected edges of a body.
    ///
    /// Like Fillet, chamfer cannot be done on transient bodies, so it is
    /// recorded as a PendingOperation and applied during the Apply phase.
    ///
    /// Workflow: Box → Deconstruct Body → Select Edge → Chamfer → [Apply]
    /// </summary>
    public class ChamferNode : Node
    {
        public override string TypeName => "Chamfer";
        public override string Title => "Chamfer";
        public override string Category => "Operations";

        public ChamferNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("Edges", "Edges", PortDataType.Edge, null);
            AddInput("Distance", "Distance", PortDataType.Number, 1.0);
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var bodyVal = GetInput("Body")!.GetEffectiveValue();
            var edgesVal = GetInput("Edges")!.GetEffectiveValue();
            var distance = GetInput("Distance")!.GetDouble(1.0);

            if (bodyVal is not BodyData srcBody)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }

            if (distance <= 0)
            {
                HasError = true;
                ErrorMessage = "Distance must be positive";
                return;
            }

            if (edgesVal is not EdgeListData edgeList || edgeList.Edges.Count == 0)
            {
                HasError = true;
                ErrorMessage = "No edges selected for chamfer";
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

            // Clone the body's metadata and add the pending chamfer
            var result = srcBody.CloneMetadata();
            result.Description = $"Chamfer (D={distance:F1})";
            result.SourceNodeId = Id;
            result.PendingOperations.Add(new PendingChamfer
            {
                Distance = distance,
                EdgeSnapshots = snapshots,
                LiveEdges = new List<object>(edgeList.Edges),
                OriginNodeId = Id
            });

            GetOutput("Body")!.Value = result;
        }
    }
}
