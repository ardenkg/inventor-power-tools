// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// MergeBodiesNode.cs - Union multiple bodies into one
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Unions multiple bodies together. Connect bodies to A/B/C/D inputs.
    /// At least two bodies must be connected.
    /// </summary>
    public class MergeBodiesNode : Node
    {
        public override string TypeName => "MergeBodies";
        public override string Title => "Merge Bodies";
        public override string Category => "Geometry";

        public MergeBodiesNode()
        {
            AddInput("A", "Body A", PortDataType.Geometry, null);
            AddOptionalInput("B", "Body B", PortDataType.Geometry, null);
            AddOptionalInput("C", "Body C", PortDataType.Geometry, null);
            AddOptionalInput("D", "Body D", PortDataType.Geometry, null);
            AddOutput("Result", "Result", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var bodies = new List<BodyData>();
            foreach (var inputName in new[] { "A", "B", "C", "D" })
            {
                var val = GetInput(inputName)?.GetEffectiveValue();
                if (val is BodyData bd && bd.Body != null)
                    bodies.Add(bd);
            }

            if (bodies.Count < 2)
            {
                HasError = true;
                ErrorMessage = "Connect at least 2 bodies to merge";
                return;
            }

            if (Context?.IsAvailable != true)
            {
                GetOutput("Result")!.Value = new BodyData
                {
                    Description = $"Merge ({bodies.Count} bodies)",
                    SourceNodeId = Id
                };
                return;
            }

            try
            {
                var tb = Context.TB;
                var result = tb.Copy(bodies[0].Body as SurfaceBody);

                for (int i = 1; i < bodies.Count; i++)
                {
                    var copy = tb.Copy(bodies[i].Body as SurfaceBody);
                    tb.DoBoolean(result, copy, BooleanTypeEnum.kBooleanTypeUnion);
                }

                // Merge pending operations
                var pendingOps = new List<PendingOperation>();
                foreach (var bd in bodies)
                    pendingOps.AddRange(bd.PendingOperations);

                GetOutput("Result")!.Value = new BodyData
                {
                    Body = result,
                    Description = $"Merge ({bodies.Count} bodies)",
                    SourceNodeId = Id,
                    PendingOperations = pendingOps
                };
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Merge failed: {ex.Message}";
            }
        }
    }
}
