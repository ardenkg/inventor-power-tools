// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ScaleNode.cs - Scale a body uniformly
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Scales a body uniformly around a center point.
    /// Factor > 1 enlarges, 0 &lt; factor &lt; 1 shrinks.
    /// </summary>
    public class ScaleNode : Node
    {
        public override string TypeName => "Scale";
        public override string Title => "Scale";
        public override string Category => "Transform";

        public ScaleNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("Factor", "Factor", PortDataType.Number, 2.0);
            AddInput("Center", "Center", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var geomVal = GetInput("Body")!.GetEffectiveValue();
            var factor = GetInput("Factor")!.GetDouble(2.0);
            var center = GetInput("Center")!.GetPoint3D();

            if (geomVal is not BodyData srcBody)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }

            if (factor <= 0)
            {
                HasError = true;
                ErrorMessage = "Scale factor must be positive";
                return;
            }

            if (Context?.IsAvailable != true || srcBody.Body == null)
            {
                GetOutput("Body")!.Value = new BodyData
                {
                    Description = $"Scale (\u00D7{factor:F2})",
                    SourceNodeId = Id,
                    IsFromActivePart = srcBody.IsFromActivePart,
                    PendingOperations = new List<PendingOperation>(srcBody.PendingOperations)
                };
                return;
            }

            try
            {
                var copy = Context.TB.Copy(srcBody.Body as SurfaceBody);
                var matrix = Context.CreateScaleMatrix(center.X, center.Y, center.Z, factor);
                Context.TB.Transform(copy, matrix);

                GetOutput("Body")!.Value = new BodyData
                {
                    Body = copy,
                    Description = $"Scale (\u00D7{factor:F2})",
                    SourceNodeId = Id,
                    IsFromActivePart = srcBody.IsFromActivePart,
                    PendingOperations = new List<PendingOperation>(srcBody.PendingOperations)
                };
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Scale failed: {ex.Message}";
            }
        }
    }
}
