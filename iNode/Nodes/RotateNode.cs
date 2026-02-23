// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// RotateNode.cs - Rotate a body around an axis
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Rotates a body around an axis through a center point.
    /// Angle is specified in degrees.
    /// Axis is a direction vector (e.g., 0,0,1 for Z-axis).
    /// </summary>
    public class RotateNode : Node
    {
        public override string TypeName => "Rotate";
        public override string Title => "Rotate";
        public override string Category => "Transform";

        public RotateNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("Angle", "Angle (deg)", PortDataType.Number, 45.0);
            AddInput("Axis", "Axis", PortDataType.Point3D, (0.0, 1.0, 0.0));
            AddInput("Center", "Center", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var geomVal = GetInput("Body")!.GetEffectiveValue();
            var angle = GetInput("Angle")!.GetDouble(45.0);
            var axis = GetInput("Axis")!.GetPoint3D();
            var center = GetInput("Center")!.GetPoint3D();

            if (geomVal is not BodyData srcBody)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }

            if (Context?.IsAvailable != true || srcBody.Body == null)
            {
                GetOutput("Body")!.Value = new BodyData
                {
                    Description = $"Rotate ({angle:F0}\u00B0)",
                    SourceNodeId = Id,
                    IsFromActivePart = srcBody.IsFromActivePart,
                    PendingOperations = new List<PendingOperation>(srcBody.PendingOperations)
                };
                return;
            }

            try
            {
                double radians = angle * Math.PI / 180.0;
                var copy = Context.TB.Copy(srcBody.Body as SurfaceBody);
                var matrix = Context.CreateRotationMatrix(
                    center.X, center.Y, center.Z,
                    axis.X, axis.Y, axis.Z,
                    radians);
                Context.TB.Transform(copy, matrix);

                GetOutput("Body")!.Value = new BodyData
                {
                    Body = copy,
                    Description = $"Rotate ({angle:F0}\u00B0)",
                    SourceNodeId = Id,
                    IsFromActivePart = srcBody.IsFromActivePart,
                    PendingOperations = new List<PendingOperation>(srcBody.PendingOperations)
                };
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Rotate failed: {ex.Message}";
            }
        }
    }
}
